using System;
using System.Timers;
using Android.App;
using Android.Views;
using Android.Widget;
using Arise.FileSyncer.Core;
using Timer = System.Timers.Timer;

namespace Arise.FileSyncer.AndroidApp.Modules
{
    internal class ProgressBarUpdater : IDisposable
    {
        private const int BarMax = 500;
        private const int UpdateInterval = 500;

        private readonly Func<ProgressCounter> updateFunc;
        private readonly ProgressBar progressBar;
        private readonly Activity activity;
        private readonly Timer progressTimer;

        private bool isBarVisible;

        public ProgressBarUpdater(Activity activity, ProgressBar progressBar, Func<ProgressCounter> updateFunc)
        {
            this.progressBar = progressBar;
            this.updateFunc = updateFunc;
            this.activity = activity;

            isBarVisible = false;
            activity.RunOnUiThread(() =>
            {
                progressBar.Visibility = ViewStates.Invisible;
                progressBar.Max = BarMax;
            });

            progressTimer = new Timer();
            progressTimer.Elapsed += ProgressTimer_Elapsed;
            progressTimer.Interval = UpdateInterval;
            progressTimer.AutoReset = true;
            progressTimer.Start();
        }

        private void ProgressTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var progress = updateFunc();

                if (progress == null)
                {
                    if (isBarVisible)
                    {
                        activity.RunOnUiThread(() =>
                        {
                            progressBar.Visibility = ViewStates.Invisible;
                        });

                        isBarVisible = false;
                    }
                }
                else
                {
                    if (!isBarVisible)
                    {
                        activity.RunOnUiThread(() =>
                        {
                            progressBar.Visibility = ViewStates.Visible;
                        });

                        isBarVisible = true;
                    }

                    activity.RunOnUiThread(() =>
                    {
                        progressBar.Indeterminate = progress.Indeterminate;
                        progressBar.Progress = (int)(progress.GetPercent() * BarMax);
                    });
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(Constants.TAG, $"{this}: {ex}");
                Dispose();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    progressTimer.Dispose();
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
