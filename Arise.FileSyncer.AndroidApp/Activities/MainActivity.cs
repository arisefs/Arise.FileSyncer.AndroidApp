using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Fragment.App;
using Arise.FileSyncer.AndroidApp.Fragments;
using Arise.FileSyncer.AndroidApp.Modules;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;
using Google.Android.Material.Navigation;

using ActionBarDrawerToggle = AndroidX.AppCompat.App.ActionBarDrawerToggle;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Android.App.Activity(Label = "@string/act_main", Theme = "@style/AppTheme.NoActionBar.TransStatus")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        protected int navId = Resource.Id.nav_profiles;

        private FloatingButtonHandler floatingButtonHandler;
        private ProgressBarUpdater progressBarUpdater;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);
            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            floatingButtonHandler = new FloatingButtonHandler(this, OnFabClicked);

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            // Load saved state
            if (savedInstanceState != null)
            {
                navId = savedInstanceState.GetInt("navId", navId);
            }

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);
            SelectMenuItemByNavId(navigationView);

            var progressBar = navigationView.GetHeaderView(0).FindViewById<ProgressBar>(Resource.Id.sync_progress);
            progressBarUpdater = new ProgressBarUpdater(this, progressBar, UpdateGlobalProgressBar);

            SwitchToFragment(GetFragmentByNavId(navId));
        }

        protected override void OnDestroy()
        {
            floatingButtonHandler.Dispose();
            progressBarUpdater.Dispose();

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

            SyncerService.Instance.Peer.ProfileReceived += Peer_ProfileReceived;

            SyncerJob.Schedule(this, false);
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
                SyncerService.Instance.Peer.ProfileReceived -= Peer_ProfileReceived;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to remove events: " + ex.Message);
            }

            base.OnStop();
        }

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        private void OnFabClicked(View view)
        {
            switch (navId)
            {
                case Resource.Id.nav_profiles:
                    {
                        StartActivity(new Intent(this, typeof(ProfileNewActivity)));
                    }
                    break;
                case Resource.Id.nav_connections:
                    {
                        var service = SyncerService.Instance;
                        bool switched = !service.Peer.AllowPairing;
                        service.SetAllowPairing(switched);
                        floatingButtonHandler.Rotate(switched);
                        var message = switched ? Resource.String.msg_pairing_enabled : Resource.String.msg_pairing_disabled;
                        Toast.MakeText(this, message, ToastLength.Short).Show();
                    }
                    break;
                default: break;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return base.OnOptionsItemSelected(item);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            navId = item.ItemId;
            SwitchToFragment(GetFragmentByNavId(navId));
            SupportActionBar.TitleFormatted = item.TitleFormatted;

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }

        #region Navigation and Fragments

        private void SwitchToFragment(Fragment fragment)
        {
            FragmentTransaction fragmentTx = SupportFragmentManager.BeginTransaction();
            fragmentTx.Replace(Resource.Id.fragment_container, fragment);
            fragmentTx.Commit();

            floatingButtonHandler.Enable(navId != Resource.Id.nav_settings);
            floatingButtonHandler.Rotate(navId == Resource.Id.nav_connections && SyncerService.Instance.Peer.AllowPairing == true);
        }

        private void SelectMenuItemByNavId(NavigationView navigationView)
        {
            bool selectedItem = false;

            if (navId != 0)
            {
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
            }

            if (!selectedItem && navigationView.Menu.Size() > 0)
            {
                IMenuItem item = navigationView.Menu.GetItem(0);
                item.SetChecked(true);
                navId = item.ItemId;
            }
        }

        private static Fragment GetFragmentByNavId(int navId)
        {
            switch (navId)
            {
                case Resource.Id.nav_profiles: return new ProfilesFragment();
                case Resource.Id.nav_connections: return new ConnectionsFragment();
                case Resource.Id.nav_settings: return new SettingsFragment();
                default: return new Fragment();
            }
        }

        #endregion

        #region Service

        private void Peer_ProfileReceived(object sender, ProfileReceivedEventArgs e)
        {
            var args = new ProfileReceivedActivity.Args(e);
            var intent = new Intent(this, typeof(ProfileReceivedActivity));
            intent.PutExtra(Constants.Keys.ProfileReceivedArgsJson, Json.Serialize(args));
            RunOnUiThread(() => { StartActivity(intent); });
        }

        private ISyncProgress UpdateGlobalProgressBar()
        {
            if (SyncerService.Instance.Peer.IsSyncing() == true) return SyncerService.Instance.GlobalProgress;
            else return null;
        }

        #endregion
    }
}

