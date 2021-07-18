using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Arise.FileSyncer.AndroidApp.Service;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MyApplication.Splash", MainLauncher = true)]
    public class SplashActivity : AppCompatActivity
    {
        private const string MP_WES = Manifest.Permission.WriteExternalStorage;
        private const int RPC = 101;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected override void OnResume()
        {
            base.OnResume();

            TryGoToMain();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RPC)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    TryGoToMain();
                }
                else
                {
                    FinishAffinity();
                }
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void TryGoToMain()
        {
            if (!CheckPermissions()) return;
            //if (!CheckBatteryOptimizations()) return;

            GoToMain();
        }

        private bool CheckPermissions()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && CheckSelfPermission(MP_WES) != Permission.Granted)
            {
                RequestPermissions(new string[] { MP_WES }, RPC);
                return false;
            }

            return true;
        }

        private bool CheckBatteryOptimizations()
        {
            if (GetSystemService(PowerService) is PowerManager powerManager)
            {
                if (!powerManager.IsIgnoringBatteryOptimizations(PackageName))
                {
                    var intent = new Intent(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
                    StartActivity(intent);
                    return false;
                }
            }
            else Log.Error("Failed to get PowerManager");

            return true;
        }

        private void GoToMain()
        {
            SyncerService.Instance.Discovery.SendDiscoveryMessage();

            var intent = new Intent(Application.Context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask);

            StartActivity(intent);
            Finish();
        }
    }
}
