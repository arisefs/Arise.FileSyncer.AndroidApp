using System;
using Android.App;
using Android.Content;
using Android.Runtime;
//using Microsoft.AppCenter;
//using Microsoft.AppCenter.Analytics;
//using Microsoft.AppCenter.Crashes;

namespace Arise.FileSyncer.AndroidApp
{
    [Application]
    public class MainApplication : Application
    {
        public static Context AppContext { get; private set; }

        public MainApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            AppContext = ApplicationContext;

            //AppCenter.Start("cbc0d21c-7bce-4f58-ba64-9e538dcca5a0", typeof(Analytics), typeof(Crashes));
        }
    }
}
