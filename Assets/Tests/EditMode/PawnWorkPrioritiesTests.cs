using NUnit.Framework;

/// <summary>
/// Covers per-pawn work priority rules for PawnWorkPriorities.
/// These tests focus on deterministic priority lookup, clamping, assignment, and work-type comparison.
/// </summary>
public class PawnWorkPrioritiesTests
{
    private PawnWorkPriorities m_workPriorities;

    [SetUp]
    public void SetUp()
    {
        m_workPriorities = new PawnWorkPriorities();
    }

    [Test]
    public void Constructor_DefaultPriorities_AreAllFour()
    {
        Assert.That(m_workPriorities.PlantPriority, Is.EqualTo(4));
        Assert.That(m_workPriorities.CutPriority, Is.EqualTo(4));
        Assert.That(m_workPriorities.ConstructPriority, Is.EqualTo(4));
        Assert.That(m_workPriorities.CookPriority, Is.EqualTo(4));
        Assert.That(m_workPriorities.CraftPriority, Is.EqualTo(4));
    }

    [Test]
    public void SetPriority_ValidPriority_StoresValue()
    {
        m_workPriorities.SetPriority(WorkType.Plant, 1);

        Assert.That(m_workPriorities.PlantPriority, Is.EqualTo(1));
    }

    [Test]
    public void SetPriority_BelowMinimum_ClampsToOne()
    {
        m_workPriorities.SetPriority(WorkType.Cook, 0);

        Assert.That(m_workPriorities.CookPriority, Is.EqualTo(1));
    }

    [Test]
    public void SetPriority_AboveMaximum_ClampsToFour()
    {
        m_workPriorities.SetPriority(WorkType.Craft, 99);

        Assert.That(m_workPriorities.CraftPriority, Is.EqualTo(4));
    }

    [Test]
    public void PropertySetter_BelowMinimum_ClampsToOne()
    {
        m_workPriorities.CutPriority = -10;

        Assert.That(m_workPriorities.CutPriority, Is.EqualTo(1));
    }

    [Test]
    public void PropertySetter_AboveMaximum_ClampsToFour()
    {
        m_workPriorities.ConstructPriority = 20;

        Assert.That(m_workPriorities.ConstructPriority, Is.EqualTo(4));
    }

    [Test]
    public void GetPriority_EachWorkType_ReturnsMatchingPriority()
    {
        m_workPriorities.SetPriority(WorkType.Plant, 1);
        m_workPriorities.SetPriority(WorkType.Cut, 2);
        m_workPriorities.SetPriority(WorkType.Construct, 3);
        m_workPriorities.SetPriority(WorkType.Cook, 4);
        m_workPriorities.SetPriority(WorkType.Craft, 1);

        Assert.That(m_workPriorities.GetPriority(WorkType.Plant), Is.EqualTo(1));
        Assert.That(m_workPriorities.GetPriority(WorkType.Cut), Is.EqualTo(2));
        Assert.That(m_workPriorities.GetPriority(WorkType.Construct), Is.EqualTo(3));
        Assert.That(m_workPriorities.GetPriority(WorkType.Cook), Is.EqualTo(4));
        Assert.That(m_workPriorities.GetPriority(WorkType.Craft), Is.EqualTo(1));
    }

    [Test]
    public void CompareWorkTypes_EqualPriority_LowerEnumIndexWins()
    {
        int result = m_workPriorities.CompareWorkTypes(WorkType.Plant, WorkType.Cut);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void CompareWorkTypes_FirstWorkTypeHasBetterPriority_FirstWins()
    {
        m_workPriorities.SetPriority(WorkType.Cook, 1);
        m_workPriorities.SetPriority(WorkType.Plant, 4);

        int result = m_workPriorities.CompareWorkTypes(WorkType.Cook, WorkType.Plant);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void CompareWorkTypes_SecondWorkTypeHasBetterPriority_SecondWins()
    {
        m_workPriorities.SetPriority(WorkType.Plant, 4);
        m_workPriorities.SetPriority(WorkType.Cook, 1);

        int result = m_workPriorities.CompareWorkTypes(WorkType.Plant, WorkType.Cook);

        Assert.That(result, Is.GreaterThan(0));
    }

    [Test]
    public void CompareWorkTypes_SameWorkType_ReturnsZero()
    {
        int result = m_workPriorities.CompareWorkTypes(WorkType.Craft, WorkType.Craft);

        Assert.That(result, Is.EqualTo(0));
    }
}