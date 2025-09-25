using UnityEngine;

public class StatsManager1 : MonoBehaviour
{
    private float timer;
    private bool running;

    public void StartTimer()
    {
        timer = 0f;
        running = true;
    }

    public void StopAndRecord()
    {
        running = false;
        Debug.Log("Clear Time: " + timer.ToString("F2") + " วินาที");
    }

    void Update()
    {
        if (running)
            timer += Time.deltaTime;
    }
}
