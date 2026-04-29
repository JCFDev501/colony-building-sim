using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

/// <summary>
/// Owns the staged world-generation pipeline for the prototype.
/// This component works on top of the grid system and is responsible for
/// coordinating world-generation steps without overloading GridManager.
/// </summary>
public class WorldGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager m_gridManager;
    [SerializeField] private WorldTerrainRenderer m_worldTerrainRenderer;
    [SerializeField] private BlockRenderer m_blockRenderer;
    [SerializeField] private WorldObjectRenderer m_worldObjectRenderer;

    [Header("Generation Settings")]
    [SerializeField] private int m_seed = 12345;
    [SerializeField] private bool m_useRandomSeed = false;

    [Header("Debug")]
    [SerializeField] private bool m_enableDebugRegenerateInput = true;
    [SerializeField] private bool m_logTemporaryGenerationPipeline = true;
    [SerializeField] private bool m_logGenerationTimings = true;

    [Header("Water Settings")]
    [SerializeField] [Range(1, 3)] private int m_riverCount = 1;
    [SerializeField] [Range(1, 4)] private int m_riverMinWidth = 1;
    [SerializeField] [Range(1, 6)] private int m_riverMaxWidth = 2;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_riverMeanderChance = 0.30f;
    [SerializeField] [Range(0.1f, 5.0f)] private float m_riverDirectionBiasStrength = 2.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_riverWidenChance = 0.15f;
    [SerializeField] [Range(1, 4)] private int m_riverWidenRadiusMin = 1;
    [SerializeField] [Range(1, 6)] private int m_riverWidenRadiusMax = 2;
    [SerializeField] [Range(0, 8)] private int m_pondCount = 2;
    [SerializeField] [Range(1, 8)] private int m_pondMinRadius = 2;
    [SerializeField] [Range(1, 10)] private int m_pondMaxRadius = 4;
    [SerializeField] [Range(0, 20)] private int m_pondMinDistanceFromRiver = 4;
    [SerializeField] [Range(0, 8)] private int m_mapEdgeBuffer = 2;

    [Header("Water Distance Band Settings")]
    [SerializeField] [Range(1, 32)] private int m_nearWaterMaxDistance = 3;
    [SerializeField] [Range(2, 64)] private int m_midWaterMaxDistance = 8;

    [Header("Terrain Assignment Settings")]
    [SerializeField] [Range(0.01f, 1.0f)] private float m_terrainNoiseScale = 0.08f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_terrainNoiseStrength = 0.35f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_nearDirtBias = 0.60f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_nearGrassBias = 0.40f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_midGrassBias = 0.60f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_midDirtBias = 0.20f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_midForestFloorBias = 0.20f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_farForestFloorBias = 0.70f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_farGrassBias = 0.30f;

    [Header("Mountain Settings")]
    [SerializeField] [Range(0, 20)] private int m_mountainClusterCount = 2;
    [SerializeField] [Range(1, 200)] private int m_mountainMinClusterSize = 12;
    [SerializeField] [Range(1, 300)] private int m_mountainMaxClusterSize = 30;

    [Header("Tree Placement Settings")]
    [SerializeField] [Range(0.0f, 1.0f)] private float m_forestFloorTreeChance = 0.65f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_grassTreeChance = 0.08f;

    [Header("Validation Settings")]
    [SerializeField] private bool m_enableGenerationValidation = true;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_maxBlockedTilePercentage = 0.45f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_minOpenTilePercentage = 0.40f;
    [SerializeField] private bool m_logValidationFailures = true;

    private bool m_isGenerating = false;

    /// <summary>
    /// Supports simple debug regeneration during play for faster iteration.
    /// Uses F5 through Unity's Input System package.
    /// </summary>
    private void Update()
    {
        if (!m_enableDebugRegenerateInput)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.f5Key.wasPressedThisFrame)
        {
            return;
        }

        GenerateWorld();
    }
    
    /// <summary>
    /// Fire-and-forget generation entry point used by debug controls.
    /// GameFlowManager should call GenerateWorldAsync so it can wait for completion.
    /// </summary>
    public async void GenerateWorld()
    {
        await GenerateWorldAsync();
    }

    /// <summary>
    /// Runs the staged world-generation pipeline against the current grid.
    /// Pure generation work runs on a worker thread and produces a temporary result model first.
    /// The completed result is then applied back onto live GridTile state on the main thread.
    /// </summary>
    public async Task GenerateWorldAsync()
    {
        if (m_isGenerating)
        {
            Debug.LogWarning("World generation request ignored because generation is already in progress.", this);
            return;
        }

        if (m_gridManager == null)
        {
            Debug.LogError("WorldGenerator is missing a GridManager reference.", this);
            return;
        }

        m_isGenerating = true;

        Stopwatch totalGenerationStopwatch = Stopwatch.StartNew();

        try
        {
            if (m_gridManager.Tiles.Count == 0)
            {
                m_gridManager.GenerateGrid();
            }

            int seedToUse = ResolveSeed();
            WorldGenerationSettingsSnapshot settingsSnapshot = CaptureGenerationSettings(seedToUse);

            Debug.Log(
                "Captured generation settings snapshot. Grid: " +
                settingsSnapshot.GridWidth + "x" + settingsSnapshot.GridHeight +
                " | Seed: " + settingsSnapshot.Seed,
                this);

            ClearWorldState();

            Stopwatch backgroundGenerationStopwatch = Stopwatch.StartNew();
            WorldGenerationResult generationResult =
                await Task.Run(() => GenerateWorldResult(settingsSnapshot));
            backgroundGenerationStopwatch.Stop();

            if (m_logGenerationTimings)
            {
                Debug.Log(
                    "World generation timing | Background Generation: " +
                    backgroundGenerationStopwatch.ElapsedMilliseconds + " ms",
                    this);
            }

            Stopwatch applyStopwatch = Stopwatch.StartNew();
            ApplyGenerationResultToGrid(generationResult);
            applyStopwatch.Stop();

            if (m_logGenerationTimings)
            {
                Debug.Log(
                    "World generation timing | Result Application: " +
                    applyStopwatch.ElapsedMilliseconds + " ms",
                    this);
            }

            Stopwatch validationStopwatch = Stopwatch.StartNew();
            ValidateGeneratedWorld();
            validationStopwatch.Stop();

            if (m_logGenerationTimings)
            {
                Debug.Log(
                    "World generation timing | Validation: " +
                    validationStopwatch.ElapsedMilliseconds + " ms",
                    this);
            }

            Stopwatch visualRefreshStopwatch = Stopwatch.StartNew();
            RefreshAllVisuals();
            visualRefreshStopwatch.Stop();

            if (m_logGenerationTimings)
            {
                Debug.Log(
                    "World generation timing | Visual Refresh Total: " +
                    visualRefreshStopwatch.ElapsedMilliseconds + " ms",
                    this);
            }

            totalGenerationStopwatch.Stop();

            if (m_logGenerationTimings)
            {
                Debug.Log(
                    "World generation timing | End-to-End Total: " +
                    totalGenerationStopwatch.ElapsedMilliseconds + " ms",
                    this);
            }

            Debug.Log(
                "World generation completed with seed: " + seedToUse +
                " | Applied result tiles: " + generationResult.Count,
                this);
        }
        finally
        {
            m_isGenerating = false;
        }
    }

    /// <summary>
    /// Builds the full temporary world-generation result by running all pure generation stages
    /// against the result model instead of mutating live GridTile objects.
    /// This does not apply the result back to the grid yet.
    /// </summary>
    private WorldGenerationResult GenerateWorldResult(WorldGenerationSettingsSnapshot settingsSnapshot)
    {
        WorldGenerationResult generationResult = CreateDefaultGenerationResult();
        System.Random random = new System.Random(settingsSnapshot.Seed);

        Stopwatch totalPureGenerationStopwatch = Stopwatch.StartNew();

        Stopwatch waterStopwatch = Stopwatch.StartNew();
        GenerateWaterStage(generationResult, settingsSnapshot, random);
        waterStopwatch.Stop();
        LogPhaseTiming("Water Stage", waterStopwatch);

        Stopwatch waterDistanceStopwatch = Stopwatch.StartNew();
        ComputeWaterDistanceStage(generationResult, settingsSnapshot);
        waterDistanceStopwatch.Stop();
        LogPhaseTiming("Water Distance Stage", waterDistanceStopwatch);

        Stopwatch terrainStopwatch = Stopwatch.StartNew();
        AssignTerrainStage(generationResult, settingsSnapshot);
        terrainStopwatch.Stop();
        LogPhaseTiming("Terrain Assignment Stage", terrainStopwatch);

        Stopwatch mountainStopwatch = Stopwatch.StartNew();
        GenerateMountainStage(generationResult, settingsSnapshot, random);
        mountainStopwatch.Stop();
        LogPhaseTiming("Mountain Stage", mountainStopwatch);

        Stopwatch treeStopwatch = Stopwatch.StartNew();
        PlaceContentStage(generationResult, settingsSnapshot, random);
        treeStopwatch.Stop();
        LogPhaseTiming("Tree Placement Stage", treeStopwatch);

        Stopwatch gameplayStopwatch = Stopwatch.StartNew();
        ResolveGameplayStateStage(generationResult);
        gameplayStopwatch.Stop();
        LogPhaseTiming("Gameplay Resolution Stage", gameplayStopwatch);

        totalPureGenerationStopwatch.Stop();
        LogPhaseTiming("Pure Generation Total", totalPureGenerationStopwatch);

        if (m_logTemporaryGenerationPipeline)
        {
            Debug.Log(
                "Temporary generation pipeline completed. " +
                "Tiles: " + generationResult.Count +
                " | Water: " + CountWaterTiles(generationResult) +
                " | Trees: " + CountWorldObjects(generationResult, WorldObjectType.Tree) +
                " | Stone Blocks: " + CountBlocks(generationResult, BlockType.Stone) +
                " | Blocked: " + CountBlockedTiles(generationResult),
                this);
        }

        return generationResult;
    }

    /// <summary>
    /// Applies the completed temporary generation result back onto live GridTile state.
    /// This is the main-thread bridge between pure generation data and runtime grid data.
    /// </summary>
    private void ApplyGenerationResultToGrid(WorldGenerationResult generationResult)
    {
        if (generationResult == null)
        {
            Debug.LogError("ApplyGenerationResultToGrid failed: generationResult is null.", this);
            return;
        }

        foreach (KeyValuePair<Vector2Int, WorldGenerationTileResult> pair in generationResult.TileResults)
        {
            Vector2Int coordinates = pair.Key;
            WorldGenerationTileResult tileResult = pair.Value;

            if (tileResult == null)
            {
                continue;
            }

            if (!m_gridManager.TryGetTile(coordinates, out GridTile tile))
            {
                Debug.LogWarning("ApplyGenerationResultToGrid could not find live tile at: " + coordinates, this);
                continue;
            }

            tile.TerrainType = tileResult.TerrainType;
            tile.WaterDistanceBand = tileResult.WaterDistanceBand;
            tile.BlockType = tileResult.BlockType;
            tile.WorldObjectType = tileResult.WorldObjectType;
            tile.ContentType = tileResult.ContentType;
            tile.IsWalkable = tileResult.IsWalkable;
            tile.IsOccupied = tileResult.IsOccupied;
            tile.IsReserved = tileResult.IsReserved;
        }
    }

    /// <summary>
    /// Resolves the seed used for this generation pass.
    /// </summary>
    private int ResolveSeed()
    {
        if (!m_useRandomSeed)
        {
            return m_seed;
        }

        return Random.Range(int.MinValue, int.MaxValue);
    }

    /// <summary>
    /// Creates a default temporary generation result with one tile result per grid coordinate.
    /// This does not mutate live GridTile objects. It prepares a reusable data container
    /// for future generation stages to write into before applying results back to the grid.
    /// </summary>
    private WorldGenerationResult CreateDefaultGenerationResult()
    {
        WorldGenerationResult generationResult = new WorldGenerationResult();

        foreach (KeyValuePair<Vector2Int, GridTile> pair in m_gridManager.Tiles)
        {
            Vector2Int coordinates = pair.Key;

            WorldGenerationTileResult tileResult = new WorldGenerationTileResult
            {
                Coordinates = coordinates,
                TerrainType = TileTerrainType.Grass,
                WaterDistanceBand = TileWaterDistanceBand.None,
                BlockType = BlockType.None,
                WorldObjectType = WorldObjectType.None,
                ContentType = TileContentType.Empty,
                IsWalkable = true,
                IsOccupied = false,
                IsReserved = false,
            };

            generationResult.SetTileResult(tileResult);
        }

        return generationResult;
    }

    /// <summary>
    /// Clears mutable tile state before staged generation runs.
    /// This keeps repeated test generation predictable.
    /// Generated identity layers such as blocks and world objects are also reset here
    /// so regeneration starts from a clean world-state baseline.
    /// </summary>
    private void ClearWorldState()
    {
        foreach (GridTile tile in m_gridManager.Tiles.Values)
        {
            tile.ClearDynamicState();
            tile.TerrainType = TileTerrainType.Grass;
            tile.WaterDistanceBand = TileWaterDistanceBand.None;
            tile.BlockType = BlockType.None;
            tile.WorldObjectType = WorldObjectType.None;
            tile.ContentType = TileContentType.Empty;
            tile.IsWalkable = true;
            tile.IsOccupied = false;
            tile.IsReserved = false;
        }
    }

    /// <summary>
    /// Generates one or more primary rivers first, then adds ponds as secondary water features.
    /// This writes directly into the temporary generation result model.
    /// </summary>
    private void GenerateWaterStage(
        WorldGenerationResult generationResult,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random)
    {
        HashSet<Vector2Int> riverTiles = new HashSet<Vector2Int>();

        for (int riverIndex = 0; riverIndex < settingsSnapshot.RiverCount; riverIndex++)
        {
            GeneratePrimaryRiver(generationResult, settingsSnapshot, random, riverTiles);
        }

        PlaceSecondaryPonds(generationResult, settingsSnapshot, random, riverTiles);
    }

    /// <summary>
    /// Generates one river that starts on one edge and works toward the opposite edge with some meander.
    /// </summary>
    private void GeneratePrimaryRiver(
        WorldGenerationResult generationResult,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random,
        HashSet<Vector2Int> riverTiles)
    {
        RiverPathData riverPath = CreateRiverPathData(settingsSnapshot, random);
        Vector2Int currentCoordinates = riverPath.StartCoordinates;
        int currentWidth = NextInt(random, settingsSnapshot.RiverMinWidth, settingsSnapshot.RiverMaxWidth + 1);
        int maxSteps = Mathf.Max(settingsSnapshot.GridWidth, settingsSnapshot.GridHeight) * 3;

        for (int stepIndex = 0; stepIndex < maxSteps; stepIndex++)
        {
            CarveRiverWidth(generationResult, currentCoordinates, currentWidth, settingsSnapshot, riverTiles);

            if (NextFloat01(random) < settingsSnapshot.RiverWidenChance)
            {
                int widenRadius = NextInt(
                    random,
                    settingsSnapshot.RiverWidenRadiusMin,
                    settingsSnapshot.RiverWidenRadiusMax + 1);

                CarveRiverWidenSection(generationResult, currentCoordinates, widenRadius, settingsSnapshot, riverTiles);
            }

            if (HasReachedRiverExit(currentCoordinates, riverPath.ExitEdge, settingsSnapshot))
            {
                break;
            }

            Vector2Int nextStep = ChooseNextRiverStep(currentCoordinates, riverPath, settingsSnapshot, random);
            currentCoordinates += nextStep;
            currentCoordinates = ClampToGrid(currentCoordinates, settingsSnapshot);
        }
    }

    /// <summary>
    /// Places ponds after rivers so the river remains the primary water landmark.
    /// </summary>
    private void PlaceSecondaryPonds(
        WorldGenerationResult generationResult,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random,
        HashSet<Vector2Int> riverTiles)
    {
        if (settingsSnapshot.PondCount <= 0)
        {
            return;
        }

        int attemptsPerPond = 20;

        for (int pondIndex = 0; pondIndex < settingsSnapshot.PondCount; pondIndex++)
        {
            bool placedPond = false;

            for (int attemptIndex = 0; attemptIndex < attemptsPerPond; attemptIndex++)
            {
                Vector2Int pondCenter = GetRandomInternalCoordinates(settingsSnapshot, random);

                if (!IsFarEnoughFromRiver(pondCenter, riverTiles, settingsSnapshot.PondMinDistanceFromRiver))
                {
                    continue;
                }

                int pondRadius = NextInt(random, settingsSnapshot.PondMinRadius, settingsSnapshot.PondMaxRadius + 1);
                CarvePond(generationResult, pondCenter, pondRadius, settingsSnapshot, random);
                placedPond = true;
                break;
            }

            if (!placedPond)
            {
                Debug.LogWarning("WorldGenerator could not place a pond that satisfied spacing rules.", this);
            }
        }
    }

    /// <summary>
    /// Creates the start/end edge information for a river path.
    /// Horizontal and vertical crossings are both supported.
    /// </summary>
    private RiverPathData CreateRiverPathData(WorldGenerationSettingsSnapshot settingsSnapshot, System.Random random)
    {
        bool horizontalRiver = NextFloat01(random) < 0.5f;
        bool forwardDirection = NextFloat01(random) < 0.5f;

        if (horizontalRiver)
        {
            int startY = NextInt(random, 0, settingsSnapshot.GridHeight);
            RiverEdge startEdge = forwardDirection ? RiverEdge.Left : RiverEdge.Right;
            RiverEdge exitEdge = forwardDirection ? RiverEdge.Right : RiverEdge.Left;
            int startX = startEdge == RiverEdge.Left ? 0 : settingsSnapshot.GridWidth - 1;

            return new RiverPathData(
                new Vector2Int(startX, startY),
                startEdge,
                exitEdge,
                Vector2Int.right * (forwardDirection ? 1 : -1),
                Vector2Int.up,
                Vector2Int.down);
        }

        int startXVertical = NextInt(random, 0, settingsSnapshot.GridWidth);
        RiverEdge verticalStartEdge = forwardDirection ? RiverEdge.Bottom : RiverEdge.Top;
        RiverEdge verticalExitEdge = forwardDirection ? RiverEdge.Top : RiverEdge.Bottom;
        int startYVertical = verticalStartEdge == RiverEdge.Bottom ? 0 : settingsSnapshot.GridHeight - 1;

        return new RiverPathData(
            new Vector2Int(startXVertical, startYVertical),
            verticalStartEdge,
            verticalExitEdge,
            Vector2Int.up * (forwardDirection ? 1 : -1),
            Vector2Int.right,
            Vector2Int.left);
    }

    /// <summary>
    /// Chooses the next river step with a strong forward bias and optional sideways meander.
    /// </summary>
    private Vector2Int ChooseNextRiverStep(
        Vector2Int currentCoordinates,
        RiverPathData riverPath,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random)
    {
        List<Vector2Int> validDirections = new List<Vector2Int>();
        List<float> weights = new List<float>();

        TryAddRiverDirection(
            currentCoordinates,
            riverPath.PrimaryDirection,
            settingsSnapshot,
            settingsSnapshot.RiverDirectionBiasStrength,
            validDirections,
            weights);

        float sidewaysWeight = Mathf.Max(0.01f, settingsSnapshot.RiverMeanderChance);
        TryAddRiverDirection(
            currentCoordinates,
            riverPath.SideDirectionA,
            settingsSnapshot,
            sidewaysWeight,
            validDirections,
            weights);
        TryAddRiverDirection(
            currentCoordinates,
            riverPath.SideDirectionB,
            settingsSnapshot,
            sidewaysWeight,
            validDirections,
            weights);

        if (validDirections.Count == 0)
        {
            return riverPath.PrimaryDirection;
        }

        return GetWeightedDirection(validDirections, weights, random);
    }

    /// <summary>
    /// Adds a river movement direction if it stays inside the grid.
    /// </summary>
    private void TryAddRiverDirection(
        Vector2Int currentCoordinates,
        Vector2Int direction,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        float weight,
        List<Vector2Int> validDirections,
        List<float> weights)
    {
        Vector2Int candidateCoordinates = currentCoordinates + direction;

        if (!IsInBounds(candidateCoordinates, settingsSnapshot))
        {
            return;
        }

        validDirections.Add(direction);
        weights.Add(weight);
    }

    /// <summary>
    /// Picks one direction from weighted candidates.
    /// </summary>
    private Vector2Int GetWeightedDirection(
        List<Vector2Int> directions,
        List<float> weights,
        System.Random random)
    {
        float totalWeight = 0.0f;

        foreach (float weight in weights)
        {
            totalWeight += Mathf.Max(0.0f, weight);
        }

        if (totalWeight <= 0.0f)
        {
            return directions[0];
        }

        float roll = NextFloat(random, 0.0f, totalWeight);
        float runningTotal = 0.0f;

        for (int i = 0; i < directions.Count; i++)
        {
            runningTotal += Mathf.Max(0.0f, weights[i]);

            if (roll <= runningTotal)
            {
                return directions[i];
            }
        }

        return directions[directions.Count - 1];
    }

    /// <summary>
    /// Carves the current river tile and its immediate thickness.
    /// </summary>
    private void CarveRiverWidth(
        WorldGenerationResult generationResult,
        Vector2Int centerCoordinates,
        int width,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        HashSet<Vector2Int> riverTiles)
    {
        int radius = Mathf.Max(0, width - 1);

        for (int y = centerCoordinates.y - radius; y <= centerCoordinates.y + radius; y++)
        {
            for (int x = centerCoordinates.x - radius; x <= centerCoordinates.x + radius; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);

                if (!IsInBounds(coordinates, settingsSnapshot))
                {
                    continue;
                }

                float distanceToCenter = Vector2Int.Distance(coordinates, centerCoordinates);

                if (distanceToCenter <= radius + 0.25f)
                {
                    SetWaterTile(generationResult, coordinates);
                    riverTiles.Add(coordinates);
                }
            }
        }
    }

    /// <summary>
    /// Carves a widened section along the river to make it feel less uniform.
    /// </summary>
    private void CarveRiverWidenSection(
        WorldGenerationResult generationResult,
        Vector2Int centerCoordinates,
        int radius,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        HashSet<Vector2Int> riverTiles)
    {
        for (int y = centerCoordinates.y - radius; y <= centerCoordinates.y + radius; y++)
        {
            for (int x = centerCoordinates.x - radius; x <= centerCoordinates.x + radius; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);

                if (!IsInBounds(coordinates, settingsSnapshot))
                {
                    continue;
                }

                if (Vector2Int.Distance(coordinates, centerCoordinates) <= radius + 0.35f)
                {
                    SetWaterTile(generationResult, coordinates);
                    riverTiles.Add(coordinates);
                }
            }
        }
    }

    /// <summary>
    /// Carves a pond as a soft circular basin.
    /// </summary>
    private void CarvePond(
        WorldGenerationResult generationResult,
        Vector2Int centerCoordinates,
        int radius,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random)
    {
        for (int y = centerCoordinates.y - radius; y <= centerCoordinates.y + radius; y++)
        {
            for (int x = centerCoordinates.x - radius; x <= centerCoordinates.x + radius; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);

                if (!IsInBounds(coordinates, settingsSnapshot))
                {
                    continue;
                }

                float distanceToCenter = Vector2Int.Distance(coordinates, centerCoordinates);
                float normalizedDistance = distanceToCenter / Mathf.Max(1.0f, radius);
                float edgeNoise = NextFloat(random, -0.15f, 0.15f);

                if (normalizedDistance <= 1.0f + edgeNoise)
                {
                    SetWaterTile(generationResult, coordinates);
                }
            }
        }
    }

    /// <summary>
    /// Returns true when the candidate pond center is far enough from the primary river.
    /// </summary>
    private bool IsFarEnoughFromRiver(Vector2Int coordinates, HashSet<Vector2Int> riverTiles, int minDistance)
    {
        if (riverTiles.Count == 0 || minDistance <= 0)
        {
            return true;
        }

        int minDistanceSquared = minDistance * minDistance;

        foreach (Vector2Int riverTile in riverTiles)
        {
            int deltaX = coordinates.x - riverTile.x;
            int deltaY = coordinates.y - riverTile.y;
            int distanceSquared = (deltaX * deltaX) + (deltaY * deltaY);

            if (distanceSquared < minDistanceSquared)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true when the river has reached its target exit edge.
    /// </summary>
    private bool HasReachedRiverExit(
        Vector2Int coordinates,
        RiverEdge exitEdge,
        WorldGenerationSettingsSnapshot settingsSnapshot)
    {
        switch (exitEdge)
        {
            case RiverEdge.Left:
                return coordinates.x <= 0;
            case RiverEdge.Right:
                return coordinates.x >= settingsSnapshot.GridWidth - 1;
            case RiverEdge.Bottom:
                return coordinates.y <= 0;
            default:
                return coordinates.y >= settingsSnapshot.GridHeight - 1;
        }
    }

    /// <summary>
    /// Clamps coordinates into valid grid bounds.
    /// </summary>
    private Vector2Int ClampToGrid(Vector2Int coordinates, WorldGenerationSettingsSnapshot settingsSnapshot)
    {
        int clampedX = Mathf.Clamp(coordinates.x, 0, settingsSnapshot.GridWidth - 1);
        int clampedY = Mathf.Clamp(coordinates.y, 0, settingsSnapshot.GridHeight - 1);
        return new Vector2Int(clampedX, clampedY);
    }

    /// <summary>
    /// Result-model version of water-distance computation.
    /// This writes Near, Mid, and Far band results into the temporary generation model.
    /// </summary>
    private void ComputeWaterDistanceStage(WorldGenerationResult generationResult, WorldGenerationSettingsSnapshot settingsSnapshot)
    {
        if (generationResult == null)
        {
            return;
        }

        int nearMaxDistance = Mathf.Max(1, settingsSnapshot.NearWaterMaxDistance);
        int midMaxDistance = Mathf.Max(nearMaxDistance + 1, settingsSnapshot.MidWaterMaxDistance);

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> waterDistances = new Dictionary<Vector2Int, int>();

        foreach (KeyValuePair<Vector2Int, WorldGenerationTileResult> pair in generationResult.TileResults)
        {
            Vector2Int coordinates = pair.Key;
            WorldGenerationTileResult tileResult = pair.Value;

            if (tileResult == null)
            {
                continue;
            }

            if (tileResult.TerrainType == TileTerrainType.Water)
            {
                generationResult.SetWaterDistanceBand(coordinates, TileWaterDistanceBand.None);
                waterDistances[coordinates] = 0;
                frontier.Enqueue(coordinates);
            }
            else
            {
                generationResult.SetWaterDistanceBand(coordinates, TileWaterDistanceBand.Far);
            }
        }

        while (frontier.Count > 0)
        {
            Vector2Int currentCoordinates = frontier.Dequeue();
            int currentDistance = waterDistances[currentCoordinates];

            foreach (Vector2Int neighborCoordinates in GetOrthogonalNeighborCoordinates(currentCoordinates, settingsSnapshot))
            {
                if (!generationResult.HasTileResult(neighborCoordinates))
                {
                    continue;
                }

                if (waterDistances.ContainsKey(neighborCoordinates))
                {
                    continue;
                }

                waterDistances[neighborCoordinates] = currentDistance + 1;
                frontier.Enqueue(neighborCoordinates);
            }
        }

        foreach (KeyValuePair<Vector2Int, WorldGenerationTileResult> pair in generationResult.TileResults)
        {
            Vector2Int coordinates = pair.Key;
            WorldGenerationTileResult tileResult = pair.Value;

            if (tileResult == null)
            {
                continue;
            }

            if (tileResult.TerrainType == TileTerrainType.Water)
            {
                generationResult.SetWaterDistanceBand(coordinates, TileWaterDistanceBand.None);
                continue;
            }

            if (!waterDistances.TryGetValue(coordinates, out int distanceToWater))
            {
                generationResult.SetWaterDistanceBand(coordinates, TileWaterDistanceBand.Far);
                continue;
            }

            if (distanceToWater <= nearMaxDistance)
            {
                generationResult.SetWaterDistanceBand(coordinates, TileWaterDistanceBand.Near);
            }
            else if (distanceToWater <= midMaxDistance)
            {
                generationResult.SetWaterDistanceBand(coordinates, TileWaterDistanceBand.Mid);
            }
            else
            {
                generationResult.SetWaterDistanceBand(coordinates, TileWaterDistanceBand.Far);
            }
        }
    }

    /// <summary>
    /// Result-model version of terrain assignment.
    /// This writes terrain results into the temporary generation model based on
    /// water-distance bands and noise variation.
    /// </summary>
    private void AssignTerrainStage(WorldGenerationResult generationResult, WorldGenerationSettingsSnapshot settingsSnapshot)
    {
        if (generationResult == null)
        {
            return;
        }

        float nearDirtBias = Mathf.Max(0.0f, settingsSnapshot.NearDirtBias);
        float nearGrassBias = Mathf.Max(0.0f, settingsSnapshot.NearGrassBias);
        float nearTotalBias = Mathf.Max(0.0001f, nearDirtBias + nearGrassBias);

        float midGrassBias = Mathf.Max(0.0f, settingsSnapshot.MidGrassBias);
        float midDirtBias = Mathf.Max(0.0f, settingsSnapshot.MidDirtBias);
        float midForestFloorBias = Mathf.Max(0.0f, settingsSnapshot.MidForestFloorBias);
        float midTotalBias = Mathf.Max(0.0001f, midGrassBias + midDirtBias + midForestFloorBias);

        float farForestFloorBias = Mathf.Max(0.0f, settingsSnapshot.FarForestFloorBias);
        float farGrassBias = Mathf.Max(0.0f, settingsSnapshot.FarGrassBias);
        float farTotalBias = Mathf.Max(0.0001f, farForestFloorBias + farGrassBias);

        foreach (KeyValuePair<Vector2Int, WorldGenerationTileResult> pair in generationResult.TileResults)
        {
            Vector2Int coordinates = pair.Key;
            WorldGenerationTileResult tileResult = pair.Value;

            if (tileResult == null)
            {
                continue;
            }

            if (tileResult.TerrainType == TileTerrainType.Water)
            {
                continue;
            }

            float noiseValue = Mathf.PerlinNoise(
                (coordinates.x + settingsSnapshot.Seed) * settingsSnapshot.TerrainNoiseScale,
                (coordinates.y + settingsSnapshot.Seed) * settingsSnapshot.TerrainNoiseScale);

            float noiseOffset = (noiseValue - 0.5f) * 2.0f * settingsSnapshot.TerrainNoiseStrength;

            switch (tileResult.WaterDistanceBand)
            {
                case TileWaterDistanceBand.Near:
                {
                    float dirtWeight = Mathf.Max(0.0f, nearDirtBias + noiseOffset);
                    float grassWeight = Mathf.Max(0.0f, nearGrassBias - noiseOffset);

                    if (dirtWeight + grassWeight <= 0.0001f)
                    {
                        dirtWeight = nearDirtBias / nearTotalBias;
                        grassWeight = nearGrassBias / nearTotalBias;
                    }

                    generationResult.SetTerrainType(
                        coordinates,
                        dirtWeight >= grassWeight ? TileTerrainType.Dirt : TileTerrainType.Grass);

                    break;
                }

                case TileWaterDistanceBand.Mid:
                {
                    float grassWeight = Mathf.Max(0.0f, settingsSnapshot.MidGrassBias + (noiseOffset * 0.5f));
                    float dirtWeight = Mathf.Max(0.0f, settingsSnapshot.MidDirtBias - noiseOffset);
                    float forestFloorWeight = Mathf.Max(0.0f, settingsSnapshot.MidForestFloorBias + noiseOffset);

                    if (grassWeight + dirtWeight + forestFloorWeight <= 0.0001f)
                    {
                        grassWeight = midGrassBias / midTotalBias;
                        dirtWeight = midDirtBias / midTotalBias;
                        forestFloorWeight = midForestFloorBias / midTotalBias;
                    }

                    generationResult.SetTerrainType(
                        coordinates,
                        GetWeightedMidTerrain(grassWeight, dirtWeight, forestFloorWeight));

                    break;
                }

                case TileWaterDistanceBand.Far:
                {
                    float forestFloorWeight = Mathf.Max(0.0f, farForestFloorBias + noiseOffset);
                    float grassWeight = Mathf.Max(0.0f, farGrassBias - noiseOffset);

                    if (forestFloorWeight + grassWeight <= 0.0001f)
                    {
                        forestFloorWeight = farForestFloorBias / farTotalBias;
                        grassWeight = farGrassBias / farTotalBias;
                    }

                    generationResult.SetTerrainType(
                        coordinates,
                        forestFloorWeight >= grassWeight ? TileTerrainType.ForestFloor : TileTerrainType.Grass);

                    break;
                }

                default:
                {
                    generationResult.SetTerrainType(coordinates, TileTerrainType.Grass);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Chooses a mid-band terrain type from weighted values.
    /// Mid-distance land is intended to favor Grass while still allowing some Dirt and ForestFloor.
    /// </summary>
    private TileTerrainType GetWeightedMidTerrain(float grassWeight, float dirtWeight, float forestFloorWeight)
    {
        if (grassWeight >= dirtWeight && grassWeight >= forestFloorWeight)
        {
            return TileTerrainType.Grass;
        }

        if (dirtWeight >= forestFloorWeight)
        {
            return TileTerrainType.Dirt;
        }

        return TileTerrainType.ForestFloor;
    }

    /// <summary>
    /// Result-model version of mountain generation.
    /// This writes mountain block results into the temporary generation model.
    /// </summary>
    private void GenerateMountainStage(
        WorldGenerationResult generationResult,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random)
    {
        if (generationResult == null)
        {
            return;
        }

        if (settingsSnapshot.MountainClusterCount <= 0)
        {
            return;
        }

        int minClusterSize = Mathf.Max(1, settingsSnapshot.MountainMinClusterSize);
        int maxClusterSize = Mathf.Max(minClusterSize, settingsSnapshot.MountainMaxClusterSize);

        for (int clusterIndex = 0; clusterIndex < settingsSnapshot.MountainClusterCount; clusterIndex++)
        {
            int targetClusterSize = NextInt(random, minClusterSize, maxClusterSize + 1);
            TryGenerateMountainCluster(generationResult, targetClusterSize, settingsSnapshot, random);
        }
    }

    /// <summary>
    /// Result-model version of mountain cluster generation.
    /// A cluster starts from a valid seed tile and grows through valid orthogonal neighbors.
    /// </summary>
    private void TryGenerateMountainCluster(
        WorldGenerationResult generationResult,
        int targetClusterSize,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random)
    {
        List<Vector2Int> validSeedTiles = new List<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, WorldGenerationTileResult> pair in generationResult.TileResults)
        {
            if (IsValidMountainSeed(generationResult, pair.Key))
            {
                validSeedTiles.Add(pair.Key);
            }
        }

        if (validSeedTiles.Count == 0)
        {
            return;
        }

        Vector2Int seedCoordinates = validSeedTiles[NextInt(random, 0, validSeedTiles.Count)];
        HashSet<Vector2Int> clusterTiles = new HashSet<Vector2Int>();
        List<Vector2Int> frontier = new List<Vector2Int>();

        clusterTiles.Add(seedCoordinates);
        frontier.Add(seedCoordinates);

        while (clusterTiles.Count < targetClusterSize && frontier.Count > 0)
        {
            Vector2Int currentCoordinates = frontier[NextInt(random, 0, frontier.Count)];
            List<Vector2Int> validGrowthTiles = new List<Vector2Int>();

            foreach (Vector2Int neighborCoordinates in GetOrthogonalNeighborCoordinates(currentCoordinates, settingsSnapshot))
            {
                if (!generationResult.HasTileResult(neighborCoordinates))
                {
                    continue;
                }

                if (clusterTiles.Contains(neighborCoordinates))
                {
                    continue;
                }

                if (!IsValidMountainGrowthTile(generationResult, neighborCoordinates))
                {
                    continue;
                }

                validGrowthTiles.Add(neighborCoordinates);
            }

            if (validGrowthTiles.Count == 0)
            {
                frontier.Remove(currentCoordinates);
                continue;
            }

            Vector2Int nextCoordinates = validGrowthTiles[NextInt(random, 0, validGrowthTiles.Count)];
            clusterTiles.Add(nextCoordinates);
            frontier.Add(nextCoordinates);
        }

        foreach (Vector2Int coordinates in clusterTiles)
        {
            ApplyMountainTile(generationResult, coordinates);
        }
    }

    /// <summary>
    /// Result-model version of mountain seed validation.
    /// Seeds must be on land and not already contain a structural block.
    /// </summary>
    private bool IsValidMountainSeed(WorldGenerationResult generationResult, Vector2Int coordinates)
    {
        if (generationResult == null || !generationResult.HasTileResult(coordinates))
        {
            return false;
        }

        if (generationResult.GetTerrainType(coordinates) == TileTerrainType.Water)
        {
            return false;
        }

        if (generationResult.GetBlockType(coordinates) != BlockType.None)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Result-model version of mountain growth validation.
    /// Growth tiles follow the same first-pass rules as mountain seeds.
    /// </summary>
    private bool IsValidMountainGrowthTile(WorldGenerationResult generationResult, Vector2Int coordinates)
    {
        return IsValidMountainSeed(generationResult, coordinates);
    }

    /// <summary>
    /// Result-model version of mountain tile application.
    /// Applies Stone block data and blocked movement into the temporary generation model.
    /// </summary>
    private void ApplyMountainTile(WorldGenerationResult generationResult, Vector2Int coordinates)
    {
        if (generationResult == null || !generationResult.HasTileResult(coordinates))
        {
            return;
        }

        generationResult.SetBlockType(coordinates, BlockType.Stone);
        generationResult.SetWalkable(coordinates, false);
        generationResult.SetOccupied(coordinates, false);
        generationResult.SetReserved(coordinates, false);
    }

    /// <summary>
    /// Result-model version of world-object placement.
    /// Trees strongly prefer ForestFloor, may rarely appear on Grass, and do not appear
    /// on Dirt, Water, blocked tiles, or tiles that already contain another world object.
    /// </summary>
    private void PlaceContentStage(
        WorldGenerationResult generationResult,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random)
    {
        if (generationResult == null)
        {
            return;
        }

        foreach (KeyValuePair<Vector2Int, WorldGenerationTileResult> pair in generationResult.TileResults)
        {
            Vector2Int coordinates = pair.Key;

            if (!CanPlaceTreeAt(generationResult, coordinates))
            {
                continue;
            }

            if (!ShouldPlaceTreeAt(generationResult, coordinates, settingsSnapshot, random))
            {
                continue;
            }

            generationResult.SetWorldObjectType(coordinates, WorldObjectType.Tree);
        }
    }

    /// <summary>
    /// Result-model version of tree placement validation.
    /// Trees cannot be placed on water, dirt, block tiles, or tiles that already
    /// contain another world object.
    /// </summary>
    private bool CanPlaceTreeAt(WorldGenerationResult generationResult, Vector2Int coordinates)
    {
        if (generationResult == null || !generationResult.HasTileResult(coordinates))
        {
            return false;
        }

        if (generationResult.GetTerrainType(coordinates) == TileTerrainType.Water)
        {
            return false;
        }

        if (generationResult.GetTerrainType(coordinates) == TileTerrainType.Dirt)
        {
            return false;
        }

        if (generationResult.GetBlockType(coordinates) != BlockType.None)
        {
            return false;
        }

        if (generationResult.GetWorldObjectType(coordinates) != WorldObjectType.None)
        {
            return false;
        }

        TileTerrainType terrainType = generationResult.GetTerrainType(coordinates);

        return terrainType == TileTerrainType.ForestFloor ||
               terrainType == TileTerrainType.Grass;
    }

    /// <summary>
    /// Result-model version of tree placement chance evaluation.
    /// ForestFloor strongly favors trees, while Grass only rarely receives them.
    /// </summary>
    private bool ShouldPlaceTreeAt(
        WorldGenerationResult generationResult,
        Vector2Int coordinates,
        WorldGenerationSettingsSnapshot settingsSnapshot,
        System.Random random)
    {
        if (generationResult == null || !generationResult.HasTileResult(coordinates))
        {
            return false;
        }

        TileTerrainType terrainType = generationResult.GetTerrainType(coordinates);

        switch (terrainType)
        {
            case TileTerrainType.ForestFloor:
                return NextFloat01(random) < settingsSnapshot.ForestFloorTreeChance;
            case TileTerrainType.Grass:
                return NextFloat01(random) < settingsSnapshot.GrassTreeChance;
            default:
                return false;
        }
    }

    /// <summary>
    /// Result-model version of gameplay-state resolution.
    /// Trees are currently treated as natural blockers in the first pass.
    /// This stage converts generated terrain/content results into gameplay-facing tile behavior.
    /// </summary>
    private void ResolveGameplayStateStage(WorldGenerationResult generationResult)
    {
        if (generationResult == null)
        {
            return;
        }

        foreach (KeyValuePair<Vector2Int, WorldGenerationTileResult> pair in generationResult.TileResults)
        {
            Vector2Int coordinates = pair.Key;
            WorldGenerationTileResult tileResult = pair.Value;

            if (tileResult == null)
            {
                continue;
            }

            if (tileResult.WorldObjectType == WorldObjectType.Tree)
            {
                generationResult.SetContentType(coordinates, TileContentType.NaturalBlocker);
                generationResult.SetWalkable(coordinates, false);
                generationResult.SetOccupied(coordinates, false);
                generationResult.SetReserved(coordinates, false);
            }
        }
    }

    /// <summary>
    /// Validates that the generated map stays broadly playable based on blocked/open tile thresholds.
    /// Returns true when the generated result passes the current validation settings.
    /// </summary>
    private bool ValidateGeneratedWorld()
    {
        if (!m_enableGenerationValidation)
        {
            return true;
        }

        int totalTileCount = m_gridManager.Tiles.Count;

        if (totalTileCount <= 0)
        {
            if (m_logValidationFailures)
            {
                Debug.LogWarning("World generation validation failed: grid contains no tiles.", this);
            }

            return false;
        }

        int blockedTileCount = CountBlockedTiles();
        int openTileCount = CountOpenTiles();

        float blockedTilePercentage = (float)blockedTileCount / totalTileCount;
        float openTilePercentage = (float)openTileCount / totalTileCount;

        bool passesBlockedThreshold = blockedTilePercentage <= m_maxBlockedTilePercentage;
        bool passesOpenThreshold = openTilePercentage >= m_minOpenTilePercentage;
        bool isValid = passesBlockedThreshold && passesOpenThreshold;

        if (!isValid && m_logValidationFailures)
        {
            Debug.LogWarning(
                "World generation validation failed. " +
                "Blocked: " + blockedTileCount + "/" + totalTileCount +
                " (" + blockedTilePercentage.ToString("P1") + ")" +
                " | Open: " + openTileCount + "/" + totalTileCount +
                " (" + openTilePercentage.ToString("P1") + ")" +
                " | Max Blocked Allowed: " + m_maxBlockedTilePercentage.ToString("P1") +
                " | Min Open Required: " + m_minOpenTilePercentage.ToString("P1"),
                this);
        }

        return isValid;
    }

    /// <summary>
    /// Counts how many tiles are currently non-walkable after gameplay-state resolution.
    /// </summary>
    private int CountBlockedTiles()
    {
        int blockedTileCount = 0;

        foreach (GridTile tile in m_gridManager.Tiles.Values)
        {
            if (tile == null)
            {
                continue;
            }

            if (!tile.IsWalkable)
            {
                ++blockedTileCount;
            }
        }

        return blockedTileCount;
    }

    /// <summary>
    /// Counts how many tiles in the temporary result are non-walkable.
    /// </summary>
    private int CountBlockedTiles(WorldGenerationResult generationResult)
    {
        if (generationResult == null)
        {
            return 0;
        }

        int blockedTileCount = 0;

        foreach (WorldGenerationTileResult tileResult in generationResult.TileResults.Values)
        {
            if (tileResult == null)
            {
                continue;
            }

            if (!tileResult.IsWalkable)
            {
                ++blockedTileCount;
            }
        }

        return blockedTileCount;
    }

    /// <summary>
    /// Counts how many tiles are currently walkable after gameplay-state resolution.
    /// </summary>
    private int CountOpenTiles()
    {
        int openTileCount = 0;

        foreach (GridTile tile in m_gridManager.Tiles.Values)
        {
            if (tile == null)
            {
                continue;
            }

            if (tile.IsWalkable)
            {
                ++openTileCount;
            }
        }

        return openTileCount;
    }

    /// <summary>
    /// Counts block instances of the requested type in the temporary result.
    /// </summary>
    private int CountBlocks(WorldGenerationResult generationResult, BlockType blockType)
    {
        if (generationResult == null)
        {
            return 0;
        }

        int count = 0;

        foreach (WorldGenerationTileResult tileResult in generationResult.TileResults.Values)
        {
            if (tileResult == null)
            {
                continue;
            }

            if (tileResult.BlockType == blockType)
            {
                ++count;
            }
        }

        return count;
    }

    /// <summary>
    /// Counts world objects of the requested type in the temporary result.
    /// </summary>
    private int CountWorldObjects(WorldGenerationResult generationResult, WorldObjectType worldObjectType)
    {
        if (generationResult == null)
        {
            return 0;
        }

        int count = 0;

        foreach (WorldGenerationTileResult tileResult in generationResult.TileResults.Values)
        {
            if (tileResult == null)
            {
                continue;
            }

            if (tileResult.WorldObjectType == worldObjectType)
            {
                ++count;
            }
        }

        return count;
    }

    /// <summary>
    /// Refreshes the terrain renderer after generation completes.
    /// This is optional so world generation can still run without a visual layer assigned.
    /// </summary>
    private void RefreshTerrainRenderer()
    {
        if (m_worldTerrainRenderer == null)
        {
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        m_worldTerrainRenderer.RenderTerrain();
        stopwatch.Stop();

        LogPhaseTiming("Terrain Refresh", stopwatch);
    }

    /// <summary>
    /// Refreshes rendered block visuals after generation completes.
    /// This is optional so world generation can still run without a block visual layer assigned.
    /// </summary>
    private void RefreshBlockRenderer()
    {
        if (m_blockRenderer == null)
        {
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        m_blockRenderer.RebuildBlocks();
        stopwatch.Stop();

        LogPhaseTiming("Block Refresh", stopwatch);
    }

    /// <summary>
    /// Refreshes rendered world object visuals after generation completes.
    /// This is optional so world generation can still run without a world object visual layer assigned.
    /// </summary>
    private void RefreshWorldObjectRenderer()
    {
        if (m_worldObjectRenderer == null)
        {
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        m_worldObjectRenderer.RebuildWorldObjects();
        stopwatch.Stop();

        LogPhaseTiming("World Object Refresh", stopwatch);
    }

    /// <summary>
    /// Refreshes all scene visuals after live tile state has been updated.
    /// This is the main-thread-only visual end phase.
    /// </summary>
    private void RefreshAllVisuals()
    {
        RefreshTerrainRenderer();
        RefreshBlockRenderer();
        RefreshWorldObjectRenderer();
    }

    /// <summary>
    /// Logs a timing entry when generation timing output is enabled.
    /// </summary>
    private void LogPhaseTiming(string phaseName, Stopwatch stopwatch)
    {
        if (!m_logGenerationTimings)
        {
            return;
        }

        Debug.Log(
            "World generation timing | " + phaseName + ": " +
            stopwatch.ElapsedMilliseconds + " ms",
            this);
    }

    /// <summary>
    /// Returns whether coordinates are inside the snapshot-defined grid bounds.
    /// This is worker-safe because it depends only on captured data.
    /// </summary>
    private bool IsInBounds(Vector2Int coordinates, WorldGenerationSettingsSnapshot settingsSnapshot)
    {
        return coordinates.x >= 0 &&
               coordinates.x < settingsSnapshot.GridWidth &&
               coordinates.y >= 0 &&
               coordinates.y < settingsSnapshot.GridHeight;
    }

    /// <summary>
    /// Returns the orthogonal neighbors using only snapshot-defined grid bounds.
    /// This is worker-safe because it depends only on captured data.
    /// </summary>
    private List<Vector2Int> GetOrthogonalNeighborCoordinates(Vector2Int coordinates, WorldGenerationSettingsSnapshot settingsSnapshot)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(4);

        Vector2Int up = new Vector2Int(coordinates.x, coordinates.y + 1);
        Vector2Int right = new Vector2Int(coordinates.x + 1, coordinates.y);
        Vector2Int down = new Vector2Int(coordinates.x, coordinates.y - 1);
        Vector2Int left = new Vector2Int(coordinates.x - 1, coordinates.y);

        if (IsInBounds(up, settingsSnapshot))
        {
            neighbors.Add(up);
        }

        if (IsInBounds(right, settingsSnapshot))
        {
            neighbors.Add(right);
        }

        if (IsInBounds(down, settingsSnapshot))
        {
            neighbors.Add(down);
        }

        if (IsInBounds(left, settingsSnapshot))
        {
            neighbors.Add(left);
        }

        return neighbors;
    }

    /// <summary>
    /// Returns a random tile coordinate away from the outer edge.
    /// </summary>
    private Vector2Int GetRandomInternalCoordinates(WorldGenerationSettingsSnapshot settingsSnapshot, System.Random random)
    {
        int minX = settingsSnapshot.MapEdgeBuffer;
        int maxX = settingsSnapshot.GridWidth - 1 - settingsSnapshot.MapEdgeBuffer;
        int minY = settingsSnapshot.MapEdgeBuffer;
        int maxY = settingsSnapshot.GridHeight - 1 - settingsSnapshot.MapEdgeBuffer;

        if (minX > maxX || minY > maxY)
        {
            minX = 0;
            maxX = Mathf.Max(0, settingsSnapshot.GridWidth - 1);
            minY = 0;
            maxY = Mathf.Max(0, settingsSnapshot.GridHeight - 1);
        }

        int x = NextInt(random, minX, maxX + 1);
        int y = NextInt(random, minY, maxY + 1);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Returns an integer in the half-open range [minInclusive, maxExclusive).
    /// Uses a local deterministic RNG suitable for worker-thread generation later.
    /// </summary>
    private int NextInt(System.Random random, int minInclusive, int maxExclusive)
    {
        return random.Next(minInclusive, maxExclusive);
    }

    /// <summary>
    /// Returns a float in the range [0, 1).
    /// Uses a local deterministic RNG suitable for worker-thread generation later.
    /// </summary>
    private float NextFloat01(System.Random random)
    {
        return (float)random.NextDouble();
    }

    /// <summary>
    /// Returns a float in the range [minInclusive, maxInclusive).
    /// Uses a local deterministic RNG suitable for worker-thread generation later.
    /// </summary>
    private float NextFloat(System.Random random, float minInclusive, float maxInclusive)
    {
        return minInclusive + ((float)random.NextDouble() * (maxInclusive - minInclusive));
    }

    /// <summary>
    /// Result-model version of water marking.
    /// Water is treated as non-walkable and cleared of dynamic pawn state.
    /// </summary>
    private bool SetWaterTile(WorldGenerationResult generationResult, Vector2Int coordinates)
    {
        if (generationResult == null || !generationResult.HasTileResult(coordinates))
        {
            return false;
        }

        generationResult.SetTerrainType(coordinates, TileTerrainType.Water);
        generationResult.SetWalkable(coordinates, false);
        generationResult.SetOccupied(coordinates, false);
        generationResult.SetReserved(coordinates, false);
        return true;
    }

    /// <summary>
    /// Result-model version of water counting.
    /// </summary>
    private int CountWaterTiles(WorldGenerationResult generationResult)
    {
        if (generationResult == null)
        {
            return 0;
        }

        int waterTileCount = 0;

        foreach (WorldGenerationTileResult tileResult in generationResult.TileResults.Values)
        {
            if (tileResult == null)
            {
                continue;
            }

            if (tileResult.TerrainType == TileTerrainType.Water)
            {
                ++waterTileCount;
            }
        }

        return waterTileCount;
    }

    /// <summary>
    /// Captures the current world-generation settings into a plain data snapshot.
    /// This does not start threading yet. It only prepares safe input data.
    /// </summary>
    private WorldGenerationSettingsSnapshot CaptureGenerationSettings(int resolvedSeed)
    {
        WorldGenerationSettingsSnapshot snapshot = new WorldGenerationSettingsSnapshot
        {
            GridWidth = m_gridManager.GridWidth,
            GridHeight = m_gridManager.GridHeight,
            Seed = resolvedSeed,

            RiverCount = m_riverCount,
            RiverMinWidth = m_riverMinWidth,
            RiverMaxWidth = m_riverMaxWidth,
            RiverMeanderChance = m_riverMeanderChance,
            RiverDirectionBiasStrength = m_riverDirectionBiasStrength,
            RiverWidenChance = m_riverWidenChance,
            RiverWidenRadiusMin = m_riverWidenRadiusMin,
            RiverWidenRadiusMax = m_riverWidenRadiusMax,
            PondCount = m_pondCount,
            PondMinRadius = m_pondMinRadius,
            PondMaxRadius = m_pondMaxRadius,
            PondMinDistanceFromRiver = m_pondMinDistanceFromRiver,
            MapEdgeBuffer = m_mapEdgeBuffer,

            NearWaterMaxDistance = m_nearWaterMaxDistance,
            MidWaterMaxDistance = m_midWaterMaxDistance,

            TerrainNoiseScale = m_terrainNoiseScale,
            TerrainNoiseStrength = m_terrainNoiseStrength,
            NearDirtBias = m_nearDirtBias,
            NearGrassBias = m_nearGrassBias,
            MidGrassBias = m_midGrassBias,
            MidDirtBias = m_midDirtBias,
            MidForestFloorBias = m_midForestFloorBias,
            FarForestFloorBias = m_farForestFloorBias,
            FarGrassBias = m_farGrassBias,

            MountainClusterCount = m_mountainClusterCount,
            MountainMinClusterSize = m_mountainMinClusterSize,
            MountainMaxClusterSize = m_mountainMaxClusterSize,

            ForestFloorTreeChance = m_forestFloorTreeChance,
            GrassTreeChance = m_grassTreeChance,
        };

        return snapshot;
    }

    /// <summary>
    /// Captured generation settings that can later be passed safely into worker-thread generation.
    /// This is plain data only.
    /// </summary>
    private struct WorldGenerationSettingsSnapshot
    {
        public int GridWidth;
        public int GridHeight;
        public int Seed;

        public int RiverCount;
        public int RiverMinWidth;
        public int RiverMaxWidth;
        public float RiverMeanderChance;
        public float RiverDirectionBiasStrength;
        public float RiverWidenChance;
        public int RiverWidenRadiusMin;
        public int RiverWidenRadiusMax;
        public int PondCount;
        public int PondMinRadius;
        public int PondMaxRadius;
        public int PondMinDistanceFromRiver;
        public int MapEdgeBuffer;

        public int NearWaterMaxDistance;
        public int MidWaterMaxDistance;

        public float TerrainNoiseScale;
        public float TerrainNoiseStrength;
        public float NearDirtBias;
        public float NearGrassBias;
        public float MidGrassBias;
        public float MidDirtBias;
        public float MidForestFloorBias;
        public float FarForestFloorBias;
        public float FarGrassBias;

        public int MountainClusterCount;
        public int MountainMinClusterSize;
        public int MountainMaxClusterSize;

        public float ForestFloorTreeChance;
        public float GrassTreeChance;
    }

    /// <summary>
    /// Stores edge-to-edge river path data.
    /// </summary>
    private readonly struct RiverPathData
    {
        public Vector2Int StartCoordinates { get; }
        public RiverEdge StartEdge { get; }
        public RiverEdge ExitEdge { get; }
        public Vector2Int PrimaryDirection { get; }
        public Vector2Int SideDirectionA { get; }
        public Vector2Int SideDirectionB { get; }

        public RiverPathData(
            Vector2Int startCoordinates,
            RiverEdge startEdge,
            RiverEdge exitEdge,
            Vector2Int primaryDirection,
            Vector2Int sideDirectionA,
            Vector2Int sideDirectionB)
        {
            StartCoordinates = startCoordinates;
            StartEdge = startEdge;
            ExitEdge = exitEdge;
            PrimaryDirection = primaryDirection;
            SideDirectionA = sideDirectionA;
            SideDirectionB = sideDirectionB;
        }
    }

    /// <summary>
    /// Edge identifiers used by river path generation.
    /// </summary>
    private enum RiverEdge
    {
        Left,
        Right,
        Bottom,
        Top
    }
}