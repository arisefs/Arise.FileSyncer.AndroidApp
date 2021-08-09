using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.DocumentFile.Provider;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Helpers;
using Uri = Android.Net.Uri;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/act_profile_edit", Theme = "@style/Theme.MyApplication.NoActionBar")]
    public class ProfileEditActivity : ProfileEditorActivity
    {
        private Guid profileId = Guid.Empty;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Get profile ID
            if (Guid.TryParse(Intent.GetStringExtra(Constants.Keys.ProfileId), out var profileId))
            {
                this.profileId = profileId;
                

                if (SyncerService.Instance.Peer.Profiles.GetProfile(profileId, out var profile))
                {
                    editName.Text = profile.Name;
                    editName.SetSelection(editName.Text.Length);

                    selectedUri = Uri.Parse(profile.RootDirectory);
                    if (selectedUri != null)
                    {
                        try
                        {
                            var rootTree = DocumentFile.FromTreeUri(this, selectedUri);
                            editDirectory.Text = rootTree.Name;
                            editDirectory.SetSelection(editDirectory.Text.Length);
                        }
                        catch (Exception)
                        {
                            OnError(Resource.String.error_profile_details_root_uri);
                        }
                    }
                    else OnError(Resource.String.error_profile_details_root_uri);

                    cbAllowSend.Checked = profile.AllowSend;
                    cbAllowReceive.Checked = profile.AllowReceive;
                    cbAllowDelete.Checked = profile.AllowDelete;

                    cbSkipHidden.Checked = profile.SkipHidden;
                    cbSkipHidden.Enabled = false;
                }
                else
                {
                    OnError(Resource.String.error_profile_details_retrive);
                }
            }
            else
            {
                OnError(Resource.String.error_profile_id_retrive);
            }
        }

        protected override void OnEditDone()
        {
            if (!SyncerService.Instance.Peer.Profiles.GetProfile(profileId, out var oldProfile))
            {
                OnError(Resource.String.error_profile_details_retrive);
                return;
            }

            if (selectedUri == null)
            {
                OnError(Resource.String.error_profile_details_root_uri);
                return;
            }

            var profile = new SyncProfile(oldProfile)
            {
                Name = editName.Text,
                RootDirectory = selectedUri.ToString(),
                AllowSend = cbAllowSend.Checked,
                AllowReceive = cbAllowReceive.Checked,
                AllowDelete = cbAllowReceive.Checked && cbAllowDelete.Checked,
            };

            if (SyncerService.Instance.Peer.Profiles.UpdateProfile(profileId, profile))
            {
                if (selectedUri.ToString() != AppPrefs.GetUri(this, profileId.ToString())?.ToString())
                {
                    Android.Util.Log.Info(Constants.TAG, "Profile root URI updated");

                    // Remove old URI permissions
                    UriHelper.RemoveUriWithPermissions(this, profileId);

                    // Save URI and permissions
                    UriHelper.SaveUriWithPermissions(this, selectedUri, profileId, cbAllowReceive.Checked);
                }

                // Notify user
                Toast.MakeText(this, Resource.String.msg_profile_updated, ToastLength.Short).Show();

                // Close activity
                Finish();
            }
            else
            {
                OnError(Resource.String.error_service_profile_update_failed);
                return;
            }
        }
    }
}
