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
        private View optionTimeout = null;
        private View optionPing = null;
        private View optionBuffer = null;
        private View optionChunk = null;

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
            optionTimeout = view.FindViewById(Resource.Id.option_timeout);
            optionTimeout.Click += OptionTimeout_Click;
            optionPing = view.FindViewById(Resource.Id.option_ping);
            optionPing.Click += OptionPing_Click;
            optionBuffer = view.FindViewById(Resource.Id.option_buffer);
            optionBuffer.Click += OptionBuffer_Click;
            optionChunk = view.FindViewById(Resource.Id.option_chunk);
            optionChunk.Click += OptionChunk_Click;

            return view;
        }

        public override void OnDestroyView()
        {
            if (optionPort != null) optionPort.Click -= OptionPort_Click;
            if (optionTimeout != null) optionTimeout.Click -= OptionTimeout_Click;
            if (optionPing != null) optionPing.Click -= OptionPing_Click;
            if (optionBuffer != null) optionBuffer.Click -= OptionBuffer_Click;
            if (optionChunk != null) optionChunk.Click -= OptionChunk_Click;

            base.OnDestroyView();
        }

        public override void OnResume()
        {
            base.OnResume();

            var config = SyncerService.Instance.Config;

            var portText = optionPort?.FindViewById<TextView>(Resource.Id.tv_port);
            if (portText != null) portText.Text = config.DiscoveryPort.ToString();
            var timeoutText = optionTimeout?.FindViewById<TextView>(Resource.Id.tv_timeout);
            if (timeoutText != null) timeoutText.Text = config.PeerSettings.ProgressTimeout.ToString();
            var pingText = optionPing?.FindViewById<TextView>(Resource.Id.tv_ping);
            if (pingText != null) pingText.Text = config.PeerSettings.PingInterval.ToString();
            var bufferText = optionBuffer?.FindViewById<TextView>(Resource.Id.tv_buffer);
            if (bufferText != null) bufferText.Text = config.PeerSettings.BufferSize.ToString();
            var chunkText = optionChunk?.FindViewById<TextView>(Resource.Id.tv_chunk);
            if (chunkText != null) chunkText.Text = config.PeerSettings.ChunkRequestCount.ToString();
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

        private void OptionTimeout_Click(object sender, EventArgs e)
        {
            int timeout = SyncerService.Instance.Config.PeerSettings.ProgressTimeout;
            ShowEditDialog_Integer(OptionTimeout_Change, Resource.String.dialog_settings_edit_timeout_title, timeout, 1000, 3600000);
        }

        private void OptionTimeout_Change(int value)
        {
            var timeoutText = optionTimeout?.FindViewById<TextView>(Resource.Id.tv_timeout);
            if (timeoutText != null) timeoutText.Text = value.ToString();

            Task.Run(() => {
                var syncer = SyncerService.Instance;
                syncer.Config.PeerSettings.ProgressTimeout = value;
                syncer.Config.Save();
            });
        }

        private void OptionPing_Click(object sender, EventArgs e)
        {
            int ping = SyncerService.Instance.Config.PeerSettings.PingInterval;
            ShowEditDialog_Integer(OptionPing_Change, Resource.String.dialog_settings_edit_ping_title, ping, 1000, 3600000);
        }

        private void OptionPing_Change(int value)
        {
            var pingText = optionPing?.FindViewById<TextView>(Resource.Id.tv_ping);
            if (pingText != null) pingText.Text = value.ToString();

            Task.Run(() => {
                var syncer = SyncerService.Instance;
                syncer.Config.PeerSettings.PingInterval = value;
                syncer.Config.Save();
            });
        }

        private void OptionBuffer_Click(object sender, EventArgs e)
        {
            int buffer = SyncerService.Instance.Config.PeerSettings.BufferSize;
            ShowEditDialog_Integer(OptionBuffer_Change, Resource.String.dialog_settings_edit_buffer_title, buffer, 256, 1048576);
        }

        private void OptionBuffer_Change(int value)
        {
            var bufferText = optionBuffer?.FindViewById<TextView>(Resource.Id.tv_buffer);
            if (bufferText != null) bufferText.Text = value.ToString();

            Task.Run(() => {
                var syncer = SyncerService.Instance;
                syncer.Config.PeerSettings.BufferSize = value;
                syncer.Config.Save();
            });
        }

        private void OptionChunk_Click(object sender, EventArgs e)
        {
            int chunk = SyncerService.Instance.Config.PeerSettings.ChunkRequestCount;
            ShowEditDialog_Integer(OptionChunk_Change, Resource.String.dialog_settings_edit_chunk_title, chunk, 1, 128);
        }

        private void OptionChunk_Change(int value)
        {
            var chunkText = optionChunk?.FindViewById<TextView>(Resource.Id.tv_chunk);
            if (chunkText != null) chunkText.Text = value.ToString();

            Task.Run(() => {
                var syncer = SyncerService.Instance;
                syncer.Config.PeerSettings.ChunkRequestCount = value;
                syncer.Config.Save();
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
