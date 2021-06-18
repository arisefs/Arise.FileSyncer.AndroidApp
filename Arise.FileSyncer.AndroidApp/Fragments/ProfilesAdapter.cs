using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Fragments
{
    public partial class ProfilesFragment
    {
        public class SyncProfileContainer
        {
            public Guid Id { get; }
            public string Name { get; private set; }
            public int SyncTypeRes { get; private set; }
            public DateTime LastSyncDate { get; private set; }
            public string RootDirectory { get; private set; }
            public bool HasError => CheckForErrors(Id, allowReceive);

            private readonly bool allowReceive;

            public SyncProfileContainer(Guid id, SyncProfile profile)
            {
                Id = id;
                Name = profile.Name;
                SyncTypeRes = GetTypeResId(profile.AllowSend, profile.AllowReceive);
                LastSyncDate = profile.LastSyncDate;
                RootDirectory = profile.RootDirectory;
                allowReceive = profile.AllowReceive;
            }

            public void Update(SyncProfile profile)
            {
                Name = profile.Name;
                SyncTypeRes = GetTypeResId(profile.AllowSend, profile.AllowReceive);
                LastSyncDate = profile.LastSyncDate;
                RootDirectory = profile.RootDirectory;
            }

            private static bool CheckForErrors(Guid id, bool allowReceive)
            {
                return !UriHelper.CheckUriPermissions(Application.Context, id, allowReceive);
            }

            private static int GetTypeResId(bool send, bool receive)
            {
                if (send)
                {
                    if (receive) return Resource.Drawable.baseline_vertical_align_center_white_24;
                    return Resource.Drawable.baseline_vertical_align_top_white_24;
                }
                return Resource.Drawable.baseline_vertical_align_bottom_white_24;
            }
        }

        private class ProfileViewHolder : RecyclerView.ViewHolder
        {
            public Guid Id { get; set; }
            public TextView Name { get; private set; }
            public TextView Root { get; private set; }
            public TextView LastSync { get; private set; }
            public ImageView SyncType { get; private set; }
            public ImageView Error { get; private set; }

            public ProfileViewHolder(View itemView, Action<Guid> listener) : base(itemView)
            {
                Name = itemView.FindViewById<TextView>(Resource.Id.profile_name);
                Root = itemView.FindViewById<TextView>(Resource.Id.profile_root);
                LastSync = itemView.FindViewById<TextView>(Resource.Id.profile_lastsync);
                SyncType = itemView.FindViewById<ImageView>(Resource.Id.profile_type);
                Error = itemView.FindViewById<ImageView>(Resource.Id.profile_error);

                itemView.Click += (sender, e) => listener(Id);
            }
        }

        private class ProfilesAdapter : RecyclerView.Adapter
        {
            public event EventHandler<Guid> ItemClick;

            public List<SyncProfileContainer> Profiles { get; }

            public ProfilesAdapter()
            {
                Profiles = new List<SyncProfileContainer>(0);
            }

            public override int ItemCount => Profiles.Count;

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                ProfileViewHolder viewHolder = holder as ProfileViewHolder;

                var profile = Profiles[position];

                viewHolder.Id = profile.Id;
                viewHolder.Name.Text = profile.Name;
                viewHolder.Root.Text = profile.RootDirectory;
                viewHolder.LastSync.Text = profile.LastSyncDate.ToString();
                viewHolder.SyncType.SetImageResource(profile.SyncTypeRes);
                viewHolder.Error.Visibility = profile.HasError ? ViewStates.Visible : ViewStates.Gone;
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                View itemView = LayoutInflater.From(parent.Context).
                    Inflate(Resource.Layout.itemview_profile, parent, false);
                return new ProfileViewHolder(itemView, OnItemClick);
            }

            public int FindById(Guid id)
            {
                for (int i = 0; i < Profiles.Count; i++)
                {
                    if (Profiles[i].Id == id)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private void OnItemClick(Guid profileId)
            {
                ItemClick?.Invoke(this, profileId);
            }
        }
    }
}
