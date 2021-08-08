using System;
using Android.App;
using Android.Content;
using AndroidX.Work;
using Arise.FileSyncer.Common;

namespace Arise.FileSyncer.AndroidApp.Service
{
    public class SyncerWorker : Worker
    {
        private const int ForegoundServiceTypeDataSync = 0x00000001;
        private const string UniqueId = "SyncData";

        public SyncerWorker(Context context, WorkerParameters parameters) : base(context, parameters)
        {
            Android.Util.Log.Debug(Constants.TAG, "SyncerWorker -> Created");
            SyncerNotification.CreateChannel(context);
        }

        public override Result DoWork()
        {
            Android.Util.Log.Debug(Constants.TAG, "SyncerWorker -> DoWork");
            SyncerService.Instance.ProgressUpdate += OnProgressUpdate;
            SyncerService.Instance.Run();
            return Result.InvokeSuccess();
        }

        private void OnProgressUpdate(object sender, ProgressStatus progress)
        {
            Android.Util.Log.Debug(Constants.TAG, "SyncerWorker -> OnProgressUpdate");
            SetForegroundAsync(CreateForegroundInfo(progress));
        }

        private ForegroundInfo CreateForegroundInfo(ProgressStatus progress)
        {
            Android.Util.Log.Debug(Constants.TAG, "SyncerWorker -> CreateForegroundInfo");
            Notification notification = SyncerNotification.Create(ApplicationContext, progress);
            return new ForegroundInfo(SyncerNotification.Id, notification, ForegoundServiceTypeDataSync);
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
                TimeSpan.FromMinutes(30),
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
