using System.Collections;
using TMPro;
using UnityEngine;
public class TankMessageDisplay : MonoBehaviour
{

    [Header("Tank")]

    [SerializeField]
    private NetworkTank tank;

    [Header("Last Message Display")]

    [SerializeField]
    public GameObject messageObject;
    [SerializeField]
    private TextMeshPro messageText;
    [SerializeField]
    private float messageDisplayDuration;

    private void Start()
    {
        HideMessage();
    }

    private void OnEnable()
    {
        NetworkChat.Singleton.ClientOnMessageAdd += OnMessageAdd;
    }

    private void OnDisable()
    {
        NetworkChat.Singleton.ClientOnMessageAdd -= OnMessageAdd;
    }

    private void OnMessageAdd(int index)
    {
        if (tank.netId == 0)
            return;
        var message = NetworkChat.Singleton.Messages[index];
        if (message.tankNetId != 0 && message.type == NetworkChat.MessageType.UserMessage && message.tankNetId == tank.netId)
            ShowMessage(message);
    }

    private void ShowMessage(NetworkChat.Message message)
    {
        messageObject.SetActive(true);
        messageText.text = message.text;
        StopAllCoroutines();
        StartCoroutine(HideMessageAfterDuration());
    }

    private void HideMessage()
    {
        messageText.text = "";
        messageObject.SetActive(false);
    }

    private IEnumerator HideMessageAfterDuration()
    {
        yield return new WaitForSeconds(messageDisplayDuration);
        HideMessage();
    }

}
