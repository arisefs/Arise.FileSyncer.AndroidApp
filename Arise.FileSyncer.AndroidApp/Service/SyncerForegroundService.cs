using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Arise.FileSyncer.Common;

namespace Arise.FileSyncer.AndroidApp.Service
{
    [Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
    class SyncerForegroundService : Android.App.Service
    {
        public static bool IsRunning = false;

        private Task syncTask = null;

        public override IBinder OnBind(Intent intent) => new Binder();

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Android.Util.Log.Info(Constants.TAG, $"{nameof(SyncerForegroundService)}: Started");
            OnProgressUpdate(this, new ProgressStatus());

            if (syncTask == null)
            {
                syncTask = Task.Run(() => {
                    SyncerService.Instance.ProgressUpdate += OnProgressUpdate;
                    SyncerService.Instance.Run();
                    SyncerService.Instance.ProgressUpdate -= OnProgressUpdate;
                    syncTask = null;
                    StopSelf();
                });
            }

            return StartCommandResult.NotSticky;
        }

        private void OnProgressUpdate(object sender, ProgressStatus e)
        {
            var notification = SyncerNotification.Create(this, e);
            
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                StartForeground(SyncerNotification.Id, notification, ForegroundService.TypeDataSync);
            else StartForeground(SyncerNotification.Id, notification);
        }

        public override void OnCreate()
        {
            IsRunning = true;
            base.OnCreate();
        }

        public override void OnDestroy()
        {
            IsRunning = false;
            base.OnDestroy();
        }

        public static void Start(Context context)
        {
            if (!IsRunning)
            {
                var serviceIntent = new Intent(context, typeof(SyncerForegroundService));
                serviceIntent.SetFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                if (OperatingSystem.IsAndroidVersionAtLeast(26)) context.StartForegroundService(serviceIntent);
                else context.StartService(serviceIntent);
            }
        }
    }
}
