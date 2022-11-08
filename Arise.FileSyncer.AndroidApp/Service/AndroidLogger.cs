namespace Arise.FileSyncer.AndroidApp.Service
{
    internal class AndroidLogger : Logger
    {
        public override void Log(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Error:
                    Android.Util.Log.Error(Constants.TAG, $"SyncerLogger: {message}");
                    break;
                case LogLevel.Warning:
                    Android.Util.Log.Warn(Constants.TAG, $"SyncerLogger: {message}");
                    break;
                case LogLevel.Info:
                    Android.Util.Log.Info(Constants.TAG, $"SyncerLogger: {message}");
                    break;
                case LogLevel.Verbose:
                    Android.Util.Log.Verbose(Constants.TAG, $"SyncerLogger: {message}");
                    break;
                case LogLevel.Debug:
                    Android.Util.Log.Debug(Constants.TAG, $"SyncerLogger: {message}");
                    break;
                default:
                    Android.Util.Log.Error(Constants.TAG, $"SyncerLogger at unhandled log level: {message}");
                    break;
            }
        }
    }
}
