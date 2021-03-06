using System;
using System.Collections.Generic;
using System.Threading;
using Android.Content;
using Android.OS;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.FileSync;
using Microsoft.AppCenter.Analytics;

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
        public NetworkDiscovery Discovery { get; }

        public ProgressStatus GlobalProgress { get; private set; }

        private readonly NetworkListener listener;
        private readonly KeyConfig keyConfig;

        private readonly Context context;
        private readonly SyncerNotification notification;
        private readonly ProgressTracker progressTracker;

        public SyncerService(Context context)
        {
            this.context = context;
            notification = new SyncerNotification(context);
            SetupOSMethods();

            // Load config
            Config = new SyncerConfig();
            LoadResult loadResult = Config.Load(CreatePeerSettings);
            if (loadResult != LoadResult.Loaded)
            {
                if (loadResult == LoadResult.Created) Log.Info("Created new config");
                if (Config.Save()) Log.Info("Saved config after create/upgrade");
                else Log.Error("Failed to save config after create/upgrade");
            }

            // Load key
            keyConfig = new KeyConfig(1024);
            loadResult = keyConfig.Load();
            if (loadResult != LoadResult.Loaded)
            {
                if (loadResult == LoadResult.Created) Log.Info("Created new key");
                if (keyConfig.Save()) Log.Info("Saved key after create/upgrade");
                else Log.Error("Failed to save key after create/upgrade");
            }

            Peer = new SyncerPeer(Config.PeerSettings);
            listener = new NetworkListener(Config, keyConfig, Peer.AddConnection);
            Discovery = new NetworkDiscovery(Config, Peer, listener);

            // Subscribe to save events
            Peer.NewPairAdded += (s, e) => Config.Save();
            Peer.ProfileAdded += (s, e) => Config.Save();
            Peer.ProfileRemoved += (s, e) => Config.Save();
            Peer.ProfileChanged += (s, e) => Config.Save();
            Peer.FileBuilt += Peer_FileBuilt;

            // Auto accept pair requests
            Peer.PairingRequest += (s, e) =>
            {
                Log.Info("Auto accepting pair: " + e.DisplayName);
                e.ResultCallback(true);
            };

            Log.Verbose($"{TAG}: Initialized");
            Log.Verbose($"{TAG}: Listener IP: {listener.LocalEndpoint}");

            progressTracker = new ProgressTracker(Peer, 1000, 5);
            progressTracker.ProgressUpdate += ProgressTracker_ProgressUpdate;
        }

        private static SyncerPeerSettings CreatePeerSettings()
        {
            return new SyncerPeerSettings(Guid.NewGuid(), $"{Build.Manufacturer} {Build.Model}");
        }

        public void Run()
        {
            // Send out a single discovery signal
            Discovery.SendDiscoveryMessage();
            Log.Debug($"{TAG}: Discovery message sent");

            do
            {
                // Wait some time to allow it get into sync state
                Thread.Sleep(5000);

            } while (Peer.IsSyncing()); // Check sync state

            Log.Debug($"{TAG}: Finished");
        }

        public void SetAllowPairing(bool newState)
        {
            Log.Info("Setting 'AllowPairing' to " + newState);
            Peer.AllowPairing = newState;

            if (newState) Discovery.SendDiscoveryMessage();
        }

        private void SetupOSMethods()
        {
            Log.Error = (message) =>
            {
                var properties = new Dictionary<string, string>
                {
                    { "Message", message }
                };

                Analytics.TrackEvent("Internal Error", properties);
                Android.Util.Log.Error(Constants.TAG, $"{TAG}: {message}");
            };

            // Setup Log
            Log.Warning = (message) => Android.Util.Log.Warn(Constants.TAG, $"{TAG}: {message}");
            Log.Info = (message) => Android.Util.Log.Info(Constants.TAG, $"{TAG}: {message}");
            Log.Verbose = (message) => Android.Util.Log.Verbose(Constants.TAG, $"{TAG}: {message}");
            Log.Debug = (message) => Android.Util.Log.Debug(Constants.TAG, $"{TAG}: {message}");

            // Setup config folder access
            Common.Config.GetConfigFolderPath = () => context.FilesDir.AbsolutePath;

            // Setup file utility functions
            Utility.FileCreate = SyncerUtility.FileCreate;
            Utility.FileDelete = SyncerUtility.FileDelete;
            Utility.FileRename = SyncerUtility.FileRename;
            Utility.FileCreateWriteStream = SyncerUtility.FileCreateWriteStream;
            Utility.FileCreateReadStream = SyncerUtility.FileCreateReadStream;
            Utility.DirectoryCreate = SyncerUtility.DirectoryCreate;
            Utility.DirectoryDelete = SyncerUtility.DirectoryDelete;

            // Disable Timestamp and FileSetTime since its not supported
            SyncerPeer.SupportTimestamp = false;
            Utility.FileSetTime = (_a, _b, _c, _d, _e) => false;
        }

        private void ProgressTracker_ProgressUpdate(object sender, ProgressUpdateEventArgs e)
        {
            if (Peer.IsSyncing())
            {
                bool indeterminate = true;
                long current = 0;
                long maximum = 0;
                double speed = 0;
                int count = 0;

                foreach (var progress in e.Progresses)
                {
                    if (!progress.Indeterminate)
                    {
                        indeterminate = false;
                        current += progress.Current;
                        maximum += progress.Maximum;
                        speed += progress.Speed;
                        count++;
                    }
                }

                speed /= count;

                GlobalProgress = new ProgressStatus(Guid.Empty, indeterminate, current, maximum, speed);

                notification.Show(GlobalProgress);

            }
            else notification.Clear();
        }

        private void Peer_FileBuilt(object sender, FileBuiltEventArgs e)
        {
            var file = Helpers.FileUtility.GetDocumentFile(e.ProfileId, e.RelativePath, false, false);
            if (file != null)
            {
                Intent scanFileIntent = new Intent(Intent.ActionMediaScannerScanFile, file.Uri);
                try { context.SendBroadcast(scanFileIntent); }
                catch (Exception ex) { Log.Error($"Peer_FileBuilt: {ex.Message}"); }
            }
            else
            {
                Log.Error("Peer_FileBuilt: Built file is null");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    progressTracker.Dispose();
                    listener.Dispose();
                    Discovery.Dispose();
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
