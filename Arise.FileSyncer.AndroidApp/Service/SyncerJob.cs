using System.Threading.Tasks;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;

namespace Arise.FileSyncer.AndroidApp.Service
{
    [Service(Name = "com.arise.filesyncer.SyncerJob", Permission = "android.permission.BIND_JOB_SERVICE")]
    public class SyncerJob : JobService
    {
        private const int syncerJobId = 1;
        private static readonly string TAG = typeof(SyncerJob).Name;

        public static void Schedule(Context context, bool forceReschedule)
        {
            JobScheduler jobScheduler = (JobScheduler)context.GetSystemService(JobSchedulerService);

            // Check if the job has already been scheduled
            if (!forceReschedule)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    if (jobScheduler.GetPendingJob(syncerJobId) != null) return;
                }
                else
                {
                    foreach (JobInfo job in jobScheduler.AllPendingJobs)
                    {
                        if (job.Id == syncerJobId) return;
                    }
                }
            }

            // Build the job
            Java.Lang.Class javaClass = Java.Lang.Class.FromType(typeof(SyncerJob));
            ComponentName componentName = new ComponentName(context, javaClass);
            JobInfo.Builder jobBuilder = new JobInfo.Builder(syncerJobId, componentName);

            jobBuilder.SetPersisted(true);
            jobBuilder.SetPeriodic(3600000); // 3600000 = 1 Hour
            jobBuilder.SetRequiredNetworkType(NetworkType.Unmetered);
            jobBuilder.SetRequiresDeviceIdle(false);
            jobBuilder.SetBackoffCriteria(10000, BackoffPolicy.Linear);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                jobBuilder.SetRequiresBatteryNotLow(true);
            }

            // Schedule the job
            JobInfo jobInfo = jobBuilder.Build();
            int scheduleResult = jobScheduler.Schedule(jobInfo);

            if (JobScheduler.ResultFailure == scheduleResult)
            {
                Android.Util.Log.Error(Constants.TAG, $"{TAG}: Failed to schedule job! Conflicting JobInfo parameters?");
            }
#if DEBUG
            else
            {
                Android.Util.Log.Debug(Constants.TAG, $"{TAG}: Succeeded to schedule the job");
            }
#endif
        }

        public override bool OnStartJob(JobParameters jobParams)
        {
#if DEBUG
            Android.Util.Log.Debug(Constants.TAG, $"{this}: Job Started");
#endif
            Task.Factory.StartNew(() =>
            {
                SyncerService.Instance.Run(); // Run job until all sync finished
#if DEBUG
                Android.Util.Log.Debug(Constants.TAG, $"{this}: Job Finished");
#endif
                // Have to tell the JobScheduler the work is done. 
                JobFinished(jobParams, false);
            }, TaskCreationOptions.LongRunning);

            // Return true because of the asynchronous work
            return true;
        }

        public override bool OnStopJob(JobParameters jobParams)
        {
#if DEBUG
            Android.Util.Log.Debug(Constants.TAG, $"{this}: Job Force Stopped");
#endif
            // Reschedule if failed to finish work
            return true;
        }
    }
}