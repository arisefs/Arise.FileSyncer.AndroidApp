using Android.Content;
using Android.Media;
using Android.OS;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Common.Helpers;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.FileSync;
using Arise.FileSyncer.Core.Peer;
using Java.Net;
using System.Net.Sockets;
using System.Net;
using Android.App;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal class SyncerService : IDisposable
    {
        private const string TAG = "SyncerService";

        private static readonly Lazy<SyncerService> instance = new(() => new SyncerService(Application.Context), true);
        public static SyncerService Instance => instance.Value;

        public SyncerConfig Config { get; }
        public SyncerPeer Peer { get; }
        public NetworkDiscovery Discovery { get; }

        public event EventHandler<ProgressStatus> ProgressUpdate;

        private readonly NetworkListener listener;
        private readonly KeyConfig keyConfig;

        private readonly Context context;
        private readonly ProgressTracker progressTracker;

        public SyncerService(Context context)
        {
            this.context = context;

            SetupOSMethods();
            SyncerNotification.CreateChannel(context);
            SyncerNotification.Clear(context);

            // Load config
            Config = new SyncerConfig();
            LoadResult loadResult = Config.Load(CreatePeerSettings(), out var configData);
            if (loadResult != LoadResult.Loaded)
            {
                if (loadResult == LoadResult.Created) Log.Info("Created new config");
                if (Config.Save(configData)) Log.Info("Saved config after create/upgrade");
                else Log.Error("Failed to save config after create/upgrade");
            }

            // Load key
            keyConfig = new KeyConfig(); // TODO?: keysize: 1024
            loadResult = keyConfig.Load();
            if (loadResult != LoadResult.Loaded)
            {
                if (loadResult == LoadResult.Created) Log.Info("Created new key");
                if (keyConfig.Save()) Log.Info("Saved key after create/upgrade");
                else Log.Error("Failed to save key after create/upgrade");
            }

            // Create managers from loaded config
            DeviceKeyManager deviceKeyManager = new(configData.DeviceKeys);
            ProfileManager profileManager = new(configData.Profiles);

            // Load syncing and connection handler classes
            Peer = new SyncerPeer(configData.PeerSettings, deviceKeyManager, profileManager);
            listener = new NetworkListener(Peer, keyConfig, Config.ListenerAddressFamily);
            Discovery = new NetworkDiscovery(Config, Peer, listener);

            // Subscribe to save events
            Peer.NewPairAdded += (s, e) => Config.Save(Peer);
            Peer.Profiles.ProfileAdded += (s, e) => Config.Save(Peer);
            Peer.Profiles.ProfileRemoved += (s, e) => Config.Save(Peer);
            Peer.Profiles.ProfileChanged += (s, e) => Task.Run(() => Config.Save(Peer));
            Peer.FileBuilt += Peer_FileBuilt;

            // Auto accept pair requests
            Peer.PairingRequest += (s, e) =>
            {
                Log.Info($"Auto accepting pair: {e.DisplayName}");
                e.ResultCallback(true);
            };

            Log.Verbose($"Initialized");
            Log.Verbose($"Listener IP: {listener.LocalEndpoint}");

            progressTracker = new ProgressTracker(Peer, 1000, 5);
            progressTracker.ProgressUpdate += ProgressTracker_ProgressUpdate;
        }

        private static SyncerPeerSettings CreatePeerSettings()
        {
            return new SyncerPeerSettings(Guid.NewGuid(), $"{Build.Manufacturer} {Build.Model}", false);
        }

        public void Run()
        {
            for (int i = 0; i < 5; i++)
            {
                // Send out discovery messages
                Discovery.SendDiscoveryMessage();
                Thread.Sleep(1000);
            }

            // Check sync state
            while (Peer.IsSyncing())
            {
                // Wait some time to allow it get into sync state
                Thread.Sleep(5000);
                Discovery.SendDiscoveryMessage();
            }

            Log.Debug($"Sync finished or not started");
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
                //var properties = new Dictionary<string, string>
                //{
                //    { "Message", message }
                //};
                //Analytics.TrackEvent("Internal Error", properties);
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
            Utility.GenerateTree = SyncerUtility.GenerateTreeAndroid;

            // Disable FileSetTime since its not supported
            Utility.FileSetTime = (_, _, _, _) => false;

            // Custom local ip resolver
            NetworkHelper.GetLocalIPAddress = GetLocalIpAddress;
        }

        public static IPAddress GetLocalIpAddress(AddressFamily addressFamily)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    var interfaces = NetworkInterface.NetworkInterfaces;
                    while (interfaces.HasMoreElements)
                    {
                        NetworkInterface networkInterface = interfaces.NextElement() as NetworkInterface;
                        var addresses = networkInterface.InetAddresses;
                        while (addresses.HasMoreElements)
                        {
                            InetAddress address = addresses.NextElement() as InetAddress;
                            if (!address.IsLoopbackAddress && address.IsSiteLocalAddress)
                            {
                                if (IPAddress.TryParse(address.HostAddress, out IPAddress ipAddress))
                                {
                                    if (ipAddress.AddressFamily == addressFamily)
                                    {
                                        return ipAddress;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return IPAddress.Any;
            });
            task.Wait();
            return task.Result;
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

                var overallProgress = new ProgressStatus(Guid.Empty, indeterminate, current, maximum, speed);
                SyncerForegroundService.Start(context);
                ProgressUpdate?.Invoke(this, overallProgress);
            }
            else SyncerNotification.Clear(context);
        }

        private void Peer_FileBuilt(object sender, FileBuiltEventArgs e)
        {
            var file = Helpers.FileUtility.GetDocumentFile(e.RootPath, e.RelativePath, false, false);
            if (file != null)
            {
                var path = Helpers.FileUtility.GetFullPathFromTreeUri(file.Uri);
                var mime = Helpers.FileUtility.GetMimeType(path);
                MediaScannerConnection.ScanFile(context, new[] { path }, new[] { mime }, null);
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
