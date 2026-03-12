using UnityEngine;

namespace OurGame.Core
{
    /// <summary>
    /// Manages in-game time progression.
    /// Converts real time to game time and triggers day/hour events.
    /// </summary>
public class TimeManager : SingletonMono<TimeManager>
{
    [Header("Time Settings")]
    public float realSecondsPerGameHour = 5; // 5 real seconds = 1 in-game hour
    public int hoursPerDay = 24;

    public int currentDay = 1;
    public int currentHour = 6;

    private float hourTimer = 0f; 

    void Update()
    {
        hourTimer += Time.deltaTime;

        if (hourTimer >= realSecondsPerGameHour)
        {
            hourTimer -= realSecondsPerGameHour;
            AdvanceHour();
        }
    }

    void AdvanceHour()
    {
        currentHour++;

        if (currentHour >= hoursPerDay)
        {
            currentHour = 0;
            AdvanceDay();
        }

        Debug.Log($"Day {currentDay} - Hour {currentHour}");
    }

    void AdvanceDay()
    {
        currentDay++;
        OurGame.Core.GameEvents.DayPassed();

        Debug.Log("New Day: " + currentDay);
    }

    public float GetCurrentTimeInHours()
    {
        return currentDay * 24f + currentHour + hourTimer / realSecondsPerGameHour;
    }
}

}