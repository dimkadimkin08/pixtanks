using System;
using System.Globalization;
using Mirror;
using UnityEngine;

public class ShopTankModificationEffect : StatusEffect
{
    [Header("Status Effect Parameters")]
    [SerializeField]
    private float selfRemoveDelay;

    [Header("Animator")]
    [SerializeField]
    private Animator animator;

    [Header("FX")]
    [SerializeField]
    private GameObject failureFXIndependentChild;
    [SerializeField]
    private GameObject successFXIndependentChild;

    private double _createdAt;

    private bool _isFailed;

    public override string ServerInit(int? fromConnId)
    {
        base.ServerInit(fromConnId);

        _createdAt = NetworkTime.time;

        EffectTarget.Tank.Movement.serverFreezeMovement++;
        EffectTarget.Tank.Parameters.OnCurrentHpSet += OnCurrentHpChanged;
        EffectTarget.Tank.Shoot.ServerSetShootingBlocked(true);

        if (NetworkClient.active)
            JumpToAnimationTime();

        return _createdAt.ToString(CultureInfo.CreateSpecificCulture("en-US"));
    }

    public override string ServerReapply()
    {
        return _createdAt.ToString(CultureInfo.CreateSpecificCulture("en-US"));
    }

    public override void ServerOnRemove()
    {
        EffectTarget.Tank.Movement.serverFreezeMovement--;
        EffectTarget.Tank.Parameters.OnCurrentHpSet -= OnCurrentHpChanged;
        EffectTarget.Tank.Shoot.ServerSetShootingBlocked(false);

        _isFailed = true;
        ShowFailureFX();
    }

    public override void ClientInit(string effectDataJson)
    {
        base.ClientInit(effectDataJson);
        effectDataJson.TryParseNoLocale(out _createdAt);
        JumpToAnimationTime();
    }

    public override void ClientSetEffectData(string effectDataJson)
    {
        effectDataJson.TryParseNoLocale(out _createdAt);
    }

    public override void ClientOnRemove()
    {
        _isFailed = true;
        ShowFailureFX();
    }

    private void JumpToAnimationTime()
    {
        if (!animator)
            return;

        AnimationClip currAnim = null;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(clip.name))
                currAnim = clip;

        var timePassed = (float)Math.Clamp(NetworkTime.time - _createdAt, 0, float.MaxValue);

        try
        {
            animator.Play(currAnim.name, 0, Mathf.Clamp01(timePassed / currAnim.averageDuration));
        }
        catch
        {
        }
    }

    private void ShowFailureFX()
    {
        if (failureFXIndependentChild && failureFXIndependentChild.transform.parent != null)
        {
            failureFXIndependentChild.transform.parent = null;
            failureFXIndependentChild.SetActive(true);
        }
    }

    private void ShowSuccessFX()
    {
        if (successFXIndependentChild && successFXIndependentChild.transform.parent != null)
        {
            successFXIndependentChild.transform.parent = null;
            successFXIndependentChild.SetActive(true);
            JumpToAnimationTime();
        }
    }

    private void OnCurrentHpChanged(ushort old, ushort current)
    {
        if (current == 0 || current < old)
            EffectTarget.ServerRemoveEffect();
    }

    protected override void TheUpdate()
    {
        if (EffectTarget.Tank.isServer && selfRemoveDelay < NetworkTime.time - _createdAt)
            EffectTarget.ServerRemoveEffect();
    }

    private void OnDestroy()
    {
        if (!_isFailed)
            ShowSuccessFX();
    }
}