using Android.Content;
using AndroidX.Work;

namespace Arise.FileSyncer.AndroidApp.Service
{
    public class SyncerWorker : Worker
    {
        private const string UniqueId = "SyncData";

        public SyncerWorker(Context context, WorkerParameters parameters) : base(context, parameters)
        {
            Android.Util.Log.Debug(Constants.TAG, "SyncerWorker -> Created");
            SyncerNotification.CreateChannel(context);
        }

        public override Result DoWork()
        {
            Android.Util.Log.Debug(Constants.TAG, "SyncerWorker -> DoWork");
            SyncerForegroundService.Start(ApplicationContext);
            return Result.InvokeSuccess();
        }

        public static void Schedule(Context context, ExistingPeriodicWorkPolicy policy)
        {
            Android.Util.Log.Debug(Constants.TAG, "SyncerWorker -> Schedule");
            Constraints constraints = new Constraints.Builder()
                .SetRequiredNetworkType(NetworkType.Unmetered)
                .SetRequiresBatteryNotLow(true)
                .SetRequiresDeviceIdle(true)
                .Build();

            PeriodicWorkRequest workRequest = new PeriodicWorkRequest.Builder(
                typeof(SyncerWorker),
                TimeSpan.FromMinutes(60),
                TimeSpan.FromMinutes(20))
                .SetConstraints(constraints)
                .Build();

            WorkManager.GetInstance(context).EnqueueUniquePeriodicWork(
                UniqueId,
                policy,
                workRequest);
        }
    }
}
