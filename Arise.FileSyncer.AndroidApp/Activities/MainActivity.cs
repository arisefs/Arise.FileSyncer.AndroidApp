using System;
using System.Text.Json;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Fragment.App;
using AndroidX.Work;
using Arise.FileSyncer.AndroidApp.Fragments;
using Arise.FileSyncer.AndroidApp.Modules;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;
using Google.Android.Material.AppBar;
using Google.Android.Material.Navigation;
using ActionBarDrawerToggle = AndroidX.AppCompat.App.ActionBarDrawerToggle;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Android.App.Activity(Label = "@string/act_main", Theme = "@style/Theme.MyApplication.NoActionBar")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        protected int navId = Resource.Id.nav_profiles;

        private DrawerLayout drawerLayout;
        private FloatingButtonHandler floatingButtonHandler;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);
            InitializeToolbar();
            floatingButtonHandler = new(this, OnFabClicked);

            if (savedInstanceState != null)
            {
                navId = savedInstanceState.GetInt("navId", navId);
            }

            InitializeNavigation();
        }

        private void InitializeToolbar()
        {
            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            var toggle = new ActionBarDrawerToggle(this, drawerLayout, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawerLayout.AddDrawerListener(toggle);
            toggle.SyncState();
        }

        private void InitializeNavigation()
        {
            var navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);
            SelectMenuItemByNavId(navigationView);
            SwitchToFragment(GetFragmentByNavId(navId));
        }

        protected override void OnDestroy()
        {
            floatingButtonHandler.Dispose();
            base.OnDestroy();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt("navId", navId);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnStart()
        {
            base.OnStart();

            SyncerService.Instance.Peer.Profiles.ProfileReceived += OnProfileReceived;
        }

        protected override void OnResume()
        {
            base.OnResume();

            SyncerService.Instance.Discovery.SendDiscoveryMessage();
        }

        protected override void OnStop()
        {
            try
            {
                SyncerService.Instance.Peer.Profiles.ProfileReceived -= OnProfileReceived;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to remove events: " + ex.Message);
            }

            base.OnStop();
        }

        private void OnFabClicked(View view)
        {
            switch (navId)
            {
                case Resource.Id.nav_profiles:
                    StartActivity(new Intent(this, typeof(ProfileNewActivity)));
                    break;
                case Resource.Id.nav_connections:
                    {
                        var service = SyncerService.Instance;
                        bool switched = !service.Peer.AllowPairing;
                        service.SetAllowPairing(switched);
                        floatingButtonHandler.Rotate(switched);
                        var message = switched ? Resource.String.msg_pairing_enabled : Resource.String.msg_pairing_disabled;
                        Toast.MakeText(this, message, ToastLength.Short).Show();
                        break;
                    }
                default:
                    Android.Util.Log.Debug(Constants.TAG, $"{this}: FAB clicked while on an unhandled nav page");
                    break;
            }
        }

        private void OnProfileReceived(object sender, ProfileReceivedEventArgs e)
        {
            var args = new ProfileReceivedActivity.Args(e);
            var intent = new Intent(this, typeof(ProfileReceivedActivity));
            intent.PutExtra(Constants.Keys.ProfileReceivedArgsJson, JsonSerializer.Serialize(args));
            RunOnUiThread(() => { StartActivity(intent); });
        }

        #region Navigation and Fragments
        public override void OnBackPressed()
        {
            // Only close the navigation drawer on BackPressed if it is open
            if (drawerLayout.IsDrawerOpen(GravityCompat.Start)) drawerLayout.CloseDrawer(GravityCompat.Start);
            else base.OnBackPressed();
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            navId = item.ItemId;
            SwitchToFragment(GetFragmentByNavId(navId));
            SupportActionBar.TitleFormatted = item.TitleFormatted;

            drawerLayout.CloseDrawer(GravityCompat.Start);
            return true;
        }

        private void SwitchToFragment(Fragment fragment)
        {
            FragmentTransaction fragmentTx = SupportFragmentManager.BeginTransaction();
            fragmentTx.Replace(Resource.Id.fragment_container, fragment);
            fragmentTx.Commit();

            floatingButtonHandler.Enable(navId == Resource.Id.nav_profiles || navId == Resource.Id.nav_connections);
            floatingButtonHandler.Rotate(navId == Resource.Id.nav_connections && SyncerService.Instance.Peer.AllowPairing == true);
        }

        private static Fragment GetFragmentByNavId(int navId)
        {
            return navId switch
            {
                Resource.Id.nav_profiles => new ProfilesFragment(),
                Resource.Id.nav_connections => new ConnectionsFragment(),
                Resource.Id.nav_settings => new SettingsFragment(),
                _ => new Fragment(),
            };
        }

        private void SelectMenuItemByNavId(NavigationView navigationView)
        {
            bool selectedItem = false;

            for (int i = 0; i < navigationView.Menu.Size(); i++)
            {
                IMenuItem item = navigationView.Menu.GetItem(i);

                if (item.ItemId == navId)
                {
                    item.SetChecked(true);
                    selectedItem = true;
                    SupportActionBar.TitleFormatted = item.TitleFormatted;
                    break;
                }
            }

            if (!selectedItem && navigationView.Menu.Size() > 0)
            {
                Android.Util.Log.Warn(Constants.TAG, $"{this}: Invalid nav menu item selected");
                var item = navigationView.Menu.GetItem(0);
                item.SetChecked(true);
                navId = item.ItemId;
            }
        }
        #endregion
    }
}

