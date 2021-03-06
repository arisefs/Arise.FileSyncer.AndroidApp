using System;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Arise.FileSyncer.AndroidApp.Service;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Fragments
{
    public partial class ConnectionsFragment : Fragment
    {
        private ConnectionsAdapter adapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            adapter = new ConnectionsAdapter();

            var service = SyncerService.Instance;
            service.Peer.ConnectionVerified += Peer_ConnectionVerified;
            service.Peer.ConnectionRemoved += Peer_ConnectionRemoved;

            foreach (var id in service.Peer.GetConnectionIds())
            {
                if (service.Peer.TryGetConnection(id, out var connection))
                {
                    adapter.Connections.Add(new ConnectionContainer(id, connection));
                }
            }

            adapter.NotifyDataSetChanged();

            // TODO progress update checks

        }

        public override void OnDestroy()
        {
            // Unsubscribe from service events
            var service = SyncerService.Instance;

            try
            {
                service.Peer.ConnectionVerified -= Peer_ConnectionVerified;
                service.Peer.ConnectionRemoved -= Peer_ConnectionRemoved;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to remove events: {ex.Message}");
            }

            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (container == null) return null;

            View view = inflater.Inflate(Resource.Layout.fragment_connections, container, false);

            RecyclerView recyclerView = view.FindViewById<RecyclerView>(Resource.Id.connections_recycler);
            LinearLayoutManager layoutManager = new LinearLayoutManager(recyclerView.Context);
            DividerItemDecoration divider = new DividerItemDecoration(recyclerView.Context, layoutManager.Orientation);
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.AddItemDecoration(divider);
            recyclerView.SetAdapter(adapter);

            return view;
        }

        private void Peer_ConnectionVerified(object sender, ConnectionVerifiedEventArgs e)
        {
            if (adapter != null)
            {
                if (SyncerService.Instance.Peer.TryGetConnection(e.Id, out var connection))
                {
                    Activity.RunOnUiThread(() =>
                    {
                        adapter.Connections.Add(new ConnectionContainer(e.Id, connection));
                        adapter.NotifyItemInserted(adapter.ItemCount - 1);
                    });
                }
            }
        }

        private void Peer_ConnectionRemoved(object sender, ConnectionRemovedEventArgs e)
        {
            if (adapter != null)
            {
                int index = adapter.FindById(e.Id);
                if (index != -1)
                {
                    Activity.RunOnUiThread(() =>
                    {
                        adapter.Connections.RemoveAt(index);
                        adapter.NotifyItemRemoved(index);
                    });
                }
            }
        }
    }
}
