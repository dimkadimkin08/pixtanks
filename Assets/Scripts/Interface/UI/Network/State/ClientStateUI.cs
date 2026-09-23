using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;
public class ClientStateUI : MonoBehaviour
{

    private static ClientStateUI instance;

    [SerializeField]
    private GameObject content;

    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Status Text")]

    [SerializeField]
    private Text statusTextView;
    [SerializeField]
    private LocalizedString connectingStatusText;
    [SerializeField]
    private LocalizedString loadingStatusText;
    [SerializeField]
    private LocalizedString disconnectedStatusText;
    [SerializeField]
    private LocalizedString connectionFailedStatusText;
    [SerializeField]
    private LocalizedString errorStatusText;

    [Header("Error Reason Text")]

    [SerializeField]
    private TMP_InputField errorReasonTextView;
    [SerializeField]
    private LocalizedString differentVersionErrorText;
    [SerializeField]
    private LocalizedString usernameTakenErrorText;
    [SerializeField]
    private LocalizedString wrongPasswordErrorText;

    [Header("Buttons")]

    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private Text closeButtonText;
    [SerializeField]
    private LocalizedString cancelText;
    [SerializeField]
    private LocalizedString okText;

    private GameNetworkManager.NetworkClientState _lastState = GameNetworkManager.NetworkClientState.None;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void Start()
    {
        closeButton.onClick.AddListener(() =>
        {
            NetworkShutdown.ShutdownAll();
            GameNetworkManager.Singleton.ResetClientErrorState();
        });
        ShowClientState(GameNetworkManager.ClientState);
    }

    private void OnEnable()
    {
        if (GameNetworkManager.Singleton)
            GameNetworkManager.Singleton.OnClientStateSet += ShowClientState;
    }

    private void OnDisable()
    {
        if (GameNetworkManager.Singleton)
            GameNetworkManager.Singleton.OnClientStateSet -= ShowClientState;
    }

    private void Update()
    {
        if (GameNetworkManager.ClientState != _lastState)
        {
            ShowClientState(GameNetworkManager.ClientState);
            _lastState = GameNetworkManager.ClientState;
        }
    }

    private void ShowClientState(GameNetworkManager.NetworkClientState status)
    {
        var showStatus = status is not (GameNetworkManager.NetworkClientState.None or GameNetworkManager.NetworkClientState.Playing);
        content.SetActive(showStatus);
        closeButton.gameObject.SetActive(showStatus);
        switch (status)
        {
            case GameNetworkManager.NetworkClientState.None:
            case GameNetworkManager.NetworkClientState.Playing:
                statusTextView.text = "";
                errorReasonTextView.text = "";
                closeButtonText.text = "";
                break;
            case GameNetworkManager.NetworkClientState.Connecting:
                connectingStatusText.LoadWithCallback(result => { statusTextView.text = result; });
                errorReasonTextView.text = "";
                cancelText.LoadWithCallback(result => { closeButtonText.text = result; });
                break;
            case GameNetworkManager.NetworkClientState.Loading:
                loadingStatusText.LoadWithCallback(result => { statusTextView.text = result; });
                errorReasonTextView.text = "";
                cancelText.LoadWithCallback(result => { closeButtonText.text = result; });
                break;
            case GameNetworkManager.NetworkClientState.ConnectionFailed:
                connectionFailedStatusText.LoadWithCallback(result => { statusTextView.text = result; });
                errorReasonTextView.text = GameNetworkManager.ClientStateErrorMessage;
                okText.LoadWithCallback(result => { closeButtonText.text = result; });
                break;
            case GameNetworkManager.NetworkClientState.Error:
                errorStatusText.LoadWithCallback(result => { statusTextView.text = result; });
                errorReasonTextView.text = GameNetworkManager.ClientStateErrorMessage;
                okText.LoadWithCallback(result => { closeButtonText.text = result; });
                break;
            case GameNetworkManager.NetworkClientState.AuthFailed:
                errorStatusText.LoadWithCallback(result => { statusTextView.text = result; });
                switch(GameNetworkAuthenticator.LastResponse)
                {
                    case GameNetworkAuthenticator.AuthResponse.ErrorDifferentVersion:
                        differentVersionErrorText.LoadWithCallback(result => { errorReasonTextView.text = result; });
                        break;
                     case GameNetworkAuthenticator.AuthResponse.ErrorUsernameTaken:
                        usernameTakenErrorText.LoadWithCallback(result => { errorReasonTextView.text = result; });
                        break;
                    case GameNetworkAuthenticator.AuthResponse.ErrorWrongPassword:
                        wrongPasswordErrorText.LoadWithCallback(result => { errorReasonTextView.text = result; });
                        break;
                    default:
                        errorReasonTextView.text = GameNetworkAuthenticator.LastResponse.ToString();
                        break;
                };
                okText.LoadWithCallback(result => { closeButtonText.text = result; });
                break;
            case GameNetworkManager.NetworkClientState.Disconnected:
                disconnectedStatusText.LoadWithCallback(result => { statusTextView.text = result; });
                errorReasonTextView.text = "";
                okText.LoadWithCallback(result => { closeButtonText.text = result; });
                break;
        }
    }
}
