using Android.App;
using Android.Content;
using Android.OS;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal class SyncerNotification
    {
        private const string channelId = "syncer_notification";
        private const string channelName = "Syncer Notification Channel";
        private const string wakelockTag = "syncer_wakelock";

        private const int syncNotifyId = 0;

        private readonly Context context;
        private readonly NotificationManager notificationManager;
        private readonly PowerManager.WakeLock wakeLock;

        public SyncerNotification(Context context)
        {
            this.context = context;
            notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

            // Get wake lock
            PowerManager powerManager = context.GetSystemService(Context.PowerService) as PowerManager;
            if (powerManager == null) Log.Error("Failed to get PowerManager");
            wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, wakelockTag);

            // Create channel
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, channelName, NotificationImportance.Low);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        public void Show(ProgressStatus progress)
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

            // Speed text calc
            (var speedNum, var speedLevel) = DividerCounter(progress.Speed, 1000);
            string speedText = $"{speedNum.ToString("### ##0.0")} {SpeedLevel(speedLevel)}/s";

            // Time text calc
            string timeText;
            if (progress.Speed > 0)
            {
                string rawTimeText = context.Resources.GetString(Resource.String.notification_time);
                (var timeNum, var timeLevel) = DividerCounter((progress.Maximum - progress.Current) / progress.Speed, 60);
                timeText = string.Format(rawTimeText, $"{timeNum.ToString("0")} {context.Resources.GetString(TimeLevel(timeLevel))}");
            }
            else timeText = "-";

            // Notification text
            string rawNotificationText = context.Resources.GetString(Resource.String.notification_text);
            string notificationText = string.Format(rawNotificationText, speedText, timeText);

            // Build notification
            notificationBuilder
                .SetSmallIcon(Resource.Drawable.baseline_sync_24)
                .SetContentTitle(context.Resources.GetString(Resource.String.notification_title))
                .SetContentText(notificationText)
                .SetCategory(Notification.CategoryProgress)
                .SetProgress(100, (int)(progress.Indeterminate ? 0 : progress.GetPercent() * 100), progress.Indeterminate)
                .SetOngoing(true);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                notificationBuilder.SetTimeoutAfter(5000);
            }

            Notification notification = notificationBuilder.Build();
            notification.Priority = (int)NotificationPriority.Low;

            notificationManager.Notify(syncNotifyId, notification);
            if (!wakeLock.IsHeld) wakeLock.Acquire();
        }

        public void Clear()
        {
            notificationManager.Cancel(syncNotifyId);
            if (wakeLock.IsHeld) wakeLock.Release();
        }

        private static (double, int) DividerCounter(double number, double divider)
        {
            int level = 0;

            while ((number / divider) >= 1.0)
            {
                number /= divider;
                level++;
            }

            return (number, level);
        }

        private static string SpeedLevel(int level)
        {
            switch (level)
            {
                case 0: return "B";
                case 1: return "KB";
                case 2: return "MB";
                case 3: return "GB";
                case 4: return "TB";
                default: return "PB";
            }
        }

        private static int TimeLevel(int level)
        {
            switch (level)
            {
                case 0: return Resource.String.unit_time_seconds;
                case 1: return Resource.String.unit_time_minutes;
                default: return Resource.String.unit_time_hours;
            }
        }
    }
}
