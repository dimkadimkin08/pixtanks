using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public const int minSeed = 0;
    public const int maxSeed = 999999;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallsTilemap;

    [Space]

    [Header("Ground Tiles")]
    [SerializeField] private Tile[] groundTiles;
    [SerializeField] private Tile[] groundAltTiles;
    [SerializeField] private Tile[] groundDetailedTiles;
    [SerializeField] private Tile[] groundDetailedAltTiles;

    [Header("Wall Tiles")]
    [SerializeField] private Tile[] wallTiles;
    [SerializeField] private Tile[] wallAltTiles;
    [SerializeField] private Tile[] wallDetailedTiles;
    [SerializeField] private Tile[] wallDetailedAltTiles;

    [Header("Bush Tiles")]
    [SerializeField] private Tile[] bushTiles;

    [Space]

    [Header("Generation")]
    [SerializeField] private Vector2Int mapSize;
    [SerializeField] private Vector2Int mapOffset;
    [SerializeField] private NoiseSettings wallSettings;
    [SerializeField] private NoiseSettings altBiomeSettings;
    [SerializeField] private NoiseSettings detailedBiomeSettings;
    [SerializeField] private NoiseSettings bushSettings;

    [Header("Spawn Points")]
    [SerializeField] private Vector2 mainTilemapScale = Vector2.one;
    [SerializeField] private Vector2Int spawnPointsGrid = new(10, 10);
    [SerializeField] private int minFreeTilesRadius = 1;

    public Vector2[] _spawnPoints = Array.Empty<Vector2>();

    private readonly Perlin _perlin = new();
    private System.Random _tileVariantRandom;

    private int _mapSizeMultiplier = 1;

    private Vector2Int FinalMapSize => mapSize * _mapSizeMultiplier;

    public ushort LastSeed { get; private set; }
    public ushort LastSizeMultiplier { get; private set; }

    private void Start()
    {
        var seed = (ushort)UnityEngine.Random.Range(minSeed, maxSeed);
        var sizeMultiplier = GameNetworkManager.Singleton.serverConfig.GetConfigParams().worldSize;
        WriteMap(seed, sizeMultiplier);
    }

    private bool IsBounds(int x, int y)
    {
        return x == 0 || y == 0 || x == FinalMapSize.x - 1 || y == FinalMapSize.y - 1;
    }

    private bool IsCloseToBounds(int x, int y)
    {
        return x == 1 || x == 2 || y == 1 || y == 2 ||
               x == FinalMapSize.x - 2 || x == FinalMapSize.x - 3 ||
               y == FinalMapSize.y - 2 || y == FinalMapSize.y - 3;
    }

    private TileInfo GetMapTileInfo(int x, int y, float floatSeed)
    {
        var isWall = IsBounds(x, y) ||
                     (!IsCloseToBounds(x, y) &&
                      wallSettings.CheckPosition(x, y, floatSeed, _perlin));

        var isAlt = altBiomeSettings.CheckPosition(x, y, floatSeed, _perlin);
        var isDetailed = detailedBiomeSettings.CheckPosition(x, y, floatSeed, _perlin);

        var tileInfo = new TileInfo { isWall = isWall };

        if (isWall)
        {
            tileInfo.tileVariants = isAlt
                ? (isDetailed ? wallDetailedAltTiles : wallAltTiles)
                : (isDetailed ? wallDetailedTiles : wallTiles);
        }
        else
        {
            tileInfo.tileVariants = isAlt
                ? (isDetailed ? groundDetailedAltTiles : groundAltTiles)
                : (isDetailed ? groundDetailedTiles : groundTiles);
        }

        return tileInfo;
    }

    private void WriteTile(Tilemap tilemap, Tile[] tiles, int x, int y, int seed)
    {
        tilemap.SetTile(
            new Vector3Int(x, y) + (Vector3Int)mapOffset,
            tiles[_tileVariantRandom.Next(x + y + Mathf.Abs(seed)) % tiles.Length]
        );
    }

    public void WriteMap(ushort seed, ushort sizeMultiplier)
    {
        LastSeed = seed;
        LastSizeMultiplier = sizeMultiplier;
        _mapSizeMultiplier = sizeMultiplier;

        var clampedSeed = Mathf.Clamp(Mathf.Abs(seed), minSeed, maxSeed);
        var floatSeed = clampedSeed / 100f + 1.1f;

        _tileVariantRandom = new System.Random(clampedSeed);
        _perlin.SetPermutationTable(PerlinUtils.GeneratePremutationTable(clampedSeed));

        floorTilemap.ClearAllTiles();
        wallsTilemap.ClearAllTiles();

        floorTilemap.origin = (Vector3Int)mapOffset;
        floorTilemap.size = (Vector3Int)FinalMapSize;

        wallsTilemap.origin = (Vector3Int)mapOffset;
        wallsTilemap.size = (Vector3Int)FinalMapSize;

        // Кэш карты: true = стена, false = свободно
        bool[,] blocked = new bool[FinalMapSize.x, FinalMapSize.y];

        for (int x = 0; x < FinalMapSize.x; x++)
        {
            for (int y = 0; y < FinalMapSize.y; y++)
            {
                var tileInfo = GetMapTileInfo(x, y, floatSeed);

                blocked[x, y] = tileInfo.isWall;

                WriteTile(
                    tileInfo.isWall ? wallsTilemap : floorTilemap,
                    tileInfo.tileVariants,
                    x,
                    y,
                    clampedSeed
                );
            }
        }

        _spawnPoints = CalculateSpawnPoints(blocked);
    }

    private Vector2[] CalculateSpawnPoints(bool[,] blockedMap)
    {
        var scaledGrid = spawnPointsGrid * _mapSizeMultiplier;
        var result = new List<Vector2>();

        float stepX = FinalMapSize.x / (float)scaledGrid.x;
        float stepY = FinalMapSize.y / (float)scaledGrid.y;

        for (int gx = 0; gx < scaledGrid.x; gx++)
        {
            for (int gy = 0; gy < scaledGrid.y; gy++)
            {
                int tx = Mathf.Clamp(Mathf.FloorToInt((gx + 0.5f) * stepX), 0, FinalMapSize.x - 1);
                int ty = Mathf.Clamp(Mathf.FloorToInt((gy + 0.5f) * stepY), 0, FinalMapSize.y - 1);

                if (CanSpawnAt(tx, ty, blockedMap))
                {
                    Vector2 worldPos = TileToWorldCenter(tx, ty);
                    result.Add(worldPos);
                }
            }
        }

        return result.ToArray();
    }

    private bool CanSpawnAt(int x, int y, bool[,] blockedMap)
    {
        for (int ox = -minFreeTilesRadius; ox <= minFreeTilesRadius; ox++)
        {
            for (int oy = -minFreeTilesRadius; oy <= minFreeTilesRadius; oy++)
            {
                int nx = x + ox;
                int ny = y + oy;

                if (nx < 0 || ny < 0 || nx >= FinalMapSize.x || ny >= FinalMapSize.y)
                    return false;

                if (blockedMap[nx, ny])
                    return false;
            }
        }

        return true;
    }

    private Vector2 TileToWorldCenter(int x, int y)
    {
        Vector2 local = new(
            (x + 0.5f) * mainTilemapScale.x,
            (y + 0.5f) * mainTilemapScale.y
        );

        Vector2 offset = Vector2.Scale(mapOffset, mainTilemapScale);
        return offset + local;
    }

    [Serializable]
    public class NoiseSettings
    {
        public float seedOffset = 100;
        public float noiseScale = 10;

        [Range(0, 1)]
        public float selectionThreshold = 0.7f;

        public bool CheckPosition(int x, int y, float floatSeed, Perlin perlin)
        {
            var seedValue = floatSeed + seedOffset;
            var scaledPos = ScalePosition(x, y, seedValue);

            if (CheckNoiseThreshold(scaledPos.x, scaledPos.y, perlin))
            {
                foreach (var neighbor in GetNeighborOffsets())
                {
                    var neighborPos = ScalePosition(x + neighbor.x, y + neighbor.y, seedValue);

                    if (CheckNoiseThreshold(neighborPos.x, neighborPos.y, perlin))
                        return true;
                }
            }

            return false;
        }

        private Vector2 ScalePosition(int x, int y, float seedValue)
        {
            return new Vector2(
                seedValue + (x / noiseScale),
                seedValue + (y / noiseScale)
            );
        }

        private bool CheckNoiseThreshold(float x, float y, Perlin perlin)
        {
            return selectionThreshold <= 0.5f * (1 + perlin.Noise(x, y));
        }

        private static Vector2Int[] GetNeighborOffsets()
        {
            return new[]
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };
        }
    }

    private struct TileInfo
    {
        public bool isWall;
        public Tile[] tileVariants;
    }
}