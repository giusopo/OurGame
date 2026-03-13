using UnityEngine;

namespace OurGame.Core
{
public class TimeManager : SingletonMono<TimeManager>
{
    [Header("Time Settings")]
    public float realSecondsPerGameMinute = 2f; // 2 secondi reali = 1 minuto di gioco

    public int minutesPerHour = 60;
    public int hoursPerDay = 24;

    public int currentDay = 1;
    public int currentHour = 6;
    public int currentMinute = 0;

    private float minuteTimer = 0f;

    void Update()
    {
        minuteTimer += Time.deltaTime;

        if (minuteTimer >= realSecondsPerGameMinute)
        {
            minuteTimer -= realSecondsPerGameMinute;
            AdvanceMinute();
        }
    }

    void AdvanceMinute()
    {
        currentMinute++;

        if (currentMinute >= minutesPerHour)
        {
            currentMinute = 0;
            currentHour++;

            if (currentHour >= hoursPerDay)
            {
                currentHour = 0;
                currentDay++;
                GameEvents.DayPassed();
            }
        }

        Debug.Log($"Day {currentDay} {currentHour}:{currentMinute}");
    }

    public float GetCurrentTimeInMinutes()
    {
        return currentDay * 1440f + currentHour * 60f + currentMinute;
    }
}
}