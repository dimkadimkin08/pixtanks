using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
public class SimpleTransformAnimation : MonoBehaviour
{

    [Header("Transform (select none for this object transform)")]
    [SerializeField]
    private Transform targetTransform;

    [Header("Animation")]
    [SerializeField]
    private float duration;
    [SerializeField]
    private AnimationCurve animationCurve;
    [SerializeField]
    private bool playOnAwake;
    [SerializeField]
    private bool playOnEnable;
    [SerializeField]
    private bool playForever;

    [Header("Position")]
    [SerializeField]
    private bool animatePosition;
    [SerializeField]
    private Vector3 startPosition;
    [SerializeField]
    private Vector3 endPosition;
    [SerializeField]
    private bool isLocalPosition = true;

    [Header("Rotation")]
    [SerializeField]
    private bool animateRotation;
    [SerializeField]
    private Vector3 startRotation;
    [SerializeField]
    private Vector3 endRotation;
    [SerializeField]
    private bool isLocalRotation = true;

    [Header("Scale")]
    [SerializeField]
    private bool animateScale;
    [SerializeField]
    private Vector3 startScale;
    [SerializeField]
    private Vector3 endScale;
    [SerializeField]
    private bool isLocalScale = true;

    private bool _isPlaying;
    private float _animationTimer;

    private Transform TargetTransform => targetTransform ? targetTransform : transform;

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

    private void SetAnimationProgress(float t)
    {
        if (animatePosition)
            if (isLocalPosition)
                TargetTransform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            else
                TargetTransform.position = Vector3.Lerp(startPosition, endPosition, t);
        if (animateRotation)
            if (isLocalRotation)
                TargetTransform.localEulerAngles = Vector3.Lerp(startRotation, endRotation, t);
            else
                TargetTransform.eulerAngles = Vector3.Lerp(startRotation, endRotation, t);
        if (animateScale)
            if (isLocalScale)
                TargetTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            else
                TargetTransform.SetGlobalScale(Vector3.Lerp(startScale, endScale, t));
    }

    private void AnimationUpdate()
    {
        if (!_isPlaying)
        {
            _animationTimer = 0;
            SetAnimationProgress(animationCurve.Evaluate(0));
            _isPlaying = true;
            return;
        }
        _animationTimer += Time.deltaTime;
        SetAnimationProgress(animationCurve.Evaluate(Mathf.Clamp01(_animationTimer / duration)));
        if (_animationTimer >= duration)
            _isPlaying = false;
    }

    public void Play()
    {
        _isPlaying = false;
        AnimationUpdate();
    }

}