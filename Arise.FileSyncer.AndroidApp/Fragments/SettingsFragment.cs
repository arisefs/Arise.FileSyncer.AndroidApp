using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Arise.FileSyncer.AndroidApp.Service;

namespace Arise.FileSyncer.AndroidApp.Fragments
{
    public class SettingsFragment : Fragment
    {
        private View optionPort = null;
        private bool dialogOpen = false;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            optionPort = null;
            dialogOpen = false;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (container == null) return null;

            View view = inflater.Inflate(Resource.Layout.fragment_settings, container, false);
            optionPort = view.FindViewById(Resource.Id.option_port);
            optionPort.Click += OptionPort_Click;

            return view;
        }

        public override void OnDestroyView()
        {
            if (optionPort != null)
            {
                optionPort.Click -= OptionPort_Click;
            }

            base.OnDestroyView();
        }

        public override void OnResume()
        {
            base.OnResume();

            var portText = optionPort?.FindViewById<TextView>(Resource.Id.tv_port);
            if (portText != null)
            {
                portText.Text = SyncerService.Instance.Config.DiscoveryPort.ToString();
            }
        }

        private void OptionPort_Click(object sender, System.EventArgs e)
        {
            int port = SyncerService.Instance.Config.DiscoveryPort;
            ShowEditDialog_Integer(OptionPort_Change, Resource.String.dialog_settings_edit_port_title, port, 0, 65534);
        }

        private void OptionPort_Change(int value)
        {
            var portText = optionPort?.FindViewById<TextView>(Resource.Id.tv_port);
            if (portText != null) portText.Text = value.ToString();

            Task.Run(() => {
                var syncer = SyncerService.Instance;
                syncer.Config.DiscoveryPort = value;
                syncer.Config.Save();
                syncer.Discovery.RefreshPort();
            });
        }

        private void ShowEditDialog_Integer(Action<int> changeAction, int titleRes, int defaultValue, int minValue, int maxValue)
        {
            if (dialogOpen) return;
            dialogOpen = true;

            var alert = new Android.Support.V7.App.AlertDialog.Builder(Activity);
            alert.SetTitle(titleRes);

            View alertView = Activity.LayoutInflater.Inflate(Resource.Layout.dialog_settings_number, null);
            var editNum = alertView.FindViewById<EditText>(Resource.Id.edit_number);
            if (defaultValue != 0) editNum.Text = defaultValue.ToString();
            editNum.SelectAll();
            alert.SetView(alertView);

            alert.SetPositiveButton(Resource.String.dialog_btn_ok, (sender, args) =>
            {
                if (int.TryParse(editNum.Text, out int newValue))
                {
                    changeAction(Clamp(newValue, minValue, maxValue));
                }
            });

            alert.SetNegativeButton(Resource.String.dialog_btn_cancel, (sender, args) => { });

            var alertD = alert.Create();
            alertD.Window.SetSoftInputMode(SoftInput.StateVisible);
            alertD.DismissEvent += (sender, args) => { dialogOpen = false; };
            alertD.Show();
        }

        private static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }
}
