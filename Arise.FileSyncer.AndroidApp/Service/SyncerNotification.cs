using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal static class SyncerNotification
    {
        public const string ChannelId = "sync_notify_id";
        public const int Id = 1;

        private const int Timeout = 10000;
        private const int ProgressMax = 100;
        private const int ByteDivider = 1000;

        public static void CreateChannel(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                string channelName = context.Resources.GetString(Resource.String.notification_channel_name);
                string channelDescription = context.Resources.GetString(Resource.String.notification_channel_description);

                var channel = new NotificationChannel(ChannelId, channelName, NotificationImportance.Low)
                {
                    Description = channelDescription
                };

                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        public static Notification Create(Context context, ProgressStatus progress)
        {
            int progressCurrent = progress.Indeterminate ? 0 : (int)(progress.GetPercent() * ProgressMax);

            // Create an explicit intent for an Activity in your app
            var intent = new Intent(context, typeof(Activities.SplashActivity));
            intent.SetFlags(ActivityFlags.NewTask);

            Notification notification = new NotificationCompat.Builder(context, ChannelId)
                .SetSmallIcon(Resource.Drawable.baseline_sync_24)
                .SetContentTitle(context.Resources.GetString(Resource.String.notification_title))
                .SetContentText(ContentText(context, progress))
                .SetCategory(NotificationCompat.CategoryProgress)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetProgress(ProgressMax, progressCurrent, progress.Indeterminate)
                .SetOngoing(true)
                .SetTimeoutAfter(Timeout)
                .SetContentIntent(PendingIntent.GetActivity(context, 0, intent, 0))
                .Build();

            return notification;
        }

        public static void Notify(Context context, Notification notification)
        {
            var notificationManager = NotificationManagerCompat.From(context);
            notificationManager.Notify(Id, notification);
        }

        public static void Clear(Context context)
        {
            var notificationManager = NotificationManagerCompat.From(context);
            notificationManager.Cancel(Id);
        }

        private static string ContentText(Context context, ProgressStatus progress)
        {
            string speedText = SpeedText(progress.Speed);
            string timeText = TimeText(context, progress);

            try
            {
                string rawNotificationText = context.Resources.GetString(Resource.String.notification_text);
                return string.Format(rawNotificationText, speedText, timeText);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(Constants.TAG, $"SyncerNotification: ContentText format error: {ex}");
                return "";
            }
        }

        private static string SpeedText(double speed)
        {
            (var speedNum, var speedLevel) = DividerCounter(speed, ByteDivider);
            return $"{speedNum:### ##0.0} {SpeedLevel(speedLevel)}/s";
        }

        private static string TimeText(Context context, ProgressStatus progress)
        {
            if (progress.Speed > 0)
            {
                string rawTimeText = context.Resources.GetString(Resource.String.notification_time);
                (var timeNum, var timeLevel) = DividerCounter((progress.Maximum - progress.Current) / progress.Speed, 60);
                return string.Format(rawTimeText, $"{timeNum:0} {context.Resources.GetString(TimeLevel(timeLevel))}");
            }
            else return "-";
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
            return level switch
            {
                0 => "B",
                1 => "KB",
                2 => "MB",
                3 => "GB",
                4 => "TB",
                _ => "PB",
            };
        }

        private static int TimeLevel(int level)
        {
            return level switch
            {
                0 => Resource.String.unit_time_seconds,
                1 => Resource.String.unit_time_minutes,
                _ => Resource.String.unit_time_hours,
            };
        }
    }
}
