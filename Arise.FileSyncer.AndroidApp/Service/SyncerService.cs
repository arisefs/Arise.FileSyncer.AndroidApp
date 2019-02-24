using System;
using System.Threading;
using Android.Content;
using Android.OS;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.FileSync;
using Timer = System.Timers.Timer;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal class SyncerService : IDisposable
    {
        private const string TAG = "SyncerService";

        private static volatile SyncerService instance = null;
        public static SyncerService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SyncerService(MainApplication.AppContext);
                }

                return instance;
            }
        }

        public SyncerConfig Config { get; }
        public SyncerPeer Peer { get; }

        private readonly NetworkDiscovery discovery;
        private readonly NetworkListener listener;

        private readonly Context context;
        private readonly SyncerNotification notification;
        private readonly Timer progressTimer;

        public SyncerService(Context context)
        {
            this.context = context;
            notification = new SyncerNotification(context);
            SetupOSMethods();

            Config = new SyncerConfig();

            if (!Config.Load())
            {
                Log.Info($"{TAG}: Failed to load config. Creating new!");
                Config.PeerSettings = new SyncerPeerSettings(Guid.NewGuid(), $"{Build.Manufacturer} {Build.Model}");
                Config.Save();
            }

            Peer = new SyncerPeer(Config.PeerSettings);
            listener = new NetworkListener(Config, Peer.AddConnection);
            discovery = new NetworkDiscovery(Config, Peer, listener);

            // Subscribe to save events
            Peer.NewPairAdded += (s, e) => Config.Save();
            Peer.ProfileAdded += (s, e) => Config.Save();
            Peer.ProfileRemoved += (s, e) => Config.Save();
            Peer.ProfileChanged += (s, e) => Config.Save();

            // Auto accept pair requests
            Peer.PairingRequest += (s, e) =>
            {
                Log.Info("Auto accepting pair: " + e.DisplayName);
                e.ResultCallback(true);
            };

            Log.Verbose($"{TAG}: Initialized");
            Log.Verbose($"{TAG}: Listener IP: {listener.LocalEndpoint}");

            progressTimer = new Timer();
            progressTimer.Elapsed += ProgressTimer_Elapsed;
            progressTimer.Interval = 1000; // 1 second
            progressTimer.AutoReset = true;
            progressTimer.Start();
        }

        public void Run()
        {
            // Send out a single discovery signal
            discovery.SendDiscoveryMessage();
            Log.Debug($"{TAG}: Discovery message sent");

            do
            {
                // Wait some time to allow it get into sync state
                Thread.Sleep(5000);

            } while (Peer.IsSyncing()); // Check sync state

            Log.Debug($"{TAG}: Finished");
        }

        public void SendDiscoveryMessage()
        {
            discovery.SendDiscoveryMessage();
        }

        public void SetAllowPairing(bool newState)
        {
            Log.Info("Setting 'AllowPairing' to " + newState);
            Peer.AllowPairing = newState;

            if (newState) discovery.SendDiscoveryMessage();
        }

        private void SetupOSMethods()
        {
            // Setup Log
            Log.Error = (message) => Android.Util.Log.Error(Constants.TAG, $"{TAG}: {message}");
            Log.Warning = (message) => Android.Util.Log.Warn(Constants.TAG, $"{TAG}: {message}");
            Log.Info = (message) => Android.Util.Log.Info(Constants.TAG, $"{TAG}: {message}");
            Log.Verbose = (message) => Android.Util.Log.Verbose(Constants.TAG, $"{TAG}: {message}");
            Log.Debug = (message) => Android.Util.Log.Debug(Constants.TAG, $"{TAG}: {message}");

            // Setup config folder access
            SyncerConfig.GetConfigFolderPath = () => context.FilesDir.AbsolutePath;

            // Setup file utility functions
            Utility.FileCreate = SyncerUtility.FileCreate;
            Utility.FileDelete = SyncerUtility.FileDelete;
            Utility.FileRename = SyncerUtility.FileRename;
            Utility.FileCreateWriteStream = SyncerUtility.FileCreateWriteStream;
            Utility.FileCreateReadStream = SyncerUtility.FileCreateReadStream;
            Utility.DirectoryCreate = SyncerUtility.DirectoryCreate;

            // Disable Timestamp and FileSetTime since its not supported
            SyncerPeer.SupportTimestamp = false;
            Utility.FileSetTime = (_a, _b, _c, _d, _e) => false;
        }

        private void ProgressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Peer.IsSyncing()) notification.Show(Peer.GetGlobalProgress());
            else notification.Clear();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    progressTimer.Dispose();
                    listener.Dispose();
                    discovery.Dispose();
                    Peer.Dispose();
                    notification.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
