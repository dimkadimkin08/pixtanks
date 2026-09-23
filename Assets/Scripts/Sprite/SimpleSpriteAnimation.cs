using System;
using UnityEngine;
public class SimpleSpriteAnimation : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Frame[] animationFrames;
    public bool playOnAwake;
    public bool playOnEnable;
    public bool playForever;

    private bool _isPlaying;
    private float _nextFrameTimer;
    private int _currentFrameIndex;

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

    private bool SetFrame(int frameIndex)
    {
        if (animationFrames.Length <= frameIndex)
            return false;
        _currentFrameIndex = frameIndex;
        var frame = animationFrames[frameIndex];
        _nextFrameTimer = frame.duration;
        spriteRenderer.sprite = frame.sprite;
        return true;
    }

    private void AnimationUpdate()
    {
        if (!_isPlaying)
        {
            _isPlaying = SetFrame(0);
            return;
        }
        _nextFrameTimer -= Time.deltaTime;
        if (_nextFrameTimer <= 0)
        {
            _currentFrameIndex++;
            _isPlaying = SetFrame(_currentFrameIndex);
        }
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

    [Serializable]
    private struct Frame
    {
        public Sprite sprite;
        public float duration;
    }
}
