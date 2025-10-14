using UnityEngine;

[ExecuteAlways]
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("Camera to follow. Leave empty to use Camera.main.")]
    public Transform cameraTransform;

    [Tooltip("Parallax multiplier: smaller = farther (moves less).")]
    public Vector2 parallaxMultiplier = new Vector2(0.5f, 0f);

    [Tooltip("When enabled the script will attempt to reposition the sprite horizontally to appear infinite. Requires a repeating/tiled sprite or multiple copies.")]
    public bool infiniteHorizontal = false;

    [Tooltip("When enabled the layer updates right before the camera renders (reduces jitter with Cinemachine).")]
    public bool updateOnPreRender = true;

    [Tooltip("If true, apply infinite positioning in the Scene view (edit mode). Default: false (prevents editor clustering).")]
    public bool applyInEditMode = false;

    [Tooltip("If true and no SpriteRenderer is found on this GameObject, the first child SpriteRenderer will be used to detect tile width.")]
    public bool useChildForWidth = true;

    [Tooltip("If > 0 this value overrides automatic tile width detection (world units). Useful for grouped parents.")]
    public float tileWidthOverride = 0f;

    [Tooltip("Manual tile index (0 = original, 1 = one tile to the right, -1 = left). Use only when attaching the script to each tile separately.")]
    public int tileIndex = 0;

    private Vector3 initialLayerPos;
    private Vector3 initialCameraPos;
    private float spriteWidthWorld = 0f;
    private SpriteRenderer spriteRenderer;
    private bool initialized = false;
    private bool tileIndexApplied = false;
    private bool subscribedToPreRender = false;

    void OnEnable()
    {
        // In editor, skip runtime initialization unless user explicitly wants edit-mode behavior.
        if (!Application.isPlaying && !applyInEditMode)
            return;

        Initialize();

        if (updateOnPreRender && !subscribedToPreRender)
        {
            Camera.onPreRender += OnCameraPreRender;
            subscribedToPreRender = true;
        }
    }

    void OnDisable()
    {
        if (subscribedToPreRender)
        {
            Camera.onPreRender -= OnCameraPreRender;
            subscribedToPreRender = false;
        }
    }

    void Start()
    {
        // Only initialize in Start during Play mode (safe for runtime)
        if (Application.isPlaying)
            Initialize();
    }

    void Initialize()
    {
        // Avoid reinitializing repeatedly
        if (initialized) return;

        // Find sprite renderer (self or child)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null && useChildForWidth)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Capture initial positions only at runtime or when edit preview enabled
        initialLayerPos = transform.position;
        initialCameraPos = cameraTransform != null ? cameraTransform.position : Vector3.zero;

        // Determine tile width (override if provided)
        if (tileWidthOverride > 0f)
            spriteWidthWorld = tileWidthOverride;
        else if (spriteRenderer != null)
            spriteWidthWorld = spriteRenderer.bounds.size.x;
        else
            spriteWidthWorld = 0f;

        initialized = true;
        tileIndexApplied = false;

        // If user provided a tileIndex in the editor and we are allowed to apply it (either playing or preview),
        // adjust the initialLayerPos so the placed transform acts as the visible tile position.
        if (tileIndex != 0 && spriteWidthWorld > 0f)
        {
            initialLayerPos = transform.position - new Vector3(tileIndex * spriteWidthWorld, 0f, 0f);
            tileIndexApplied = true;
        }
    }

    void LateUpdate()
    {
        // Do nothing in editor unless preview requested
        if (!Application.isPlaying && !applyInEditMode) return;

        // Only update here when not using render-time updates
        if (updateOnPreRender) return;

        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                return;
        }

        if (!initialized) Initialize();

        UpdateLayer(cameraTransform);
    }

    void OnCameraPreRender(Camera cam)
    {
        if (!updateOnPreRender) return;
        if (Camera.main == null || cam != Camera.main) return;

        // In editor, skip unless preview enabled
        if (!Application.isPlaying && !applyInEditMode) return;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        if (!initialized) Initialize();

        UpdateLayer(cameraTransform);
    }

    private void UpdateLayer(Transform cam)
    {
        if (cam == null) return;

        Vector3 camDelta = cam.position - initialCameraPos;
        Vector3 desired = initialLayerPos + new Vector3(camDelta.x * parallaxMultiplier.x, camDelta.y * parallaxMultiplier.y, 0f);

        // Apply explicit tile index offset (useful when script is on multiple tiles)
        if (spriteWidthWorld > 0f && tileIndex != 0)
            desired.x += tileIndex * spriteWidthWorld;

        if (infiniteHorizontal && spriteWidthWorld > 0f)
        {
            float camX = cam.position.x;
            float diff = camX - desired.x;
            float shift = Mathf.Round(diff / spriteWidthWorld) * spriteWidthWorld;
            desired.x += shift;
        }

        transform.position = new Vector3(desired.x, desired.y, initialLayerPos.z);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Only compute sprite width for scene editing; don't change initial positions here
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null && useChildForWidth)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteWidthWorld = spriteRenderer.bounds.size.x;

        // Reset runtime flags (safe); don't overwrite placed positions in editor
        initialized = false;
        tileIndexApplied = false;
    }
#endif

    void OnDrawGizmosSelected()
    {
        var sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(sr.bounds.size.x, sr.bounds.size.y, 0.01f));
        }
    }
}