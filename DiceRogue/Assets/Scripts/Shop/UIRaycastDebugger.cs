using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastDebugger : MonoBehaviour
{
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    void Update()
    {
        if (raycaster == null || eventSystem == null) return;

        var data = new PointerEventData(eventSystem);
        data.position = Input.mousePosition;

        var results = new List<RaycastResult>();
        // 使用 EventSystem 對所有 Raycaster 進行 Raycast，而不是只針對單一 Canvas
        EventSystem.current.RaycastAll(data, results);

        if (results.Count > 0)
        {
            // 只看最上面那個 UI 物件
            Debug.Log("[UIRaycast] top = " + results[0].gameObject.name);
        }
        else
        {
            Debug.Log("[UIRaycast] no hit at " + Input.mousePosition);
        }
    }
}