using System;

using Android.App;
using Android.Content;
using Android.Runtime;

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
        }
    }
}