using System.Collections.Generic;

/// <summary>
/// Generates pawn first names, last names, and display names using frontier/survivor-style source pools.
/// This class only handles name generation and does not create full pawn profiles.
/// </summary>
public class PawnNameGenerator
{
    private const int kDefaultMinFirstNameLength = 3;
    private const int kDefaultMaxFirstNameLength = 10;
    private const int kDefaultMinLastNameLength = 4;
    private const int kDefaultMaxLastNameLength = 12;
    private const int kDefaultMaxAttempts = 20;

    private readonly MarkovNameGenerator m_maleFirstNameGenerator = new MarkovNameGenerator();
    private readonly MarkovNameGenerator m_femaleFirstNameGenerator = new MarkovNameGenerator();
    private readonly MarkovNameGenerator m_neutralFirstNameGenerator = new MarkovNameGenerator();
    private readonly MarkovNameGenerator m_lastNameGenerator = new MarkovNameGenerator();

    /// <summary>
    /// Generates a first name for the provided gender.
    /// Nonbinary pawns use the neutral first-name pool.
    /// </summary>
    public string GenerateFirstName(PawnGender gender, System.Random random)
    {
        EnsureBuilt();

        switch (gender)
        {
            case PawnGender.Male:
                return m_maleFirstNameGenerator.GenerateName(
                    kDefaultMinFirstNameLength,
                    kDefaultMaxFirstNameLength,
                    kDefaultMaxAttempts,
                    random);

            case PawnGender.Female:
                return m_femaleFirstNameGenerator.GenerateName(
                    kDefaultMinFirstNameLength,
                    kDefaultMaxFirstNameLength,
                    kDefaultMaxAttempts,
                    random);

            default:
                return m_neutralFirstNameGenerator.GenerateName(
                    kDefaultMinFirstNameLength,
                    kDefaultMaxFirstNameLength,
                    kDefaultMaxAttempts,
                    random);
        }
    }

    /// <summary>
    /// Generates a last name from the shared last-name pool.
    /// </summary>
    public string GenerateLastName(System.Random random)
    {
        EnsureBuilt();

        return m_lastNameGenerator.GenerateName(
            kDefaultMinLastNameLength,
            kDefaultMaxLastNameLength,
            kDefaultMaxAttempts,
            random);
    }

    /// <summary>
    /// Generates a full display name from a generated first and last name.
    /// </summary>
    public string GenerateDisplayName(PawnGender gender, System.Random random)
    {
        string firstName = GenerateFirstName(gender, random);
        string lastName = GenerateLastName(random);

        return firstName + " " + lastName;
    }

    /// <summary>
    /// Builds all internal Markov generators if any model is missing.
    /// This protects name generation from Unity reloads clearing runtime-only data.
    /// </summary>
    private void EnsureBuilt()
    {
        if (m_maleFirstNameGenerator.HasModel &&
            m_femaleFirstNameGenerator.HasModel &&
            m_neutralFirstNameGenerator.HasModel &&
            m_lastNameGenerator.HasModel)
        {
            return;
        }

        m_maleFirstNameGenerator.Build(GetMaleFirstNamePool());
        m_femaleFirstNameGenerator.Build(GetFemaleFirstNamePool());
        m_neutralFirstNameGenerator.Build(GetNeutralFirstNamePool());
        m_lastNameGenerator.Build(GetLastNamePool());
    }

    /// <summary>
    /// Returns rugged first-name sources commonly suitable for male pawn profiles.
    /// </summary>
    private IReadOnlyList<string> GetMaleFirstNamePool()
    {
        return new List<string>
        {
            "Cole",
            "Wade",
            "Silas",
            "Ronan",
            "Jasper",
            "Elias",
            "Caleb",
            "Gideon",
            "Wyatt",
            "Emmett",
            "Rowan",
            "Sawyer",
            "Griffin",
            "Bennett",
            "Mason",
            "Arlen",
            "Holden",
            "Reed",
            "Tobin",
            "Garrett",
        };
    }

    /// <summary>
    /// Returns rugged first-name sources commonly suitable for female pawn profiles.
    /// </summary>
    private IReadOnlyList<string> GetFemaleFirstNamePool()
    {
        return new List<string>
        {
            "Mara",
            "Elena",
            "Tessa",
            "Nora",
            "Clara",
            "June",
            "Willa",
            "Maeve",
            "Rhea",
            "Sadie",
            "Iris",
            "Lena",
            "Cora",
            "Vera",
            "Elsie",
            "Mira",
            "Anya",
            "Rosalind",
            "Cassia",
            "Marin",
        };
    }

    /// <summary>
    /// Returns neutral first-name sources suitable for nonbinary or blended pawn profiles.
    /// </summary>
    private IReadOnlyList<string> GetNeutralFirstNamePool()
    {
        return new List<string>
        {
            "Ash",
            "Rook",
            "Quinn",
            "Sage",
            "Rowan",
            "Reese",
            "Finch",
            "Vale",
            "Pax",
            "River",
            "Hale",
            "Wren",
            "Blair",
            "Avery",
            "Morgan",
            "Shiloh",
            "Ember",
            "Scout",
            "Hollis",
            "Lane",
        };
    }

    /// <summary>
    /// Returns shared surname sources with a grounded frontier/survivor tone.
    /// </summary>
    private IReadOnlyList<string> GetLastNamePool()
    {
        return new List<string>
        {
            "Blackwood",
            "Stone",
            "Ridge",
            "Holloway",
            "Ironwood",
            "Graves",
            "Marsh",
            "Vale",
            "Hawthorne",
            "Cross",
            "Wilder",
            "Locke",
            "Frost",
            "Hale",
            "Crowe",
            "Briar",
            "Ashford",
            "Fielding",
            "Merrick",
            "Sawyer",
            "Thorne",
            "Rookwood",
            "Caldwell",
            "West",
            "Redford",
        };
    }
}