using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.Splash", MainLauncher = true)]
    public class SplashActivity : AppCompatActivity
    {
        private const string MP_WES = Manifest.Permission.WriteExternalStorage;
        private const int RPC = 101;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected override void OnStart()
        {
            base.OnStart();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && CheckSelfPermission(MP_WES) != Permission.Granted)
            {
                RequestPermissions(new string[] { MP_WES }, RPC);
            }
            else
            {
                GoToMain();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RPC)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    GoToMain();
                }
                else
                {
                    FinishAffinity();
                }
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void GoToMain()
        {
            var intent = new Intent(Application.Context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask);

            StartActivity(intent);
            Finish();
        }
    }
}