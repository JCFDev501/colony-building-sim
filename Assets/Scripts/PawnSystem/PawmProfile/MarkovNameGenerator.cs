using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Generates readable names from a source-name pool using a simple Markov-chain-style model.
/// This class only generates text and does not know anything about pawns or pawn profiles.
/// </summary>
public class MarkovNameGenerator
{
    private const char kEndCharacter = '$';
    private const string kStartKey = "^^";
    private const int kOrder = 2;

    private readonly Dictionary<string, List<char>> m_transitions = new Dictionary<string, List<char>>();
    private readonly HashSet<string> m_sourceNames = new HashSet<string>();

    /// <summary>
    /// Gets whether this generator currently has transition data available.
    /// </summary>
    public bool HasModel
    {
        get { return m_transitions.Count > 0; }
    }

    /// <summary>
    /// Builds the transition table from the provided source names.
    /// Empty names are ignored.
    /// </summary>
    public void Build(IReadOnlyList<string> sourceNames)
    {
        m_transitions.Clear();
        m_sourceNames.Clear();

        if (sourceNames == null)
        {
            return;
        }

        for (int i = 0; i < sourceNames.Count; i++)
        {
            string sourceName = NormalizeSourceName(sourceNames[i]);

            if (string.IsNullOrWhiteSpace(sourceName))
            {
                continue;
            }

            m_sourceNames.Add(sourceName);
            AddNameToTransitions(sourceName);
        }
    }

    /// <summary>
    /// Attempts to generate a name within the requested length range.
    /// The generator avoids exact source-name duplicates when possible.
    /// </summary>
    public string GenerateName(
        int minLength,
        int maxLength,
        int maxAttempts,
        System.Random random)
    {
        if (random == null)
        {
            random = new System.Random();
        }

        if (!HasModel)
        {
            return GetFallbackSourceName(random);
        }

        int safeMinLength = Math.Max(1, minLength);
        int safeMaxLength = Math.Max(safeMinLength, maxLength);
        int safeMaxAttempts = Math.Max(1, maxAttempts);

        string fallbackName = string.Empty;

        for (int attemptIndex = 0; attemptIndex < safeMaxAttempts; attemptIndex++)
        {
            string generatedName = GenerateSingleName(safeMaxLength, random);

            if (string.IsNullOrWhiteSpace(generatedName))
            {
                continue;
            }

            fallbackName = generatedName;

            if (generatedName.Length < safeMinLength || generatedName.Length > safeMaxLength)
            {
                continue;
            }

            if (m_sourceNames.Contains(generatedName) && m_sourceNames.Count > 1)
            {
                continue;
            }

            return generatedName;
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            return fallbackName;
        }

        return GetFallbackSourceName(random);
    }

    /// <summary>
    /// Adds one normalized source name into the transition table.
    /// </summary>
    private void AddNameToTransitions(string sourceName)
    {
        string paddedName = kStartKey + sourceName.ToLowerInvariant() + kEndCharacter;

        for (int i = 0; i <= paddedName.Length - kOrder - 1; i++)
        {
            string key = paddedName.Substring(i, kOrder);
            char nextCharacter = paddedName[i + kOrder];

            AddTransition(key, nextCharacter);
        }
    }

    /// <summary>
    /// Adds one possible transition from a two-character key to the next character.
    /// </summary>
    private void AddTransition(string key, char nextCharacter)
    {
        if (!m_transitions.TryGetValue(key, out List<char> nextCharacters))
        {
            nextCharacters = new List<char>();
            m_transitions.Add(key, nextCharacters);
        }

        nextCharacters.Add(nextCharacter);
    }

    /// <summary>
    /// Generates one candidate name from the transition table.
    /// </summary>
    private string GenerateSingleName(int maxLength, System.Random random)
    {
        if (!m_transitions.ContainsKey(kStartKey))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        string currentKey = kStartKey;

        for (int i = 0; i < maxLength; i++)
        {
            if (!m_transitions.TryGetValue(currentKey, out List<char> nextCharacters))
            {
                break;
            }

            if (nextCharacters.Count == 0)
            {
                break;
            }

            char nextCharacter = nextCharacters[random.Next(0, nextCharacters.Count)];

            if (nextCharacter == kEndCharacter)
            {
                break;
            }

            builder.Append(nextCharacter);

            string combinedKey = currentKey + nextCharacter;
            currentKey = combinedKey.Substring(combinedKey.Length - kOrder, kOrder);
        }

        return FormatGeneratedName(builder.ToString());
    }

    /// <summary>
    /// Normalizes source names before they are added to the model.
    /// </summary>
    private string NormalizeSourceName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return string.Empty;
        }

        return FormatGeneratedName(sourceName.Trim());
    }

    /// <summary>
    /// Formats a generated name with an uppercase first letter and lowercase remaining letters.
    /// </summary>
    private string FormatGeneratedName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        string trimmedName = name.Trim().ToLowerInvariant();

        if (trimmedName.Length == 1)
        {
            return trimmedName.ToUpperInvariant();
        }

        return char.ToUpperInvariant(trimmedName[0]) + trimmedName.Substring(1);
    }

    /// <summary>
    /// Returns a fallback source name if generated candidates fail.
    /// </summary>
    private string GetFallbackSourceName(System.Random random)
    {
        if (m_sourceNames.Count == 0)
        {
            return "Rook";
        }

        List<string> sourceNames = new List<string>(m_sourceNames);
        return sourceNames[random.Next(0, sourceNames.Count)];
    }
}