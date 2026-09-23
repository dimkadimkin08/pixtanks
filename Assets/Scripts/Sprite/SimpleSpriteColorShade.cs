using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public class SimpleSpriteColorShade : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Gradient gradient;
    [SerializeField]
    private bool onlyChangeAlpha;
    [SerializeField]
    private float duration;
    public bool playOnAwake;
    public bool playOnEnable;
    public bool playForever;

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
            var newColor = spriteRenderer.color;
            newColor.a = color.a;
            spriteRenderer.color = newColor;
        }
        else
            spriteRenderer.color = color;
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

    public void SetPlayForever(bool isPlayForever)
    {
        playForever = isPlayForever;
    }
}
