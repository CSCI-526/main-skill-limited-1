using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DiceGame; // BaseDice, DiceTier 等

/// <summary>
/// Provides access to all available dice types using reflection-based auto-discovery.
/// Automatically discovers all BaseDice subclasses in the assembly.
/// </summary>
public static class DicePool
{
    private static List<BaseDice> _cachedDiceTypes = null;
    
    /// <summary>
    /// Types to exclude from auto-discovery
    /// </summary>
    private static readonly HashSet<string> _excludedTypes = new HashSet<string>
    {
        "NormalDice",  // Filler dice, auto-generated
        "BaseDice"     // Abstract base class
    };

    /// <summary>
    /// Get all dice types using reflection-based auto-discovery.
    /// Automatically discovers all BaseDice subclasses in the assembly.
    /// Results are cached for performance.
    /// </summary>
    /// <returns>List of all discovered dice type instances</returns>
    public static List<BaseDice> GetAll()
    {
        // Return cached result if available
        if (_cachedDiceTypes != null)
        {
            return new List<BaseDice>(_cachedDiceTypes);
        }

        // Discover dice types using reflection
        _cachedDiceTypes = DiscoverDiceTypes();
        return new List<BaseDice>(_cachedDiceTypes);
    }

    /// <summary>
    /// Get all non-Filler dice types.
    /// Uses GetAll() and filters out Filler tier dice.
    /// </summary>
    /// <returns>List of non-Filler dice types</returns>
    public static List<BaseDice> GetNonFiller() =>
        GetAll().Where(d => d != null && d.tier != DiceTier.Filler).ToList();

    /// <summary>
    /// Get dice types by tier.
    /// </summary>
    /// <param name="tier">The tier to filter by</param>
    /// <returns>List of dice types matching the specified tier</returns>
    public static List<BaseDice> GetByTier(DiceTier tier) =>
        GetAll().Where(d => d != null && d.tier != DiceTier.Filler && d.tier == tier).ToList();

    /// <summary>
    /// Discover all dice types using reflection.
    /// Scans the assembly for all BaseDice subclasses and creates instances.
    /// </summary>
    /// <returns>List of discovered dice type instances</returns>
    private static List<BaseDice> DiscoverDiceTypes()
    {
        var diceTypes = new List<BaseDice>();
        var assembly = typeof(BaseDice).Assembly;
        var baseDiceType = typeof(BaseDice);

        // Find all types that inherit from BaseDice
        var diceClasses = assembly.GetTypes()
            .Where(t =>
                baseDiceType.IsAssignableFrom(t) &&  // Is a subclass of BaseDice
                !t.IsAbstract &&                      // Not abstract
                t != baseDiceType &&                  // Not BaseDice itself
                !t.IsGenericType &&                   // Not a generic type
                !_excludedTypes.Contains(t.Name)       // Not in exclusion list
            )
            .OrderBy(t => t.Name)  // Sort for consistent ordering
            .ToList();

        // Create instances of each dice type
        foreach (var diceType in diceClasses)
        {
            try
            {
                var dice = System.Activator.CreateInstance(diceType) as BaseDice;
                if (dice != null)
                {
                    diceTypes.Add(dice);
                    Debug.Log($"[DicePool] Auto-discovered: {dice.diceName} ({dice.tier}) - {diceType.Name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DicePool] Failed to create instance of {diceType.Name}: {ex.Message}");
            }
        }

        Debug.Log($"[DicePool] Auto-discovery complete: {diceTypes.Count} dice types found");
        return diceTypes;
    }

    /// <summary>
    /// Clear the cached dice types.
    /// Useful for testing or when dice types need to be reloaded.
    /// </summary>
    public static void ClearCache()
    {
        _cachedDiceTypes = null;
        Debug.Log("[DicePool] Cache cleared");
    }
}