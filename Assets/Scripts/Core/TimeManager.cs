using UnityEngine;

namespace OurGame.Core
{
public class TimeManager : SingletonMono<TimeManager>
{
    [Header("Time Settings")]
    // 1 tick = 1 minuto di gioco
    // realSecondsPerTick determina quanto tempo reale deve passare per avanzare di 1 tick (1 minuto di gioco)
    public float realSecondsPerTick = 2f; 
    public const int MinutesPerHour = 60;
    public const int HoursPerDay = 24;
    public const int MinutesPerDay = MinutesPerHour * HoursPerDay;

    private float tickTimer;

    public long CurrentTick { get; private set; } = 0;

    void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= realSecondsPerTick)
        {
            tickTimer -= realSecondsPerTick;
            AdvanceTick();
        }
    }

    public void SetCurrentTick(long tick)
    {
        CurrentTick = tick;
    }

    void AdvanceTick()
    {
        CurrentTick++;

        if (CurrentTick % MinutesPerDay == 0)
        {
            GameEvents.DayPassed();
        }

        Debug.Log(GetFormattedTime());
    }

    public int GetCurrentDay()
    {
        return (int)(CurrentTick / MinutesPerDay) + 1;
    }

    public int GetCurrentHour()
    {
        return (int)((CurrentTick % MinutesPerDay) / MinutesPerHour);
    }

    public int GetCurrentMinute()
    {
        return (int)(CurrentTick % MinutesPerHour);
    }

    public string GetFormattedTime()
    {
        return $"Day {GetCurrentDay()} {GetCurrentHour():00}:{GetCurrentMinute():00}";
    }
}
}