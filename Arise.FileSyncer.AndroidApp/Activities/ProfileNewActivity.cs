using System;
using Android.App;
using Android.Widget;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Helpers;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/act_profile_new", Theme = "@style/Theme.MyApplication.NoActionBar")]
    public class ProfileNewActivity : ProfileEditorActivity
    {
        protected override void OnEditDone()
        {
            var profile = new SyncProfile()
            {
                Key = Guid.NewGuid(),
                Name = editName.Text,
                RootDirectory = selectedUri.ToString(),
                AllowSend = cbAllowSend.Checked,
                AllowReceive = cbAllowReceive.Checked,
                AllowDelete = cbAllowReceive.Checked && cbAllowDelete.Checked,
                SkipHidden = cbSkipHidden.Checked,
            };

            Guid profileId = Guid.NewGuid();

            if (SyncerService.Instance.Peer.Profiles.AddProfile(profileId, profile))
            {
                // Save URI and permissions
                UriHelper.SaveUriWithPermissions(this, selectedUri, profileId, cbAllowReceive.Checked);

                // Notify user
                Toast.MakeText(this, Resource.String.msg_profile_created, ToastLength.Short).Show();

                // Close activity
                Finish();
            }
            else
            {
                OnError(Resource.String.error_service_profile_add_failed);
                return;
            }
        }
    }
}
