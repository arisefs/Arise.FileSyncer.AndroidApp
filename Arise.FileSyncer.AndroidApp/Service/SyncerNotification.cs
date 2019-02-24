using Android.App;
using Android.Content;
using Android.OS;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal class SyncerNotification
    {
        private const string channelId = "syncer_notification";
        private const string channelName = "Syncer Notification Channel";

        private const int syncNotifyId = 0;

        private readonly Context context;
        private readonly NotificationManager notificationManager;

        public SyncerNotification(Context context)
        {
            this.context = context;
            notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

            // Create channel
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, channelName, NotificationImportance.Low);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        public void Show(ProgressCounter progress)
        {
            Notification.Builder notificationBuilder;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                notificationBuilder = new Notification.Builder(context, channelId);
            }
            else
            {
                // Pre-Oreo
#pragma warning disable 0618
                notificationBuilder = new Notification.Builder(context);
#pragma warning restore 0618
            }

            string rawText = context.Resources.GetString(Resource.String.notification_text);
            string notificationText = string.Format(rawText, ((progress.Maximum - progress.Current) / 1024).ToString("### ### ##0"));

            notificationBuilder
                .SetSmallIcon(Resource.Drawable.baseline_sync_24)
                .SetContentTitle(context.Resources.GetString(Resource.String.notification_title))
                .SetContentText(notificationText)
                .SetCategory(Notification.CategoryProgress)
                .SetProgress(100, (int)(progress.GetPercent() * 100), progress.Indeterminate)
                .SetOngoing(true);

            //if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            //{
            //    notificationBuilder.SetTimeoutAfter(5000);
            //}

            //context.Resources.GetString(Resource.String.notification_content_text)

            Notification notification = notificationBuilder.Build();
            notification.Priority = (int)NotificationPriority.Low;

            notificationManager.Notify(syncNotifyId, notification);
        }

        public void Clear()
        {
            notificationManager.Cancel(syncNotifyId);
        }
    }
}
