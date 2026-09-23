using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
public static class LocalizationExt
{
    public static void LoadWithCallback(this LocalizedString localizedString, Action<string> callback)
    {
        new Task(async () =>
        {
            var operationAsync = localizedString.GetLocalizedStringAsync();
            Addressables.ResourceManager.Acquire(operationAsync);
            await operationAsync.Task;
            callback(operationAsync.Result);
        }).RunSynchronously();
    }
}