using UnityEngine;

[CreateAssetMenu(menuName = "TankAI/TankAIScannerSettingsAsset", fileName = "TankAIScannerSettings", order = 0)]
public class TankAIScannerSettings : ScriptableObject
{
    public float scanInterval = 1;
    public float scanRadius = 10;
    public LayerMask scanLayerMask;
    public bool checkForObstacles;
    public LayerMask obstaclesLayerMask;
}
