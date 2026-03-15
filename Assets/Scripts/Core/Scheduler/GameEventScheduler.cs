using UnityEngine;
using OurGame.Core.DataStructures;

namespace OurGame.Core
{
    public class GameEventScheduler : SingletonMono<GameEventScheduler>
    {
        private PriorityQueue<ScheduledEvent, long> queue =
            new PriorityQueue<ScheduledEvent, long>();

        public ScheduledEvent Schedule(long tick, System.Action action)
        {
            ScheduledEvent ev = new ScheduledEvent(tick, action);
            queue.Enqueue(ev, tick);
            return ev;
        }

        void Update()
        {
            long currentTick = TimeManager.Instance.CurrentTick;

            while (queue.Count > 0)
            {
                if (!queue.TryPeek(out var ev, out var tick))
                    return;

                if (tick > currentTick)
                    return;

                queue.Dequeue();

                if (!ev.cancelled)
                    ev.action?.Invoke();
            }
        }
    }
}