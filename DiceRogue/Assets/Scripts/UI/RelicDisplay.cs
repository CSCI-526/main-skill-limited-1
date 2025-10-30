using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Relics;

namespace DiceGame.UI
{
    /// <summary>
    /// Simple relic display UI - shows equipped relics as colored squares
    /// </summary>
    public class RelicDisplay : MonoBehaviour
    {
        [Header("UI References")]
        public Transform relicContainer;  // Parent transform for relic icons
        public GameObject relicIconPrefab; // Prefab with Image component (square)
        
        [Header("Colors by Rarity")]
        public Color commonColor = new Color(0.56f, 0.93f, 0.56f);    // Light green
        public Color rareColor = new Color(0.58f, 0.44f, 0.86f);      // Purple
        public Color legendaryColor = new Color(1f, 0.84f, 0f);       // Gold
        
        private readonly List<GameObject> _relicIcons = new();
        
        /// <summary>
        /// Display relics from RelicManager
        /// </summary>
        public void DisplayRelics(RelicManager relicManager)
        {
            if (relicManager == null || relicContainer == null)
            {
                Debug.LogWarning("[RelicDisplay] RelicManager or container is null!");
                return;
            }
            
            // Clear existing icons
            ClearRelics();
            
            // Create icon for each equipped relic
            var relics = relicManager.Equipped;
            Debug.Log($"[RelicDisplay] Displaying {relics.Count} relic(s)");
            
            for (int i = 0; i < relics.Count; i++)
            {
                var relic = relics[i];
                if (relic == null) continue;
                
                CreateRelicIcon(relic, i);
            }
        }
        
        /// <summary>
        /// Create a single relic icon
        /// </summary>
        private void CreateRelicIcon(RelicBase relic, int index)
        {
            GameObject icon;
            
            if (relicIconPrefab != null)
            {
                // Use prefab if provided
                icon = Instantiate(relicIconPrefab, relicContainer);
            }
            else
            {
                // Create simple square UI if no prefab
                icon = CreateSquareIcon();
            }
            
            // Check if icon creation failed
            if (icon == null)
            {
                Debug.LogError($"[RelicDisplay] Failed to create icon for relic: {relic.relicName}");
                return;
            }
            
            icon.name = $"RelicIcon_{relic.relicName}";
            
            // Set color based on rarity
            var image = icon.GetComponent<Image>();
            if (image != null)
            {
                image.color = GetRarityColor(relic.rarity);
            }
            
            // Add RelicIcon component for hover tooltip
            var relicIcon = icon.GetComponent<RelicIcon>();
            if (relicIcon == null)
            {
                relicIcon = icon.AddComponent<RelicIcon>();
            }
            
            if (relicIcon != null)
            {
                relicIcon.relic = relic;
            }
            
            _relicIcons.Add(icon);
            
            Debug.Log($"[RelicDisplay] Created icon for: {relic.relicName} ({relic.rarity})");
        }
        
        /// <summary>
        /// Create a simple square UI element programmatically
        /// </summary>
        private GameObject CreateSquareIcon()
        {
            var icon = new GameObject("RelicIcon");
            icon.transform.SetParent(relicContainer, false);
            
            // Add RectTransform (automatically added with AddComponent<Image>)
            var image = icon.AddComponent<Image>();
            if (image == null)
            {
                Debug.LogError("[RelicDisplay] Failed to add Image component");
                Destroy(icon);
                return null;
            }
            
            // Try to load the Square.png sprite from Resources
            var squareSprite = Resources.Load<Sprite>("Square");
            if (squareSprite != null)
            {
                image.sprite = squareSprite;
                Debug.Log("[RelicDisplay] Loaded Square sprite from Resources");
            }
            else
            {
                // Fallback: use Unity's default white sprite (built-in)
                image.sprite = null;
                image.color = Color.white; // Will show as white square
                Debug.LogWarning("[RelicDisplay] Square sprite not found in Resources. Using default white square. Make sure 'Square.png' is in 'Assets/Resources/' folder.");
            }
            
            // Set size
            var rectTransform = icon.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(60, 60);
            }
            
            return icon;
        }
        
        
        /// <summary>
        /// Get color for rarity
        /// </summary>
        private Color GetRarityColor(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => commonColor,
                RelicRarity.Rare => rareColor,
                RelicRarity.Legendary => legendaryColor,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Clear all relic icons
        /// </summary>
        public void ClearRelics()
        {
            foreach (var icon in _relicIcons)
            {
                if (icon != null)
                {
                    Destroy(icon);
                }
            }
            _relicIcons.Clear();
        }
        
        /// <summary>
        /// Trigger pop effect on a specific relic by name
        /// </summary>
        public void PopRelicByName(string relicName, float intensity = 1.0f)
        {
            foreach (var icon in _relicIcons)
            {
                if (icon != null && icon.name.Contains(relicName))
                {
                    StartCoroutine(PopEffectCoroutine(icon.transform, intensity));
                    return;
                }
            }
        }
        
        /// <summary>
        /// Trigger pop effect on a specific relic by reference
        /// </summary>
        public void PopRelicByReference(RelicBase relic, float intensity = 1.0f)
        {
            if (relic == null) return;
            PopRelicByName(relic.relicName, intensity);
        }
        
        /// <summary>
        /// Pop effect animation coroutine
        /// </summary>
        private System.Collections.IEnumerator PopEffectCoroutine(Transform target, float intensity)
        {
            Vector3 originalScale = target.localScale;
            
            // Scale based on intensity (1.0 = normal, higher = bigger pop)
            float targetScale = 1.0f + (0.3f * intensity);
            Vector3 popScale = originalScale * targetScale;
            
            float duration = 0.15f;
            float elapsed = 0f;
            
            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(originalScale, popScale, t);
                yield return null;
            }
            
            elapsed = 0f;
            // Scale down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(popScale, originalScale, t);
                yield return null;
            }
            
            target.localScale = originalScale;
        }
        
        void OnDestroy()
        {
            ClearRelics();
        }
    }
}

