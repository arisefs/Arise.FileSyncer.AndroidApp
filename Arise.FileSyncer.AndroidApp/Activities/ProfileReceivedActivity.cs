using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Common;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.Helpers;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/act_profile_received", Theme = "@style/AppTheme.NoActionBar")]
    public class ProfileReceivedActivity : ProfileEditorActivity
    {
        private Args args;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            args = Args.FromJson(Intent.GetStringExtra(Constants.Keys.ProfileReceivedArgsJson));

            if (args != null)
            {
                editName.Text = args.Name;
                editName.SetSelection(editName.Text.Length);

                cbSkipHidden.Checked = args.SkipHidden;
                cbSkipHidden.Enabled = false;
            }
            else
            {
                OnError(Resource.String.error_prec_arg_parse);
            }
        }

        protected override void OnEditDone()
        {
            if (args == null)
            {
                OnError(Resource.String.error_prec_arg_parse);
                return;
            }

            string correctPath = PathHelper.GetCorrect(editDirectory.Text, true);

            SyncProfile profile = new SyncProfile.Creator()
            {
                Key = args.Key,
                Name = editName.Text,
                RootDirectory = correctPath,
                CreationDate = args.CreationDate,
                LastSyncDate = DateTime.Now,
                Activated = true,
                AllowSend = cbAllowSend.Checked,
                AllowReceive = cbAllowReceive.Checked,
                AllowDelete = cbAllowReceive.Checked && cbAllowDelete.Checked,
                SkipHidden = args.SkipHidden,
            };

            if (SyncerService.Instance.Peer.AddProfile(args.Id, profile))
            {
                // Save URI and permissions
                UriHelper.SaveUriWithPermissions(this, selectedUri, args.Id, cbAllowReceive.Checked);

                // Start profile sync
                if (!SyncerService.Instance.Peer.SyncProfile(args.RemoteId, args.Id))
                {
                    Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to start syncing the profile");
                }

                // Notify user
                Toast.MakeText(this, Resource.String.msg_profile_accepted, ToastLength.Short).Show();

                // Close activity
                Finish();
            }
            else
            {
                OnError(Resource.String.error_service_profile_add_failed);
                return;
            }
        }

        public class Args
        {
            public Guid RemoteId { get; }

            // Profile Data
            public Guid Id { get; }
            public Guid Key { get; }
            public string Name { get; }
            public DateTime CreationDate { get; }
            public bool SkipHidden { get; }

            public Args() { }

            public Args(ProfileReceivedEventArgs args)
            {
                RemoteId = args.RemoteId;
                Id = args.Id;
                Key = args.Key;
                Name = args.Name;
                CreationDate = args.CreationDate;
                SkipHidden = args.SkipHidden;
            }

            public static Args FromJson(string json)
            {
                if (json != null)
                {
                    try
                    {
                        return Json.Deserialize<Args>(json);
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Error(Constants.TAG, $"ProfileReceivedActivity.Args: {ex.Message}");
                    }
                }

                return null;
            }
        }
    }
}
