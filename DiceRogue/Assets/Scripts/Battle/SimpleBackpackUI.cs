using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame
{
    /// <summary>
    /// Minimal backpack overlay that lists dice names, cooldown status, and lets the player pick dice in order.
    /// Icons can be added later; currently uses text buttons.
    /// </summary>
    public class SimpleBackpackUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform listRoot;
        [SerializeField] private TMP_Text helperText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;

        private readonly List<Entry> _entries = new();
        private readonly List<BaseDice> _selected = new();
        private HashSet<BaseDice> _availableDice = new();
        private Action<List<BaseDice>> _onConfirm;
        private int _recommendedCount = 5;
        private bool _selectionEnabled = true;

        private void Awake()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirm);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        /// <summary>
        /// Open overlay in editable mode so the player can pick dice.
        /// </summary>
        public void OpenForSelection(List<BaseDice> allDice, List<BaseDice> availableDice, int recommendedCount, Action<List<BaseDice>> onConfirm)
        {
            OpenInternal(allDice, availableDice, recommendedCount, onConfirm, true);
        }

        /// <summary>
        /// Open overlay in read-only mode for inspection only.
        /// </summary>
        public void OpenViewer(List<BaseDice> allDice, List<BaseDice> availableDice)
        {
            OpenInternal(allDice, availableDice, _recommendedCount, null, false);
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OpenInternal(List<BaseDice> allDice, List<BaseDice> availableDice, int recommendedCount, Action<List<BaseDice>> callback, bool selectionEnabled)
        {
            _selectionEnabled = selectionEnabled;
            _recommendedCount = Mathf.Max(0, recommendedCount);
            _onConfirm = callback;
            _selected.Clear();
            _availableDice = new HashSet<BaseDice>(availableDice ?? new List<BaseDice>());

            RebuildList(allDice ?? new List<BaseDice>());
            UpdateHelper();

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(selectionEnabled);
                confirmButton.interactable = true;
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(!selectionEnabled);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        private void RebuildList(List<BaseDice> allDice)
        {
            if (listRoot == null)
            {
                Debug.LogError("[SimpleBackpackUI] listRoot is not assigned.");
                return;
            }

            var vertical = listRoot.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                vertical.childControlWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandWidth = true;
                vertical.childForceExpandHeight = false;
                if (Mathf.Approximately(vertical.spacing, 0f))
                {
                    vertical.spacing = 6f;
                }
            }

            for (int i = listRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(listRoot.GetChild(i).gameObject);
            }

            _entries.Clear();

            foreach (var dice in allDice)
            {
                if (dice == null) continue;

                var entryGO = CreateEntryObject(dice.diceName);
                entryGO.transform.SetParent(listRoot, false);

                var entry = new Entry
                {
                    dice = dice,
                    button = entryGO.GetComponent<Button>(),
                    background = entryGO.GetComponent<Image>(),
                    label = entryGO.GetComponentInChildren<TextMeshProUGUI>()
                };

                bool isReady = dice.cooldownRemain <= 0;
                bool isSelectable = isReady;

                entry.isOnCooldown = !isReady;

                if (entry.button != null)
                {
                    entry.button.onClick.RemoveAllListeners();
                    entry.button.interactable = _selectionEnabled && isSelectable;
                    if (_selectionEnabled && isSelectable)
                    {
                        entry.button.onClick.AddListener(() => ToggleSelection(entry));
                    }
                }

                UpdateEntryVisual(entry);
                _entries.Add(entry);
            }

            if (_entries.Count == 0 && helperText != null)
            {
                helperText.text = "No dice available.";
            }
        }

        private GameObject CreateEntryObject(string diceName)
        {
            var go = new GameObject($"Entry_{diceName}", typeof(RectTransform), typeof(Image), typeof(Button));

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0, 64f);
            rect.anchoredPosition = Vector2.zero;

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 60f;
            layout.preferredHeight = 64f;
            layout.flexibleWidth = 1000f;

            var bg = go.GetComponent<Image>();
            bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.16f, 0.16f, 0.16f, 0.9f);
            bg.raycastTarget = true;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);

            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(18, 6);
            labelRect.offsetMax = new Vector2(-18, -6);

            var label = labelGO.GetComponent<TextMeshProUGUI>();
            label.text = diceName;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 28f;
            label.enableWordWrapping = false;
            label.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            colors.highlightedColor = new Color(0.24f, 0.24f, 0.24f, 0.95f);
            colors.pressedColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
            colors.selectedColor = new Color(0.24f, 0.24f, 0.24f, 0.95f);
            button.colors = colors;

            return go;
        }

        private void ToggleSelection(Entry entry)
        {
            if (entry == null || entry.dice == null) return;

            int index = _selected.IndexOf(entry.dice);
            if (index >= 0)
            {
                _selected.RemoveAt(index);
            }
            else
            {
                if (_recommendedCount > 0 && _selected.Count >= _recommendedCount)
                {
                    UpdateHelper();
                    return;
                }
                _selected.Add(entry.dice);
            }

            RefreshEntryVisuals();
            UpdateHelper();
        }

        private void RefreshEntryVisuals()
        {
            foreach (var entry in _entries)
            {
                UpdateEntryVisual(entry);
            }
        }

        private void UpdateEntryVisual(Entry entry)
        {
            if (entry == null || entry.dice == null) return;

            int orderIndex = _selected.IndexOf(entry.dice);
            bool isSelected = orderIndex >= 0;

            if (entry.label != null)
            {
                string status = entry.dice.cooldownRemain > 0 ? $"CD {entry.dice.cooldownRemain}" : "Ready";
                string orderPrefix = isSelected ? $"{orderIndex + 1}. " : string.Empty;
                entry.label.text = $"{orderPrefix}{entry.dice.diceName}  <size=70%><color=#AAAAAA>({status})</color></size>";
                entry.label.color = entry.isOnCooldown ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
            }

            if (entry.background != null)
            {
                if (entry.isOnCooldown)
                {
                    entry.background.color = new Color(0.25f, 0.25f, 0.25f, 0.6f);
                }
                else if (isSelected)
                {
                    entry.background.color = new Color(0.2f, 0.36f, 0.2f, 0.9f);
                }
                else
                {
                    entry.background.color = new Color(0.16f, 0.16f, 0.16f, 0.9f);
                }
            }
        }

        private void UpdateHelper()
        {
            if (helperText == null) return;

            if (_selectionEnabled)
            {
                if (_recommendedCount > 0)
                {
                    helperText.text = $"Selected {_selected.Count}/{_recommendedCount}. Confirm whenever you're ready.";
                    if (_selected.Count >= _recommendedCount)
                    {
                        helperText.text += "\n<size=80%><color=#FFD070>Maximum slots filled.</color></size>";
                    }
                }
                else
                {
                    helperText.text = $"Selected {_selected.Count} dice. Confirm whenever you're ready.";
                }
                if (_availableDice.Count == 0)
                {
                    helperText.text += "\nAll dice are currently on cooldown.";
                }
            }
            else
            {
                helperText.text = "Viewing backpack (close when finished).";
            }
        }

        private void OnConfirm()
        {
            var callback = _onConfirm;
            var result = new List<BaseDice>(_selected);
            Close();
            callback?.Invoke(result);
        }

        private class Entry
        {
            public BaseDice dice;
            public Button button;
            public Image background;
            public TextMeshProUGUI label;
            public bool isOnCooldown;
        }
    }
}
