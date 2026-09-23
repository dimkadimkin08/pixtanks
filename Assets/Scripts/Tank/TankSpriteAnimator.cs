using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class TankSpriteAnimator : MonoBehaviour
{
    [Header("Tank")]
    [SerializeField] private NetworkTank tank;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animators")]
    [SerializeField] private TankSpriteAnimation idleAnimation;
    [SerializeField] private bool useIdleAnimation;

    [SerializeField] private TankSpriteAnimation idleReloadAnimation;
    [SerializeField] private bool useIdleReloadAnimation;

    [SerializeField] private TankSpriteAnimation movementAnimation;
    [SerializeField] private TankSpriteAnimation reloadMovementAnimation;
    [SerializeField] private bool useReloadAnimation;

    [SerializeField] private TankSpriteAnimation[] shootAnimations;

    private AnimationType _currentAnimType;

    [CanBeNull]
    private AnimationInstance _currentAnimInstance;

    // Кэш состояний только для loop-анимаций
    private readonly Dictionary<TankSpriteAnimation, AnimationInstance> _loopInstances = new();

    private enum AnimationType
    {
        Idle,
        IdleReload,
        Movement,
        MovementReload,
        Shoot
    }

    private void OnEnable()
    {
        tank.Shoot.OnShoot += OnShoot;
    }

    private void OnDisable()
    {
        tank.Shoot.OnShoot -= OnShoot;
    }

    private void Update()
    {
        if (_currentAnimType != AnimationType.Shoot || !tank.Shoot.IsShooting)
        {
            SetCurrentAnimation(
                tank.Movement.IsMoving || tank.Movement.IsRotating
                    ? tank.Shoot.IsReloading ? AnimationType.MovementReload : AnimationType.Movement
                    : tank.Shoot.IsReloading ? AnimationType.IdleReload : AnimationType.Idle
            );
        }

        _currentAnimInstance?.Update(Time.deltaTime);
    }

    private void SetCurrentAnimation(AnimationType animationType)
    {
        if (_currentAnimType == animationType)
            return;

        _currentAnimType = animationType;

        var newAnimation = animationType switch
        {
            AnimationType.Idle =>
                idleAnimation,

            AnimationType.IdleReload =>
                useIdleReloadAnimation ? idleReloadAnimation : idleAnimation,

            AnimationType.Movement =>
                movementAnimation,

            AnimationType.MovementReload =>
                useReloadAnimation ? reloadMovementAnimation : movementAnimation,

            AnimationType.Shoot =>
                shootAnimations.Length > tank.Shoot.PatternIndex
                    ? shootAnimations[tank.Shoot.PatternIndex]
                    : null,

            _ => idleAnimation
        };

        if (newAnimation == null)
        {
            _currentAnimInstance = null;
            return;
        }

        if (_currentAnimInstance != null && _currentAnimInstance.animation == newAnimation)
            return;

        _currentAnimInstance = GetOrCreateInstance(newAnimation);
    }

    private AnimationInstance GetOrCreateInstance(TankSpriteAnimation animation)
    {
        // Для loop-анимаций сохраняем состояние
        if (animation.loop)
        {
            if (_loopInstances.TryGetValue(animation, out var instance))
                return instance;

            instance = new AnimationInstance(animation, spriteRenderer);
            _loopInstances.Add(animation, instance);
            return instance;
        }

        // Для одноразовых всегда создаём заново
        return new AnimationInstance(animation, spriteRenderer);
    }

    private void OnShoot(TankShoot.PatternData patternData)
    {
        SetCurrentAnimation(AnimationType.Shoot);
    }

    [Serializable]
    private class TankSpriteAnimation
    {
        public Frame[] frames;
        public bool loop;

        [Serializable]
        public struct Frame
        {
            public Sprite sprite;
            public float duration;
        }
    }

    private class AnimationInstance
    {
        public readonly SpriteRenderer spriteRenderer;
        public readonly TankSpriteAnimation animation;

        private float _timeUntilNextFrame;
        private int _currentFrameIndex = -1;
        private bool _finished;

        public AnimationInstance(TankSpriteAnimation animation, SpriteRenderer spriteRenderer)
        {
            this.animation = animation;
            this.spriteRenderer = spriteRenderer;
        }

        public void Update(float deltaTime)
        {
            if (_finished)
                return;

            _timeUntilNextFrame -= deltaTime;

            if (_timeUntilNextFrame > 0f)
                return;

            _currentFrameIndex++;

            if (_currentFrameIndex >= animation.frames.Length)
                _currentFrameIndex = animation.loop ? 0 : -1;

            if (_currentFrameIndex >= 0 && animation.frames.Length > 0)
            {
                var frame = animation.frames[Mathf.Min(_currentFrameIndex, animation.frames.Length-1)];
                _timeUntilNextFrame = frame.duration;
                spriteRenderer.sprite = frame.sprite;
            }
            else
            {
                _finished = true;
            }
        }
    }
}