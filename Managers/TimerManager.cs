using System;
using System.Timers;

namespace LuckyStars.Managers
{
    public class TimerManager
    {
        private System.Timers.Timer? timer;
        private System.Timers.Timer? idleTimer;
        private bool pendingRefresh = false;
        private const int IdleTimeout = 60000;
        private readonly Action showMedia;
        private readonly Action loadMediaPaths;

        public TimerManager(Action showMedia, Action loadMediaPaths)
        {
            this.showMedia = showMedia;
            this.loadMediaPaths = loadMediaPaths;
            InitializeIdleTimer();
        }

        public void SetupTimer(int interval)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= OnTimedEvent;
                timer.Dispose();
                timer = null;
            }
            if (interval == 0)
            {
                return;
            }
            timer = new System.Timers.Timer(interval)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += OnTimedEvent;
        }

        private void OnTimedEvent(object? source, ElapsedEventArgs e)
        {
            showMedia();
        }

        private void InitializeIdleTimer()
        {
            idleTimer = new System.Timers.Timer(IdleTimeout)
            {
                AutoReset = false
            };
            idleTimer.Elapsed += OnIdleTimeout;
        }

        public void ResetIdleTimer()
        {
            if (idleTimer != null)
            {
                idleTimer.Stop();
                idleTimer.Start();
            }
            pendingRefresh = true;
        }

        private void OnIdleTimeout(object? sender, ElapsedEventArgs e)
        {
            if (pendingRefresh)
            {
                loadMediaPaths();
                pendingRefresh = false;
            }
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
            if (idleTimer != null)
            {
                idleTimer.Stop();
                idleTimer.Dispose();
                idleTimer = null;
            }
        }
    }
}
