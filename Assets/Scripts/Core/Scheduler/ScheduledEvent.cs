using System;

namespace OurGame.Core
{

public class ScheduledEvent
{
    public long executionTick;
    public Action action;
    public bool cancelled;

    public ScheduledEvent(long tick, Action action)
    {
        this.executionTick = tick;
        this.action = action;
        cancelled = false;
    }

    public void Cancel()
    {
        cancelled = true;
    }
}

}