using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Work;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Service
{
    public class SyncerWorker : Worker
    {
        private const string ChannelId = "sync_notify_id";
        private const string ChannelName = "Sync Progress";
        private const int SyncNotificationId = 1;
        private const int ForegoundServiceTypeDataSync = 0x00000001;
        private const int ProgressMax = 100;

        public SyncerWorker(Context context, WorkerParameters parameters) : base(context, parameters)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
                {
                    Description = "Progress of the file sync"
                };

                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        public override Result DoWork()
        {
            SetForegroundAsync(CreateForegroundInfo(new ProgressStatus(Guid.Empty, true, 0, 0, 0)));
            SyncerService.Instance.ProgressUpdate += OnProgressUpdate;
            SyncerService.Instance.Run();
            return Result.InvokeSuccess();
        }

        private void OnProgressUpdate(object sender, ProgressStatus progress)
        {
            SetForegroundAsync(CreateForegroundInfo(progress));
        }

        private ForegroundInfo CreateForegroundInfo(ProgressStatus progress)
        {
            Context context = ApplicationContext;

            string notificationText = SyncerNotification.ProgressText(context,progress);
            int progressCurrent = progress.Indeterminate ? 0 : (int)(progress.GetPercent() * ProgressMax);

            Notification notification = new NotificationCompat.Builder(context, ChannelId)
                .SetSmallIcon(Resource.Drawable.baseline_sync_24)
                .SetContentTitle(context.Resources.GetString(Resource.String.notification_title))
                .SetContentText(notificationText)
                .SetCategory(NotificationCompat.CategoryProgress)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetProgress(ProgressMax, progressCurrent, progress.Indeterminate)
                .SetOngoing(true)
                .Build();

            return new ForegroundInfo(SyncNotificationId, notification, ForegoundServiceTypeDataSync);
        }

        public static void Schedule(Context context)
        {
            Constraints constraints = new Constraints.Builder()
                .SetRequiredNetworkType(NetworkType.Unmetered)
                .SetRequiresBatteryNotLow(true)
                .SetRequiresDeviceIdle(true)
                .Build();

            PeriodicWorkRequest workRequest = new PeriodicWorkRequest.Builder(
                typeof(SyncerWorker),
                TimeSpan.FromHours(1),
                TimeSpan.FromMinutes(15))
                .SetConstraints(constraints)
                .Build();

            WorkManager.GetInstance(context).EnqueueUniquePeriodicWork(
                "SyncData",
                ExistingPeriodicWorkPolicy.Keep,
                workRequest);
        }
    }
}
