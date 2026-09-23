using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class SimpleUIImageColorShade : MonoBehaviour
{
    [SerializeField]
    private Image uiImage;
    [SerializeField]
    private Gradient gradient;
    [SerializeField]
    private bool onlyChangeAlpha;
    [SerializeField]
    private float duration;
    [SerializeField]
    private bool playOnAwake;
    [SerializeField]
    private bool playOnEnable;
    [SerializeField]
    private bool playForever;

    private bool _isPlaying;
    private float _animationTimer;

    private void Awake()
    {
        if (playOnAwake)
            Play();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void Update()
    {
        if (playForever || _isPlaying)
            AnimationUpdate();
    }

    private void SetColor(Color color)
    {
        if (onlyChangeAlpha)
        {
            var newColor = uiImage.color;
            newColor.a = color.a;
            uiImage.color = newColor;
        }
        else
            uiImage.color = color;
    }

    private void SetAnimationProgress(float t)
    {
        SetColor(gradient.Evaluate(t));
    }

    private void AnimationUpdate()
    {
        if (!_isPlaying)
        {
            _animationTimer = 0;
            SetAnimationProgress(0);
            _isPlaying = true;
            return;
        }
        _animationTimer += Time.deltaTime;
        SetAnimationProgress(Mathf.Clamp01(_animationTimer / duration));
        if (_animationTimer >= duration)
            _isPlaying = false;
    }

    public void Play()
    {
        _isPlaying = false;
        AnimationUpdate();
    }
}
