using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Debug tool to see what UI element is under the mouse cursor.
/// Only logs when the hit target changes (not every frame).
/// Automatically disabled in production builds.
/// </summary>
public class UIRaycastDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Enable this to see UI raycast debugging. Disabled in production builds.")]
    public bool enableDebug = true;
    
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    private string lastHitObjectName = "";
    private bool wasHitting = false;

    void Update()
    {
        // Disable in production builds to avoid performance issues
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return;
        #endif

        if (!enableDebug) return;
        if (raycaster == null || eventSystem == null) return;

        var data = new PointerEventData(eventSystem);
        data.position = Input.mousePosition;

        var results = new List<RaycastResult>();
        // 使用 EventSystem 對所有 Raycaster 進行 Raycast，而不是只針對單一 Canvas
        EventSystem.current.RaycastAll(data, results);

        bool isHitting = results.Count > 0;
        string currentHitName = isHitting ? results[0].gameObject.name : "";

        // Only log when the hit target changes (not every frame)
        if (isHitting != wasHitting || currentHitName != lastHitObjectName)
        {
            if (isHitting)
            {
                // 只看最上面那個 UI 物件
                Debug.Log("[UIRaycast] top = " + currentHitName);
            }
            else
            {
                Debug.Log("[UIRaycast] no hit at " + Input.mousePosition);
            }
            
            lastHitObjectName = currentHitName;
            wasHitting = isHitting;
        }
    }
}