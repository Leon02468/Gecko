using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures the MissionOverlayUI has a properly configured Canvas
/// Attach this to the MissionOverlayUI GameObject
/// Assign the Canvas in the Inspector, and this script will configure it properly
/// </summary>
[ExecuteAlways]
public class EnsureMissionOverlayCanvas : MonoBehaviour
{
    [Header("Canvas Reference")]
    [Tooltip("The Canvas to configure. If not assigned, will try to find one on this GameObject.")]
    public Canvas overlayCanvas;

    void Awake()
    {
        ConfigureCanvas();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Also configure in editor when values change
        if (!Application.isPlaying)
        {
            ConfigureCanvas();
        }
    }
#endif

    void ConfigureCanvas()
    {
        Debug.Log($"[EnsureMissionOverlayCanvas] Configuring Canvas on {gameObject.name}");
        
        // If no canvas assigned, try to get one from this GameObject
        if (overlayCanvas == null)
        {
            overlayCanvas = GetComponent<Canvas>();
        }

        // Still no canvas? Try to get one from children
        if (overlayCanvas == null)
        {
            overlayCanvas = GetComponentInChildren<Canvas>();
        }

        if (overlayCanvas == null)
        {
            Debug.LogError($"[EnsureMissionOverlayCanvas] ? No Canvas assigned or found on {gameObject.name}! Please assign one in the Inspector.");
            return;
        }

        Debug.Log($"[EnsureMissionOverlayCanvas] Using Canvas: {overlayCanvas.gameObject.name}");

        // Get or add CanvasScaler
        var scaler = overlayCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = overlayCanvas.gameObject.AddComponent<CanvasScaler>();
            Debug.Log($"[EnsureMissionOverlayCanvas] Added CanvasScaler component");
        }

        // Get or add GraphicRaycaster
        var raycaster = overlayCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = overlayCanvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log($"[EnsureMissionOverlayCanvas] Added GraphicRaycaster component");
        }

        // Configure Canvas
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.pixelPerfect = false;
        overlayCanvas.sortingOrder = 100; // High number to ensure it's on top
        overlayCanvas.targetDisplay = 0;
        
        Debug.Log($"[EnsureMissionOverlayCanvas] Canvas settings - RenderMode: {overlayCanvas.renderMode}, SortingOrder: {overlayCanvas.sortingOrder}");

        // Configure Canvas Scaler
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        Debug.Log($"[EnsureMissionOverlayCanvas] CanvasScaler settings - Resolution: {scaler.referenceResolution}");

        Debug.Log($"[EnsureMissionOverlayCanvas] ? Canvas fully configured on {overlayCanvas.gameObject.name}");
    }
}
