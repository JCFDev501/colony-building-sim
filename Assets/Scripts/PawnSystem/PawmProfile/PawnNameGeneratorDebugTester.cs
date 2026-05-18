using UnityEngine;

/// <summary>
/// Debug helper for testing pawn name generation in the Unity Console.
/// </summary>
public class PawnNameGeneratorDebugTester : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private int m_seed = 12345;
    [SerializeField] private int m_nameCount = 10;
    [SerializeField] private PawnGender m_gender = PawnGender.Nonbinary;

    /// <summary>
    /// Generates sample pawn names from the Inspector context menu.
    /// </summary>
    [ContextMenu("Generate Sample Names")]
    private void GenerateSampleNames()
    {
        PawnNameGenerator nameGenerator = new PawnNameGenerator();
        System.Random random = new System.Random(m_seed);

        for (int i = 0; i < m_nameCount; i++)
        {
            string displayName = nameGenerator.GenerateDisplayName(m_gender, random);
            Debug.Log("Generated Pawn Name: " + displayName, this);
        }
    }
}