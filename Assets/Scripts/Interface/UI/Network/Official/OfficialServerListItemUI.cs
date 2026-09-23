using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class OfficialServerListItemUI : MonoBehaviour
{
    [SerializeField]
    private Text nameText;
    [SerializeField]
    private Text pingText;
    [SerializeField]
    private Text versionText;
    [SerializeField]
    private Text playersCountText;
    [SerializeField]
    private Button button;
    [SerializeField]
    private Image background;
    [SerializeField]
    private Color normalColor = Color.white;
    [SerializeField]
    private Color selectedColor = Color.green;

    private string address = "";

    public void Init(OfficialServersLoader.ServerInfo serverInfo, bool isCurrentServer, Action onClick)
    {
        address = serverInfo.address;
        background.color = isCurrentServer ? selectedColor : normalColor;
        nameText.text = serverInfo.name;
        pingText.text = "(... ms)";
        playersCountText.text = serverInfo.playersCount + "/" + serverInfo.playersLimit;
#if UNITY_WEBGL
        pingText.gameObject.SetActive(false);
#endif
        versionText.text = $"(v {serverInfo.version})";
        button.onClick.AddListener(() => onClick?.Invoke());
#if !UNITY_WEBGL
        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(FetchPing(address));
        }
#endif
    }

#if !UNITY_WEBGL
    private void OnEnable()
    {
        if (pingText.text == "(... ms)" && address != "")
        {
            StopAllCoroutines();
            StartCoroutine(FetchPing(address));
        }
    }

    private IEnumerator FetchPing(string rawAddress)
    {
        string address = rawAddress.Split(':')[0];
        static bool IsDomain(string input)
        {
            if (System.Net.IPAddress.TryParse(input, out _))
                return false;
            return input.Contains('.') && !input.EndsWith(".");
        }
        string ip = address;
        if (IsDomain(address))
        {
            try
            {
                System.Net.IPHostEntry myHost = System.Net.Dns.GetHostEntry(address);
                ip = myHost.AddressList[0].ToString();
            }
            catch
            {
            }
        }
        var ping = new Ping(ip);
        yield return new WaitUntil(() => ping.isDone);
        pingText.text = $"({(ping.time < 0 ? "ping fail" : ping.time + " ms")})";
        ping.DestroyPing();
    }
#endif
}