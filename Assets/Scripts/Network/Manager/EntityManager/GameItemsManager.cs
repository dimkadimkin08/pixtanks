using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
public class GameItemsManager : MonoBehaviour
{
    private static float Hash01(int seed, int index)
    {
        int n = seed;
        n ^= index * 374761393;
        n = (n << 13) ^ n;
        int nn = (n * (n * n * 15731 + 789221) + 1376312589);

        return Mathf.Abs(nn % 10000) / 10000f;
    }

    public static Vector2 GetSpreadImpulse(int index, int total, float maxRadius, int seed = 0)
    {
        if (total <= 1)
            return Vector2.zero;

        float angleStep = Mathf.PI * 2f / total;

        float angle = angleStep * index;

        float angleOffset = angleStep + (45f * Hash01(seed, 0)) + (0.5f * Hash01(seed, index));

        float forceOffset = 0.9f + 0.2f * Hash01(seed + 999, index);

        float finalAngle = angle + angleOffset;
        float finalLength = maxRadius * forceOffset;

        return new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle)) * finalLength;
    }

    public static GameItemsManager Singleton;

    [Header("Items")]
    public ItemAsset[] items;

    private readonly List<Item> serverItemInstances = new();

    private void Awake()
    {
        Singleton = this;
    }

    public ItemAsset GetItem(string itemId)
    {
        return items.FirstOrDefault(item => item.id == itemId);
    }

    [Server]
    public Item ServerSpawnItem(string itemId, Vector2 position, Vector2 impulse, float impulseDuration, string[] excludeTeams = null)
    {
        var itemAsset = GetItem(itemId);
        if (itemAsset == default)
            return null;

        var itemObject = Instantiate(itemAsset.prefab, position, Quaternion.identity);
        if (!itemObject.TryGetComponent(out Item itemComponent))
        {
            Destroy(itemObject);
            return null;
        }
        if (excludeTeams != null)
            itemComponent.excludeTeams.AddRange(excludeTeams);
        itemComponent.ServerSetImpulse(impulse, impulseDuration);
        NetworkServer.Spawn(itemObject);

        serverItemInstances.RemoveAll(item => !item);
        serverItemInstances.Add(itemComponent);

        return itemComponent;
    }

    public IEnumerable<Item> ServerGetItemInstances()
    {
        serverItemInstances.RemoveAll(item => !item);
        return serverItemInstances;
    }

    [Serializable]
    public class ItemAsset
    {
        private uint _prefabAssetId = 0;
        public string id;
        public GameObject prefab;
        public uint PrefabAssetId
        {
            get
            {
                if (_prefabAssetId == 0 && prefab.TryGetComponent(out NetworkIdentity networkIdentity))
                    _prefabAssetId = networkIdentity.assetId;
                return _prefabAssetId;
            }
        }
    }
}