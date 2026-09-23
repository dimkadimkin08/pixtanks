using System.Collections;
using UnityEngine;
public class TankSounds : MonoBehaviour
{

    [Header("Tank")]

    [SerializeField]
    private NetworkTank tank;

    [Header("Audio")]

    [SerializeField]
    private AudioSource[] shootSounds;
    [SerializeField]
    private float[] shootSoundDelays;
    [SerializeField]
    private AudioSource movingSound;
    [SerializeField]
    private AudioSource healSound;
    [SerializeField]
    private AudioSource damageSound;

    private void OnEnable()
    {
        if (tank.Shoot)
            tank.Shoot.OnShoot += OnShoot;
        if (tank.Parameters)
            tank.Parameters.OnCurrentHpSet += OnCurrentHpSet;
    }

    private void OnDisable()
    {
        if (tank.Shoot)
            tank.Shoot.OnShoot -= OnShoot;
        if (tank.Parameters)
            tank.Parameters.OnCurrentHpSet -= OnCurrentHpSet;
    }

    private void LateUpdate()
    {
        var isMoving = tank.Movement.IsMoving;
        if (movingSound && movingSound.enabled && isMoving != movingSound.isPlaying)
            if (isMoving)
                movingSound.Play();
            else
                movingSound.Stop();
    }

    private void OnShoot(TankShoot.PatternData patternData)
    {
        if (shootSounds.Length > patternData.index)
            if (shootSoundDelays.Length <= patternData.index || shootSoundDelays[patternData.index] <= 0)
            {
                if (shootSounds[patternData.index].enabled)
                    shootSounds[patternData.index].Play();
            }
            else
                StartCoroutine(PlayAudioAfterDelay(shootSounds[patternData.index], shootSoundDelays[patternData.index]));
    }

    private void OnCurrentHpSet(ushort oldHp, ushort currentHp)
    {
        if (oldHp < currentHp && healSound && healSound.enabled)
            healSound.Play();
        else if (oldHp > currentHp && damageSound && damageSound.enabled)
            damageSound.Play();
    }

    private IEnumerator PlayAudioAfterDelay(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource.enabled)
            audioSource.Play();
    }

}
