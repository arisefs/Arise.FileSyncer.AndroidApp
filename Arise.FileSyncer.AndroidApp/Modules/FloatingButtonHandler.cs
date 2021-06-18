using System;
using Android.Views;
using Android.Views.Animations;
using Arise.FileSyncer.AndroidApp.Activities;
using Google.Android.Material.FloatingActionButton;

namespace Arise.FileSyncer.AndroidApp.Modules
{
    internal class FloatingButtonHandler : IDisposable
    {
        private static readonly string TAG = typeof(FloatingButtonHandler).Name;

        private FloatingActionButton floatingActionButton;
        private readonly Action<View> ownerOnClicked;

        private Animation fabOpen;
        private Animation fabClose;
        private Animation fabRotateTo;
        private Animation fabRotateFrom;

        private bool rotated = false;
        private long lastClick = 0;

        public FloatingButtonHandler(MainActivity activity, Action<View> onClicked)
        {
            ownerOnClicked = onClicked;
            lastClick = GetTime();

            fabOpen = AnimationUtils.LoadAnimation(activity, Resource.Animation.fab_open);
            fabClose = AnimationUtils.LoadAnimation(activity, Resource.Animation.fab_close);
            fabRotateTo = AnimationUtils.LoadAnimation(activity, Resource.Animation.fab_rotate_45_to);
            fabRotateFrom = AnimationUtils.LoadAnimation(activity, Resource.Animation.fab_rotate_45_from);

            floatingActionButton = activity.FindViewById<FloatingActionButton>(Resource.Id.fab);
            floatingActionButton.Click += FloatingActionButton_Click;
            floatingActionButton.Visibility = ViewStates.Gone;
            floatingActionButton.Clickable = false;
        }

        public void Enable(bool enable)
        {
            if (floatingActionButton.Clickable == enable) return;

            if (enable)
            {
                floatingActionButton.Visibility = ViewStates.Visible;
                try { floatingActionButton.StartAnimation(fabOpen); }
                catch { Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to animate FAB"); }
            }
            else
            {
                try { floatingActionButton.StartAnimation(fabClose); }
                catch { Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to animate FAB"); }
            }

            floatingActionButton.Clickable = enable;
        }

        public void Rotate(bool rotate)
        {
            if (rotated == rotate) return;

            if (rotate)
            {
                try { floatingActionButton.StartAnimation(fabRotateTo); }
                catch { Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to animate FAB"); }
            }
            else
            {
                try { floatingActionButton.StartAnimation(fabRotateFrom); }
                catch { Android.Util.Log.Warn(Constants.TAG, $"{this}: Failed to animate FAB"); }
            }

            rotated = rotate;
        }

        private void FloatingActionButton_Click(object sender, EventArgs e)
        {
            long now = GetTime();
            if (now > lastClick + 300)
            {
                lastClick = now;
                ownerOnClicked((View)sender);
            }
        }

        private long GetTime()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    floatingActionButton = null;

                    fabOpen.Dispose();
                    fabClose.Dispose();
                    fabRotateTo.Dispose();
                    fabRotateFrom.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
