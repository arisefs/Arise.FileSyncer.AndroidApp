using Android.OS;
using Android.Support.V4.App;
using Android.Views;

namespace Arise.FileSyncer.AndroidApp.Fragments
{
    public class SettingsFragment : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (container == null) return null;

            View view = inflater.Inflate(Resource.Layout.fragment_settings, container, false);

            return view;
        }
    }
}