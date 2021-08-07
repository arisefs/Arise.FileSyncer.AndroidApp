using Android.App;
using Android.Content;
using Android.OS;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal static class SyncerNotification
    {
        public static string ProgressText(Context context, ProgressStatus progress)
        {
            // Speed text calc
            (var speedNum, var speedLevel) = DividerCounter(progress.Speed, 1000);
            string speedText = $"{speedNum:### ##0.0} {SpeedLevel(speedLevel)}/s";

            // Time text calc
            string timeText;
            if (progress.Speed > 0)
            {
                string rawTimeText = context.Resources.GetString(Resource.String.notification_time);
                (var timeNum, var timeLevel) = DividerCounter((progress.Maximum - progress.Current) / progress.Speed, 60);
                timeText = string.Format(rawTimeText, $"{timeNum:0} {context.Resources.GetString(TimeLevel(timeLevel))}");
            }
            else timeText = "-";

            // Notification text
            string rawNotificationText = context.Resources.GetString(Resource.String.notification_text);
            return string.Format(rawNotificationText, speedText, timeText);
        }

        public static (double, int) DividerCounter(double number, double divider)
        {
            int level = 0;

            while ((number / divider) >= 1.0)
            {
                number /= divider;
                level++;
            }

            return (number, level);
        }

        public static string SpeedLevel(int level)
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

        public static int TimeLevel(int level)
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
