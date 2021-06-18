using System;
using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Arise.FileSyncer.AndroidApp.Activities;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Fragments
{
    public partial class ProfilesFragment : Fragment
    {
        private ProfilesAdapter adapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            adapter = new ProfilesAdapter();
            adapter.ItemClick += Adapter_ItemClick;

            // Retreive profiles and setup events
            var service = SyncerService.Instance;
            // Subscribe to service events
            service.Peer.ProfileAdded += Peer_ProfileAdded;
            service.Peer.ProfileRemoved += Peer_ProfileRemoved;
            service.Peer.ProfileChanged += Peer_ProfileChanged;

            // Fill profile adapter's list
            foreach (var profileKV in service.Config.PeerSettings.Profiles)
            {
                adapter.Profiles.Add(new SyncProfileContainer(profileKV.Key, profileKV.Value));
            }

            adapter.NotifyDataSetChanged();
        }

        public override void OnDestroy()
        {
            // Unsubscribe from service events
            var service = SyncerService.Instance;

            try
            {
                service.Peer.ProfileAdded -= Peer_ProfileAdded;
                service.Peer.ProfileRemoved -= Peer_ProfileRemoved;
                service.Peer.ProfileChanged -= Peer_ProfileChanged;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to remove events: {ex.Message}");
            }

            adapter.ItemClick -= Adapter_ItemClick;

            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (container == null) return null;

            View view = inflater.Inflate(Resource.Layout.fragment_profiles, container, false);

            RecyclerView recyclerView = view.FindViewById<RecyclerView>(Resource.Id.profiles_recycler);
            LinearLayoutManager layoutManager = new LinearLayoutManager(recyclerView.Context);
            DividerItemDecoration divider = new DividerItemDecoration(recyclerView.Context, layoutManager.Orientation);
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.AddItemDecoration(divider);
            recyclerView.SetAdapter(adapter);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            adapter.NotifyDataSetChanged();
        }

        private void Adapter_ItemClick(object sender, Guid profileId)
        {
            var intent = new Intent(Activity, typeof(ProfileDetailsActivity));
            intent.PutExtra(Constants.Keys.ProfileId, profileId.ToString());
            StartActivity(intent);
        }

        private void Peer_ProfileAdded(object sender, ProfileEventArgs e)
        {
            if (adapter != null)
            {
                Activity.RunOnUiThread(() =>
                {
                    adapter.Profiles.Add(new SyncProfileContainer(e.Id, e.Profile));
                    adapter.NotifyItemInserted(adapter.ItemCount - 1);
                });
            }
        }

        private void Peer_ProfileRemoved(object sender, ProfileEventArgs e)
        {
            if (adapter != null)
            {
                int index = adapter.FindById(e.Id);
                if (index != -1)
                {
                    Activity.RunOnUiThread(() =>
                    {
                        adapter.Profiles.RemoveAt(index);
                        adapter.NotifyItemRemoved(index);
                    });
                }
            }
        }

        private void Peer_ProfileChanged(object sender, ProfileEventArgs e)
        {
            if (adapter != null)
            {
                int index = adapter.FindById(e.Id);
                if (index != -1)
                {
                    Activity.RunOnUiThread(() =>
                    {
                        adapter.Profiles[index].Update(e.Profile);
                        adapter.NotifyItemChanged(index, this);
                    });
                }
            }
        }
    }
}
