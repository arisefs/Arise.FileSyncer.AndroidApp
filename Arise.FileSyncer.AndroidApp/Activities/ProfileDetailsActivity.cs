using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;
using Google.Android.Material.AppBar;
using Google.Android.Material.Snackbar;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/act_profile_details", Theme = "@style/Theme.MyApplication.NoActionBar")]
    public class ProfileDetailsActivity : AppCompatActivity
    {
        private const int DirectorySelectRC = 1;

        private Guid profileId = Guid.Empty;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Load layout
            SetContentView(Resource.Layout.activity_profile_details);

            // Toolbar setup
            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
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

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == DirectorySelectRC)
            {
                if (resultCode == Result.Ok)
                {
                    if (SyncerService.Instance.Peer.Profiles.GetProfile(profileId, out var profile))
                    {
                        if (data.Data != null)
                        {
                            // Remove old URI permissions
                            UriHelper.RemoveUriWithPermissions(this, profileId);

                            // Save URI and permissions
                            UriHelper.SaveUriWithPermissions(this, data.Data, profileId, profile.AllowReceive);
                        }
                    }
                    else
                    {
                        OnError(Resource.String.error_profile_details_retrive);
                    }
                }
            }
            else base.OnActivityResult(requestCode, resultCode, data);
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
            if (SyncerService.Instance.Peer.Profiles.RemoveProfile(profileId))
            {
                UriHelper.RemoveUriWithPermissions(this, profileId);
                Finish();
            }
        }

        private void UpdateViews()
        {
            // Fill out the profile details
            if (SyncerService.Instance.Peer.Profiles.GetProfile(profileId, out var profile))
            {
                var layout = FindViewById<LinearLayout>(Resource.Id.view_root);
                layout.RemoveAllViews();

                if (!UriHelper.CheckUriPermissions(this, profileId, profile.AllowReceive))
                {
                    // Show URI permission error box
                    var errorBox = LayoutInflater.Inflate(Resource.Layout.error_profile_uri, layout, false);
                    var fixButton = errorBox.FindViewById<Button>(Resource.Id.btn_fix);
                    fixButton.Click += (s, e) =>
                    {
                        /*
                        Intent directorySelectIntent = new Intent(Intent.ActionOpenDocumentTree);
                        directorySelectIntent.SetFlags(
                            ActivityFlags.GrantReadUriPermission |
                            ActivityFlags.GrantWriteUriPermission |
                            ActivityFlags.GrantPersistableUriPermission |
                            ActivityFlags.GrantPrefixUriPermission);

                        Uri treeUri = AppPrefs.GetUri(MainApplication.AppContext, profileId.ToString());
                        if (treeUri != null)
                        {
                            directorySelectIntent.PutExtra(DocumentsContract.ExtraInitialUri, treeUri);
                        }

                        StartActivityForResult(directorySelectIntent, DirectorySelectRC);
                        */
                    };
                    layout.AddView(errorBox, 0);
                }

                CreateDetailsDisplay(layout, profile);
            }
            else
            {
                OnError(Resource.String.error_profile_details_retrive);
            }
        }

        private void CreateDetailsDisplay(LinearLayout layout, SyncProfile profile)
        {
            layout.AddView(CreateSpace(16));
            AddDetailsItem(layout, Resource.String.details_profile_name, profile.Name);
            layout.AddView(CreateSpace(4));
            AddDetailsItem(layout, Resource.String.details_profile_directory, UriHelper.GetUriName(this, profile.RootDirectory));
            layout.AddView(CreateSpace(4));
            AddDetailsItem(layout, Resource.String.details_profile_synctype, GetSyncType(profile));
            layout.AddView(CreateSpace(16));
            AddDetailsItem(layout, Resource.String.details_profile_creation, profile.CreationDate.ToString());
            layout.AddView(CreateSpace(4));
            AddDetailsItem(layout, Resource.String.details_profile_lastsync, profile.LastSyncDate.ToString());
            layout.AddView(CreateSpace(16));
        }

        private Space CreateSpace(int size)
        {
            var space = new Space(this);
            space.SetMinimumHeight(AsPixel(size));
            return space;
        }
        private int AsPixel(float dps)
        {
            return (int)(dps * Resources.DisplayMetrics.Density + 0.5f);
        }

        private View AddDetailsItem(ViewGroup root, int caption, string info)
        {
            var itemView = LayoutInflater.Inflate(Resource.Layout.details_item, root, false);
            itemView.FindViewById<TextView>(Resource.Id.caption).SetText(caption);
            itemView.FindViewById<TextView>(Resource.Id.info).Text = info;
            root.AddView(itemView);
            return itemView;
        }

        private string GetSyncType(SyncProfile profile)
        {
            return Resources.GetString(GetSyncTypeRes(profile));
        }

        private static int GetSyncTypeRes(SyncProfile profile)
        {
            if (profile.AllowSend)
            {
                if (profile.AllowReceive) return Resource.String.synctype_sendreceive;
                return Resource.String.synctype_sendonly;
            }
            return Resource.String.synctype_receiveonly;
        }
    }
}
