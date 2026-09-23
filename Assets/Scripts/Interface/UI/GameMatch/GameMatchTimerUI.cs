using Mirror;
using UnityEngine.UI;
using UnityEngine;
public class GameMatchTimerUI : NetworkBehaviour
{

    [SyncVar]
    public double startTime = -1;

    [SerializeField]
    private Text timerText;

    private void Update()
    {
        if (isClient)
        {
            timerText.text = startTime > 0
                ? $"{(int)((NetworkTime.time - startTime) / 60f)}m {(int)((NetworkTime.time - startTime) % 60f)}s"
                : "0m 0s";
        }
    }

    [Server]
    public void ServerStartTimer()
    {
        startTime = NetworkTime.time;
    }

    [Server]
    public void ServerStopTimer()
    {
        startTime = -1;
    }

}