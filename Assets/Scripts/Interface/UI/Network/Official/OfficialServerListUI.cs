using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
public class OfficialServerListUI : MonoBehaviour
{
    [SerializeField]
    private OfficialServersLoader serversLoader;
    [SerializeField]
    private Transform listContentObject;
    [SerializeField]
    private OfficialServerListItemUI serverItemPrefab;

    [Header("Loading Text")]
    [SerializeField]
    private Text loadingStateText;
    [SerializeField]
    private LocalizedString loadingText;
    [SerializeField]
    private LocalizedString errorText;

    private float timeUntilTextUpdate;
    private int textDotsCount;
    private OfficialServersLoader.State lastState;

    private void Start()
    {
        UpdateList();
    }

    private void OnEnable()
    {
        serversLoader.OnStateChanged += OnStateChanged;
    }

    private void Update()
    {
        timeUntilTextUpdate -= Time.deltaTime;
        if (timeUntilTextUpdate > 0)
            return;
        timeUntilTextUpdate = 0.3f;
        if (lastState != serversLoader.CurrentState || textDotsCount >= 3)
        {
            textDotsCount = 0;
            lastState = serversLoader.CurrentState;
            switch(lastState)
            {
                case OfficialServersLoader.State.Loading:
                    loadingText.LoadWithCallback(result => { loadingStateText.text = result; });
                    break;
                case OfficialServersLoader.State.Failure:
                    errorText.LoadWithCallback(result => { loadingStateText.text = result; });
                    break;
                default:
                    loadingStateText.text = "";
                    break;
            }
            return;
        }
        if (lastState == OfficialServersLoader.State.Success)
            return;
        loadingStateText.text += ".";
        textDotsCount = textDotsCount < 3 ? textDotsCount + 1 : 1;
    }

    private void OnDisable()
    {
        serversLoader.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(OfficialServersLoader.State state)
    {
        UpdateList();
    }

    private void UpdateList()
    {
        var servers = serversLoader.Servers;

        foreach (Transform child in listContentObject)
            Destroy(child.gameObject);

        listContentObject.TryGetComponent(out RectTransform containerRect);

        if (servers.Length == 0)
        {
            if (containerRect)
            {
                containerRect.anchorMax = new(containerRect.anchorMax.x, 1);
                containerRect.anchorMin = new(containerRect.anchorMin.x, 1);
            }
            return;
        }

        if (containerRect)
        {
            var prefabRectTransform = serverItemPrefab.GetComponent<RectTransform>();
            containerRect.anchorMax = new(containerRect.anchorMax.x, 1);
            containerRect.anchorMin = new(containerRect.anchorMin.x, 1 - servers.Length * (prefabRectTransform.anchorMax.y - prefabRectTransform.anchorMin.y));
        }

        float itemHeight = 1f / servers.Length;
        float totalHeight = 0;
        for (var i = 0; i < servers.Length; i++)
        {
            var server = servers[i];
            var item = Instantiate(serverItemPrefab, listContentObject);
            var rectTransform = item.GetComponent<RectTransform>();
            rectTransform.anchorMax = new(1, 1 - totalHeight);
            rectTransform.anchorMin = new(0, 1 - totalHeight - itemHeight);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            totalHeight += itemHeight;
            var id = server.id;
            var isCurrent = id == serversLoader.CurrentServerId;
            item.Init(serverInfo: server, isCurrentServer: isCurrent, onClick: () =>
            {
                serversLoader.SelectServer(id);
                UpdateList();
            });
        }
        var contentRect = listContentObject.GetComponent<RectTransform>();
        contentRect.sizeDelta = new(contentRect.sizeDelta.x, totalHeight);
    }

}