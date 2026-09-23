using System;
using UnityEngine;
public class HolydayGameObject : MonoBehaviour
{
    public NetworkTank getTankVisualSize;
    public bool showOnChristmas = false;

    public GameObject childToEnable;

    private void Awake()
    {
        if (showOnChristmas)
        {
            DateTime now = DateTime.Now;
            if ((now.Month == 12 && now.Day >= 18) || (now.Month == 1 && now.Day <= 4))
            {
                if (childToEnable)
                    childToEnable.SetActive(true);
                if (getTankVisualSize)
                    transform.localScale = new(getTankVisualSize.VisualSize, getTankVisualSize.VisualSize, 1);
                return;
            }
        }
        Destroy(gameObject);
    }
}