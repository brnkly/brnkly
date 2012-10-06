using System;
using System.Threading;
using Brnkly.Framework.Logging;
using MvcContrib;
using MvcContrib.PortableAreas;

namespace Brnkly.Framework.Web
{
    public sealed class ApplicationHeartbeat : IEventMessage
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(1);
        private static Timer HeartbeatTimer;
        private static int HeartbeatBeingProcessed = 0;

        internal static void Start()
        {
            DisposeTimer();
            HeartbeatTimer = new Timer(SendHeartbeat, null, HeartbeatInterval, HeartbeatInterval);
        }

        internal static void End()
        {
            DisposeTimer();
        }

        private static void DisposeTimer()
        {
            var timer = HeartbeatTimer;
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        private static void SendHeartbeat(object state)
        {
            if (Interlocked.Exchange(ref HeartbeatBeingProcessed, 1) != 0)
            {
                return;
            }

            try
            {
                Bus.Instance.Send(new ApplicationHeartbeat());
            }
            catch (Exception exception)
            {
                Log.Critical(
                    exception,
                    "An ApplicationHeartbeat message handler failed.",
                    LogPriority.Application);
            }
            finally
            {
                Interlocked.Exchange(ref HeartbeatBeingProcessed, 0);
            }
        }
    }
}
