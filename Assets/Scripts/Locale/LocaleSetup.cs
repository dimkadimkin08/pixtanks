using System;
using System.Globalization;
using UnityEngine;

public class LocaleSetup : MonoBehaviour
{
    void Awake()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }
}
