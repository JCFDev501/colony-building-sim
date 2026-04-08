using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Covers neighbor lookup and diagonal traversal rules for GridManager.
/// These tests focus on deterministic grid logic that future pathfinding will rely on.
/// </summary>
public class GridManagerNeighborTests
{
    private GameObject m_gridManagerObject;
    private GridManager m_gridManager;

    [SetUp]
    public void SetUp()
    {
        m_gridManagerObject = new GameObject("GridManager_Test");
        m_gridManager = m_gridManagerObject.AddComponent<GridManager>();

        SetPrivateField("m_gridWidth", 3);
        SetPrivateField("m_gridHeight", 3);
        SetPrivateField("m_cellSize", 1.0f);
        SetPrivateField("m_generateOnStart", false);

        m_gridManager.GenerateGrid();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(m_gridManagerObject);
    }

    [Test]
    public void GetAllNeighborCoordinates_CenterTile_ReturnsEightNeighbors()
    {
        Vector2Int coordinates = new Vector2Int(1, 1);

        List<Vector2Int> neighbors = m_gridManager.GetAllNeighborCoordinates(coordinates);

        Assert.That(neighbors.Count, Is.EqualTo(8));
        Assert.That(neighbors, Does.Contain(new Vector2Int(1, 2)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(2, 1)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(1, 0)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(0, 1)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(0, 2)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(2, 2)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(2, 0)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(0, 0)));
    }

    [Test]
    public void GetAllNeighborCoordinates_CornerTile_ReturnsThreeNeighbors()
    {
        Vector2Int coordinates = new Vector2Int(0, 0);

        List<Vector2Int> neighbors = m_gridManager.GetAllNeighborCoordinates(coordinates);

        Assert.That(neighbors.Count, Is.EqualTo(3));
        Assert.That(neighbors, Does.Contain(new Vector2Int(0, 1)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(1, 0)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(1, 1)));
    }

    [Test]
    public void GetAllNeighborCoordinates_EdgeTile_ReturnsFiveNeighbors()
    {
        Vector2Int coordinates = new Vector2Int(1, 0);

        List<Vector2Int> neighbors = m_gridManager.GetAllNeighborCoordinates(coordinates);

        Assert.That(neighbors.Count, Is.EqualTo(5));
        Assert.That(neighbors, Does.Contain(new Vector2Int(0, 0)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(2, 0)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(1, 1)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(0, 1)));
        Assert.That(neighbors, Does.Contain(new Vector2Int(2, 1)));
    }

    [Test]
    public void IsOrthogonalStep_AdjacentCardinalTiles_ReturnsTrue()
    {
        bool result = m_gridManager.IsOrthogonalStep(new Vector2Int(1, 1), new Vector2Int(1, 2));

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsOrthogonalStep_DiagonalTiles_ReturnsFalse()
    {
        bool result = m_gridManager.IsOrthogonalStep(new Vector2Int(1, 1), new Vector2Int(2, 2));

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsDiagonalStep_DiagonalTiles_ReturnsTrue()
    {
        bool result = m_gridManager.IsDiagonalStep(new Vector2Int(1, 1), new Vector2Int(2, 2));

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsDiagonalStep_OrthogonalTiles_ReturnsFalse()
    {
        bool result = m_gridManager.IsDiagonalStep(new Vector2Int(1, 1), new Vector2Int(1, 2));

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsDiagonalTraversalValid_ClearDiagonalPath_ReturnsTrue()
    {
        Vector2Int fromCoordinates = new Vector2Int(1, 1);
        Vector2Int toCoordinates = new Vector2Int(2, 2);

        bool result = m_gridManager.IsDiagonalTraversalValid(fromCoordinates, toCoordinates);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsDiagonalTraversalValid_BlockedTarget_ReturnsFalse()
    {
        Vector2Int fromCoordinates = new Vector2Int(1, 1);
        Vector2Int toCoordinates = new Vector2Int(2, 2);

        m_gridManager.SetWalkable(toCoordinates, false);

        bool result = m_gridManager.IsDiagonalTraversalValid(fromCoordinates, toCoordinates);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsDiagonalTraversalValid_BlockedHorizontalSide_ReturnsFalse()
    {
        Vector2Int fromCoordinates = new Vector2Int(1, 1);
        Vector2Int toCoordinates = new Vector2Int(2, 2);
        Vector2Int horizontalSideCoordinates = new Vector2Int(2, 1);

        m_gridManager.SetWalkable(horizontalSideCoordinates, false);

        bool result = m_gridManager.IsDiagonalTraversalValid(fromCoordinates, toCoordinates);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsDiagonalTraversalValid_BlockedVerticalSide_ReturnsFalse()
    {
        Vector2Int fromCoordinates = new Vector2Int(1, 1);
        Vector2Int toCoordinates = new Vector2Int(2, 2);
        Vector2Int verticalSideCoordinates = new Vector2Int(1, 2);

        m_gridManager.SetWalkable(verticalSideCoordinates, false);

        bool result = m_gridManager.IsDiagonalTraversalValid(fromCoordinates, toCoordinates);

        Assert.That(result, Is.False);
    }

    [Test]
    public void GetTraversableNeighborCoordinates_ClearCenterTile_ReturnsEightTraversableNeighbors()
    {
        Vector2Int coordinates = new Vector2Int(1, 1);

        List<Vector2Int> neighbors = m_gridManager.GetTraversableNeighborCoordinates(coordinates);

        Assert.That(neighbors.Count, Is.EqualTo(8));
    }

    [Test]
    public void GetTraversableNeighborCoordinates_BlockedSideTile_RemovesInvalidDiagonal()
    {
        Vector2Int coordinates = new Vector2Int(1, 1);
        Vector2Int blockedSideCoordinates = new Vector2Int(2, 1);
        Vector2Int blockedDiagonalCoordinates = new Vector2Int(2, 2);

        m_gridManager.SetWalkable(blockedSideCoordinates, false);

        List<Vector2Int> neighbors = m_gridManager.GetTraversableNeighborCoordinates(coordinates);

        Assert.That(neighbors, Has.No.Member(blockedDiagonalCoordinates));
    }

    private void SetPrivateField(string fieldName, object value)
    {
        System.Reflection.FieldInfo fieldInfo = typeof(GridManager).GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);

        Assert.That(fieldInfo, Is.Not.Null, "Could not find field: " + fieldName);
        fieldInfo.SetValue(m_gridManager, value);
    }
}