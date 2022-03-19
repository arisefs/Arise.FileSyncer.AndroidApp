using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.AndroidApp.Fragments
{
    public partial class ConnectionsFragment
    {
        public class ConnectionContainer
        {
            public Guid Id { get; }
            public string Name { get; }

            public ConnectionContainer(Guid id, ISyncerConnection connection)
            {
                Id = id;
                Name = connection.DisplayName;
            }
        }

        private class ConnectionViewHolder : RecyclerView.ViewHolder
        {
            public TextView Name { get; private set; }

            public ConnectionViewHolder(View itemView) : base(itemView)
            {
                Name = itemView.FindViewById<TextView>(Resource.Id.connection_name);
            }
        }

        private class ConnectionsAdapter : RecyclerView.Adapter
        {
            public List<ConnectionContainer> Connections { get; }

            public ConnectionsAdapter()
            {
                Connections = new List<ConnectionContainer>(0);
            }

            public override int ItemCount => Connections.Count;

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                ConnectionViewHolder viewHolder = holder as ConnectionViewHolder;

                var connection = Connections[position];

                viewHolder.Name.Text = connection.Name;
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                View itemView = LayoutInflater.From(parent.Context).
                    Inflate(Resource.Layout.itemview_connection, parent, false);
                return new ConnectionViewHolder(itemView);
            }

            public int FindById(Guid id)
            {
                for (int i = 0; i < Connections.Count; i++)
                {
                    if (Connections[i].Id == id)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }
    }
}
