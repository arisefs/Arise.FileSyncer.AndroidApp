using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.AndroidApp.Service;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/act_profile_details", Theme = "@style/AppTheme.NoActionBar")]
    public class ProfileDetailsActivity : AppCompatActivity
    {
        private Guid profileId = Guid.Empty;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Load layout
            SetContentView(Resource.Layout.activity_profile_details);

            // Toolbar setup
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowTitleEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);

            // Get profile ID
            if (Guid.TryParse(Intent.GetStringExtra(Constants.Keys.ProfileId), out var profileId))
            {
                this.profileId = profileId;
            }
            else
            {
                OnError(Resource.String.error_profile_id_retrive);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            UpdateViews();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.profile_details_menu, menu);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    return true;

                case Resource.Id.profile_share:
                    // TODO
                    return true;

                case Resource.Id.profile_edit:
                    var intent = new Intent(this, typeof(ProfileEditActivity));
                    intent.PutExtra(Constants.Keys.ProfileId, profileId.ToString());
                    StartActivity(intent);
                    return true;

                case Resource.Id.profile_delete:
                    Button_DeleteProfile();
                    return true;

                default: return base.OnOptionsItemSelected(item);
            }
        }

        protected virtual void OnError(int resId)
        {
            string error = Resources.GetString(resId);
            Android.Util.Log.Error(Constants.TAG, $"{this}: {error}");
            var view = FindViewById(Resource.Id.root_view);
            Snackbar.Make(view, error, Snackbar.LengthLong).Show();
        }

        private void Button_DeleteProfile()
        {
            var alert = new AlertDialog.Builder(this);
            alert.SetTitle(Resource.String.dialog_profile_delete_title);
            alert.SetMessage(Resource.String.dialog_profile_delete_message);

            alert.SetPositiveButton(Resource.String.dialog_btn_delete, (senderAlert, args) =>
            {
                DeleteProfile();
            });

            alert.SetNegativeButton(Resource.String.dialog_btn_cancel, (senderAlert, args) => { });

            alert.Create().Show();
        }

        private void DeleteProfile()
        {
            if (SyncerService.Instance.Peer.RemoveProfile(profileId))
            {
                UriHelper.RemoveUriWithPermissions(this, profileId);
                Finish();
            }
        }

        private void UpdateViews()
        {
            //TODO
        }
    }
}
