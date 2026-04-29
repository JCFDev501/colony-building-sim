using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active pawns in the scene and owns pawn spawning for the prototype.
/// This manager does not generate the world and does not control game startup flow.
/// GameFlowManager coordinates when this manager should spawn starter pawns.
/// </summary>
public class PawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager m_gridManager;
    [SerializeField] private Pawn m_starterPawnPrefab;
    [SerializeField] private Transform m_pawnRoot;

    [Header("Starter Pawn Settings")]
    [SerializeField] private int m_starterPawnCount = 4;
    [SerializeField] private int m_spawnSearchRadius = 6;
    [SerializeField] private float m_spawnHeightOffset = 0.0f;

    [Header("Debug")]
    [SerializeField] private bool m_logRegistrationEvents = false;
    [SerializeField] private bool m_logSpawnEvents = true;
    [SerializeField] private bool m_logProfileSpawnEvents = true;

    private readonly List<Pawn> m_activePawns = new();
    private readonly List<Pawn> m_colonistPawns = new();

    /// <summary>
    /// Gets the currently active pawns.
    /// </summary>
    public IReadOnlyList<Pawn> ActivePawns
    {
        get { return m_activePawns; }
    }

    /// <summary>
    /// Gets the currently tracked colonist pawns.
    /// For now, starter pawns are treated as colonists.
    /// </summary>
    public IReadOnlyList<Pawn> ColonistPawns
    {
        get { return m_colonistPawns; }
    }

    /// <summary>
    /// Spawns the starter colonists into a valid cluster after world generation completes.
    /// This fallback path creates anonymous starter pawns and is kept for temporary compatibility.
    /// </summary>
    public void SpawnStarterPawns()
    {
        if (!HasRequiredSpawnReferences())
        {
            return;
        }

        if (m_colonistPawns.Count > 0)
        {
            Debug.LogWarning("Starter pawns were not spawned because colonists already exist.", this);
            return;
        }

        if (!TryFindStarterAnchorTile(out GridTile anchorTile))
        {
            Debug.LogError("PawnManager could not find a valid starter pawn anchor tile.", this);
            return;
        }

        List<GridTile> spawnTiles = GetValidSpawnCluster(anchorTile, m_starterPawnCount);

        if (spawnTiles.Count < m_starterPawnCount)
        {
            Debug.LogWarning(
                "PawnManager found only " + spawnTiles.Count +
                " valid starter pawn spawn tiles out of requested count " +
                m_starterPawnCount + ".",
                this);
        }

        for (int i = 0; i < spawnTiles.Count; i++)
        {
            SpawnColonistPawn(spawnTiles[i]);
        }

        if (m_logSpawnEvents)
        {
            Debug.Log("Spawned starter colonists: " + spawnTiles.Count, this);
        }
    }

    /// <summary>
    /// Spawns starter colonists from generated pawn profile data.
    /// One pawn prefab is spawned per valid profile.
    /// </summary>
    public void SpawnStarterPawns(IReadOnlyList<PawnProfile> pawnProfiles)
    {
        if (!HasRequiredSpawnReferences())
        {
            return;
        }

        if (!HasValidPawnProfiles(pawnProfiles))
        {
            Debug.LogError("PawnManager could not spawn starter pawns because no valid pawn profiles were provided.", this);
            return;
        }

        if (m_colonistPawns.Count > 0)
        {
            Debug.LogWarning("Starter pawns were not spawned because colonists already exist.", this);
            return;
        }

        if (!TryFindStarterAnchorTile(out GridTile anchorTile))
        {
            Debug.LogError("PawnManager could not find a valid starter pawn anchor tile.", this);
            return;
        }

        int validProfileCount = CountValidPawnProfiles(pawnProfiles);
        List<GridTile> spawnTiles = GetValidSpawnCluster(anchorTile, validProfileCount);

        if (spawnTiles.Count < validProfileCount)
        {
            Debug.LogWarning(
                "PawnManager found only " + spawnTiles.Count +
                " valid starter pawn spawn tiles out of requested profile count " +
                validProfileCount + ".",
                this);
        }

        int spawnTileIndex = 0;

        for (int i = 0; i < pawnProfiles.Count; i++)
        {
            PawnProfile pawnProfile = pawnProfiles[i];

            if (pawnProfile == null)
            {
                continue;
            }

            if (spawnTileIndex >= spawnTiles.Count)
            {
                break;
            }

            SpawnColonistPawn(spawnTiles[spawnTileIndex], pawnProfile);
            ++spawnTileIndex;
        }

        if (m_logSpawnEvents)
        {
            Debug.Log("Spawned starter colonists from profiles: " + spawnTileIndex, this);
        }
    }

    /// <summary>
    /// Registers a pawn if it is valid and not already tracked.
    /// </summary>
    public void RegisterPawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            Debug.LogWarning("PawnManager.RegisterPawn was called with a null pawn.");
            return;
        }

        if (m_activePawns.Contains(pPawn))
        {
            return;
        }

        m_activePawns.Add(pPawn);

        if (m_logRegistrationEvents)
        {
            Debug.Log($"Registered pawn: {pPawn.PawnId}", pPawn);
        }
    }

    /// <summary>
    /// Registers a pawn as a colonist if it is valid and not already tracked.
    /// </summary>
    public void RegisterColonistPawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            Debug.LogWarning("PawnManager.RegisterColonistPawn was called with a null pawn.");
            return;
        }

        RegisterPawn(pPawn);

        if (m_colonistPawns.Contains(pPawn))
        {
            return;
        }

        m_colonistPawns.Add(pPawn);
    }

    /// <summary>
    /// Unregisters a pawn if it is currently tracked.
    /// </summary>
    public void UnregisterPawn(Pawn pPawn)
    {
        if (pPawn == null)
        {
            Debug.LogWarning("PawnManager.UnregisterPawn was called with a null pawn.");
            return;
        }

        m_colonistPawns.Remove(pPawn);

        if (!m_activePawns.Remove(pPawn))
        {
            return;
        }

        if (m_logRegistrationEvents)
        {
            Debug.Log($"Unregistered pawn: {pPawn.PawnId}", pPawn);
        }
    }

    /// <summary>
    /// Returns true if the pawn is currently tracked.
    /// </summary>
    public bool IsPawnRegistered(Pawn pPawn)
    {
        if (pPawn == null)
        {
            return false;
        }

        return m_activePawns.Contains(pPawn);
    }

    /// <summary>
    /// Verifies that starter pawn spawning has the scene references it needs.
    /// </summary>
    private bool HasRequiredSpawnReferences()
    {
        if (m_gridManager == null)
        {
            Debug.LogError("PawnManager is missing a GridManager reference.", this);
            return false;
        }

        if (m_starterPawnPrefab == null)
        {
            Debug.LogError("PawnManager is missing a starter pawn prefab reference.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns whether at least one valid pawn profile was provided.
    /// </summary>
    private bool HasValidPawnProfiles(IReadOnlyList<PawnProfile> pawnProfiles)
    {
        return CountValidPawnProfiles(pawnProfiles) > 0;
    }

    /// <summary>
    /// Counts non-null pawn profiles in the provided profile list.
    /// </summary>
    private int CountValidPawnProfiles(IReadOnlyList<PawnProfile> pawnProfiles)
    {
        if (pawnProfiles == null)
        {
            return 0;
        }

        int validProfileCount = 0;

        for (int i = 0; i < pawnProfiles.Count; i++)
        {
            if (pawnProfiles[i] != null)
            {
                ++validProfileCount;
            }
        }

        return validProfileCount;
    }

    /// <summary>
    /// Finds the valid tile closest to the center of the map.
    /// This prevents starter pawns from spawning near a corner due to grid insertion order.
    /// </summary>
    private bool TryFindStarterAnchorTile(out GridTile anchorTile)
    {
        anchorTile = null;

        Vector2 centerCoordinates = new Vector2(
            (m_gridManager.GridWidth - 1) * 0.5f,
            (m_gridManager.GridHeight - 1) * 0.5f);

        float bestDistanceSquared = float.MaxValue;

        foreach (GridTile tile in m_gridManager.Tiles.Values)
        {
            if (!IsValidStarterSpawnTile(tile))
            {
                continue;
            }

            Vector2 tileCoordinates = new Vector2(tile.Coordinates.x, tile.Coordinates.y);
            float distanceSquared = (tileCoordinates - centerCoordinates).sqrMagnitude;

            if (distanceSquared >= bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared = distanceSquared;
            anchorTile = tile;
        }

        return anchorTile != null;
    }

    /// <summary>
    /// Finds valid spawn tiles around the anchor tile.
    /// The anchor tile is checked first, then nearby tiles are searched outward.
    /// </summary>
    private List<GridTile> GetValidSpawnCluster(GridTile anchorTile, int requestedCount)
    {
        List<GridTile> spawnTiles = new List<GridTile>();

        if (anchorTile == null || requestedCount <= 0)
        {
            return spawnTiles;
        }

        TryAddSpawnTile(anchorTile, spawnTiles, requestedCount);

        Vector2Int anchorCoordinates = anchorTile.Coordinates;

        for (int radius = 1; radius <= m_spawnSearchRadius; radius++)
        {
            for (int y = anchorCoordinates.y - radius; y <= anchorCoordinates.y + radius; y++)
            {
                for (int x = anchorCoordinates.x - radius; x <= anchorCoordinates.x + radius; x++)
                {
                    if (spawnTiles.Count >= requestedCount)
                    {
                        return spawnTiles;
                    }

                    bool isOuterRing =
                        x == anchorCoordinates.x - radius ||
                        x == anchorCoordinates.x + radius ||
                        y == anchorCoordinates.y - radius ||
                        y == anchorCoordinates.y + radius;

                    if (!isOuterRing)
                    {
                        continue;
                    }

                    Vector2Int coordinates = new Vector2Int(x, y);

                    if (!m_gridManager.TryGetTile(coordinates, out GridTile tile))
                    {
                        continue;
                    }

                    TryAddSpawnTile(tile, spawnTiles, requestedCount);
                }
            }
        }

        return spawnTiles;
    }

    /// <summary>
    /// Adds a tile to the spawn set if it is valid and not already selected.
    /// </summary>
    private void TryAddSpawnTile(GridTile tile, List<GridTile> spawnTiles, int requestedCount)
    {
        if (spawnTiles.Count >= requestedCount)
        {
            return;
        }

        if (!IsValidStarterSpawnTile(tile))
        {
            return;
        }

        if (spawnTiles.Contains(tile))
        {
            return;
        }

        spawnTiles.Add(tile);
    }

    /// <summary>
    /// Returns true if a tile is safe for spawning a starter colonist.
    /// </summary>
    private bool IsValidStarterSpawnTile(GridTile tile)
    {
        if (tile == null)
        {
            return false;
        }

        if (!tile.IsWalkable)
        {
            return false;
        }

        if (tile.TerrainType == TileTerrainType.Water)
        {
            return false;
        }

        if (tile.BlockType != BlockType.None)
        {
            return false;
        }

        if (tile.WorldObjectType != WorldObjectType.None)
        {
            return false;
        }

        if (tile.IsOccupied)
        {
            return false;
        }

        if (tile.IsReserved)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Instantiates one colonist pawn on the given tile and registers it with this manager.
    /// </summary>
    private void SpawnColonistPawn(GridTile tile)
    {
        if (tile == null)
        {
            return;
        }

        Vector3 spawnPosition = tile.WorldPosition;
        spawnPosition.y += m_spawnHeightOffset;

        Transform parent = m_pawnRoot != null ? m_pawnRoot : transform;
        Pawn pawn = Instantiate(m_starterPawnPrefab, spawnPosition, Quaternion.identity, parent);

        pawn.SetGridCoordinate(tile.Coordinates);
        pawn.SnapToGridCoordinate();

        tile.IsOccupied = true;
        RegisterColonistPawn(pawn);
    }

    /// <summary>
    /// Instantiates one colonist pawn on the given tile, initializes it from a generated profile,
    /// and registers it with this manager.
    /// </summary>
    private void SpawnColonistPawn(GridTile tile, PawnProfile pawnProfile)
    {
        if (tile == null || pawnProfile == null)
        {
            return;
        }

        Vector3 spawnPosition = tile.WorldPosition;
        spawnPosition.y += m_spawnHeightOffset;

        Transform parent = m_pawnRoot != null ? m_pawnRoot : transform;
        Pawn pawn = Instantiate(m_starterPawnPrefab, spawnPosition, Quaternion.identity, parent);

        pawn.InitializeFromProfile(pawnProfile);
        pawn.SetGridCoordinate(tile.Coordinates);
        pawn.SnapToGridCoordinate();

        tile.IsOccupied = true;
        RegisterColonistPawn(pawn);

        if (m_logProfileSpawnEvents)
        {
            Debug.Log(
                "Spawned pawn from profile: " + pawnProfile.DisplayName +
                " | PawnId: " + pawnProfile.PawnId +
                " | Spawn Tile: " + tile.Coordinates,
                pawn);
        }
    }
}