using UnityEngine;

public class StatsManager : MonoBehaviour
{
    private float startTime;
    private float clearTime;
    private bool running = false;

    public void StartTimer()
    {
        startTime = Time.time;
        running = true;
    }

    public void StopAndRecord()
    {
        if (running)
        {
            clearTime = Time.time - startTime; 
            running = false;
            Debug.Log("Boss Clear Time: " + clearTime.ToString("F2") + " sec");
        }
    }
}
