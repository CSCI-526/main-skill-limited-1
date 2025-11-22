# 🪄 Relic Structure and Shop Access

**Namespace:** `DiceGame.Relics`

---

## 📦 Relic Structure

### `RelicBase` (Abstract ScriptableObject)

Base class for all relics.

```csharp
public abstract class RelicBase : ScriptableObject
{
    public string relicName;              // Display name
    public string description;             // Description shown to player
    public RelicRarity rarity;            // Common, Rare, Legendary
    public bool unique = true;            // Prevents duplicate unique relics
    
    public abstract void Apply(ScoringContext context);
}
```

### `RelicRarity` (Enum)

```csharp
public enum RelicRarity { Common = 0, Rare = 1, Legendary = 2 }
```

### `ScoringContext` (Class)

Context passed to relics during score calculation. Relics modify:

```csharp
public class ScoringContext
{
    // Inputs
    public List<int> submittedValues;
    public int handBudget;
    public int rollsUsed;
    
    // Modifiable (relics change these)
    public int additionalBase = 0;
    public float multiplier = 1f;
    
    // Helper methods
    public int Sum;
    public int CountValue(int value);
    public bool HasAnyPair();
}
```

---

## 🏪 Relic Access in Shop Scene

### Access Pattern

```csharp
// Get singleton
PlayerResourceManager resourceManager = PlayerResourceManager.Instance;

// Access RelicManager
RelicManager relicManager = resourceManager.RelicManager;
```

### Key Methods

```csharp
// Get all available relics (global pool)
IReadOnlyList<RelicBase> available = relicManager.GlobalRelicPool;

// Get player's relics
IReadOnlyList<RelicBase> owned = relicManager.PlayerBackpack;

// Purchase relic (auto-saves)
bool success = resourceManager.AddRelicToBackpackByName("Loaded Coin");

// Filter by rarity
var rareRelics = relicManager.GlobalRelicPool
    .Where(r => r.rarity == RelicRarity.Rare)
    .ToList();

// Filter out owned unique relics
var ownedNames = new HashSet<string>(owned.Select(r => r.relicName));
var availableForPurchase = relicManager.GlobalRelicPool
    .Where(r => !r.unique || !ownedNames.Contains(r.relicName))
    .ToList();
```

### Complete Shop Example

```csharp
public class ShopRelicManager : MonoBehaviour
{
    private PlayerResourceManager _resourceManager;
    private RelicManager _relicManager;
    
    void Start()
    {
        _resourceManager = PlayerResourceManager.Instance;
        _relicManager = _resourceManager.RelicManager;
        
        // Build shop
        var ownedNames = new HashSet<string>(
            _relicManager.PlayerBackpack.Select(r => r.relicName)
        );
        var available = _relicManager.GlobalRelicPool
            .Where(r => !r.unique || !ownedNames.Contains(r.relicName))
            .ToList();
    }
    
    public void OnPurchaseRelic(string relicName)
    {
        bool success = _resourceManager.AddRelicToBackpackByName(relicName);
        if (success) {
            // Refresh shop UI
        }
    }
}
```

---

## 🔄 Persistence

- Relics are saved as names in `SaveData.relicNames`
- Automatically persisted when adding via `PlayerResourceManager`
- Restored on scene load via `AddRelicToBackpackByName()`

---

## 📚 Quick Reference

| Task | Code |
|------|------|
| Get singleton | `PlayerResourceManager.Instance` |
| Get RelicManager | `_resourceManager.RelicManager` |
| All available | `_relicManager.GlobalRelicPool` |
| Player owned | `_relicManager.PlayerBackpack` |
| Purchase by name | `_resourceManager.AddRelicToBackpackByName(name)` |
| Filter by rarity | `.Where(r => r.rarity == RelicRarity.Rare)` |
| Check owned | `.Any(r => r.relicName == name)` |

---

## ⚠️ Notes

1. Relics created programmatically in `RelicManager.InitializeGlobalRelicPool()`
2. Unique relics prevent duplicates
3. `PlayerResourceManager` is singleton (`DontDestroyOnLoad`)
4. Relics saved by name, restored from global pool on load

---

**Related:** `🎲RELIC_DESCRIPTIONS.md` for full relic descriptions
