using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class PauseMenuSwitch : MonoBehaviour
{

    public static bool PauseActive;

    public static event Action<bool> OnPauseSwitch;

    [SerializeField]
    private GameObject gameplayUI;
    [SerializeField]
    private GameObject pauseMenu;

    private void Start()
    {
        HidePause();
    }

    private void OnEnable()
    {
        HidePause();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
            if ((!ChatInputUI.IsInputVisible) || PauseActive)
                SwitchPause();
    }

    public void ShowPause()
    {
        SetPauseActive(true);
    }

    public void HidePause()
    {
        SetPauseActive(false);
    }

    public void SwitchPause()
    {
        SetPauseActive(!PauseActive);
    }

    public void SetPauseActive(bool active)
    {
        PauseActive = active;
        gameplayUI.SetActive(!PauseActive);
        pauseMenu.SetActive(PauseActive);
        OnPauseSwitch?.Invoke(PauseActive);
    }

}
