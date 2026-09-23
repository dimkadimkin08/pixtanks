using UnityEngine;
[CreateAssetMenu(menuName = "TankAI/TankAISettingsAsset", fileName = "TankAISettings", order = 0)]
public class TankAISettings : ScriptableObject
{

    [Header("Battle Moving")]

    [Tooltip("Determines the chance that the tank will move backward during its battle move.")]
    [Range(0, 1)]
    public float chanceToMoveBackBattleMove = 0.35f;
    public float minBattleMoveTime = 0.75f;
    public float maxBattleMoveTime = 2;
    [Range(0, 1)]
    public float battleMoveDelayChance = 0.5f;
    public float minBattleMoveDelay = 0.05f;
    public float maxBattleMoveDelay = 0.2f;
    [Tooltip("Maximum angle for battle move for a target within the max shoot distance")]
    public float normalBattleMoveAngle = 90;
    [Tooltip("Maximum angle for battle move for a target beyond the max shoot distance")]
    public float chaseBattleMoveAngle = 45;
    public float minimalDistance = 4;
    public float safeDistance = 7;

    [Header("Shooting")]

    [Tooltip("The maximum distance a tank can fire. Especially important for tanks with close attacks.")]
    public float maxShootDistance = 10;
    [Tooltip("The maximum deviation from the target at which a tank can fire")]
    public float maxShootAngle = 5;
    public float minShootDelay = 0.2f;
    public float maxShootDelay = 0.4f;
    public float minShootTime = 0.5f;
    public float maxShootTime = 1.5f;
    [Tooltip("Determines the chance that the tank will move during its attack.")]
    [Range(0, 1)]
    public float chanceToMoveDuringShoot = 0.5f;
    [Tooltip("(Works only if chanceToMoveDuringShoot succeed) Determines the chance that the tank will move backward during its attack.")]
    [Range(0, 1)]
    public float chanceToMoveBackDuringShoot = 0.5f;

    [Header("Idle Moving")]

    public float minIdleMoveDelay = 1;
    public float maxIdleMoveDelay = 4;
    public float minIdleMoveTime = 1;
    public float maxIdleMoveTime = 3;

}
