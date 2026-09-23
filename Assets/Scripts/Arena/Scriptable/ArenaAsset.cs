using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ArenaAsset", menuName = "Arena/ArenaAsset")]
public class ArenaAsset : ScriptableObject
{
    public ArenaData arenaData;
}

[Serializable]
public class ArenaData
{
    public string id;

    [Multiline(10)]
    public string tilemap;
    public ItemSpawner[] itemSpawners;
    public Flag[] flags;
    public PlayerGroup[] playerGroups;

    [Serializable]
    public class ItemSpawner
    {
        public Type type;
        public float delay;
        public Vector2 pos;

        public enum Type { boost, gear, shield }
    }

    [Serializable]
    public class Flag
    {
        public string team;
        public Vector2 pos;
    }

    [Serializable]
    public class PlayerGroup
    {
        public string team;
        public float respawnDelay;
        public Vector3[] spawnPoints;
    }
}