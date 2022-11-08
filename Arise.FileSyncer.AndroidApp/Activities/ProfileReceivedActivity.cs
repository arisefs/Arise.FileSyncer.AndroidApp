using System;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    [Activity(Label = "@string/act_profile_received", Theme = "@style/Theme.MyApplication.NoActionBar")]
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

            if (selectedUri == null)
            {
                OnError(Resource.String.error_profile_details_root_uri);
                return;
            }

            var profile = new SyncProfile()
            {
                Key = args.Key,
                Name = editName.Text,
                RootDirectory = selectedUri.ToString(),
                CreationDate = args.CreationDate,
                LastSyncDate = DateTime.Now,
                Activated = true,
                AllowSend = cbAllowSend.Checked,
                AllowReceive = cbAllowReceive.Checked,
                AllowDelete = cbAllowReceive.Checked && cbAllowDelete.Checked,
                SkipHidden = args.SkipHidden,
            };

            if (SyncerService.Instance.Peer.Profiles.AddProfile(args.Id, profile))
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
            public Guid RemoteId { get; init; }

            // Profile Data
            public Guid Id { get; init; }
            public Guid Key { get; init; }
            public string Name { get; init; }
            public DateTime CreationDate { get; init; }
            public bool SkipHidden { get; init; }

            public Args() { }

            public Args(ProfileReceivedEventArgs args)
            {
                RemoteId = args.RemoteId;
                Id = args.ProfileShare.Id;
                Key = args.ProfileShare.Key;
                Name = args.ProfileShare.Name;
                CreationDate = args.ProfileShare.CreationDate;
                SkipHidden = args.ProfileShare.SkipHidden;
            }

            public static Args FromJson(string json)
            {
                if (json != null)
                {
                    try
                    {
                        return JsonSerializer.Deserialize<Args>(json);
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
