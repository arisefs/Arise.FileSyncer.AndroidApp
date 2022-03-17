using Android.App;
using Android.Content;
using AndroidX.Work;
using Arise.FileSyncer.AndroidApp.Service;

namespace Arise.FileSyncer.AndroidApp.BroadcastReceivers
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionMyPackageReplaced })]
    public class PackageReplacedReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            SyncerWorker.Schedule(context, ExistingPeriodicWorkPolicy.Replace);
        }
    }
}
