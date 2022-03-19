using Android.App;
using Android.Content;
using AndroidX.AppCompat.App;
using AndroidX.Work;
using Arise.FileSyncer.AndroidApp.Service;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MyApplication.Splash", MainLauncher = true)]
    public class SplashActivity : AppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();

            GoToMain();
        }

        private void GoToMain()
        {
            SyncerWorker.Schedule(this, ExistingPeriodicWorkPolicy.Keep);
            SyncerService.Instance.Discovery.SendDiscoveryMessage();

            var intent = new Intent(Application.Context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask);

            StartActivity(intent);
            Finish();
        }
    }
}
