using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Helpers;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/act_profile_edit", Theme = "@style/AppTheme.NoActionBar")]
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

                if (SyncerService.Instance.Peer.Settings.Profiles.TryGetValue(profileId, out var profile))
                {
                    editName.Text = profile.Name;
                    editName.SetSelection(editName.Text.Length);

                    editDirectory.Text = profile.RootDirectory;
                    editDirectory.SetSelection(editDirectory.Text.Length);

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
            if (!SyncerService.Instance.Peer.Settings.Profiles.TryGetValue(profileId, out var oldProfile))
            {
                OnError(Resource.String.error_profile_details_retrive);
                return;
            }

            SyncProfile profile = new SyncProfile.Creator(oldProfile)
            {
                Name = editName.Text,
                RootDirectory = PathHelper.GetCorrect(editDirectory.Text, true),
                AllowSend = cbAllowSend.Checked,
                AllowReceive = cbAllowReceive.Checked,
                AllowDelete = cbAllowReceive.Checked && cbAllowDelete.Checked,
            };

            if (SyncerService.Instance.Peer.UpdateProfile(profileId, profile))
            {
                // Remove old URI permissions
                UriHelper.RemoveUriWithPermissions(this, profileId);

                // Save URI and permissions
                UriHelper.SaveUriWithPermissions(this, selectedUri, profileId, cbAllowReceive.Checked);

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
