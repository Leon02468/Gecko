using UnityEngine;

// Use a material with a texture whose Wrap Mode = Repeat. This adjusts the material UV offset.
[ExecuteAlways]
public class ParallaxMaterial : MonoBehaviour
{
    [Tooltip("Camera to follow. Leave empty to use Camera.main.")]
    public Transform cameraTransform;
    [Tooltip("Parallax multiplier applied to material UV offset.")]
    public Vector2 parallaxMultiplier = new Vector2(0.2f, 0f);

    private Renderer rend;
    private Vector3 lastCameraPos;
    private Vector2 initialOffset;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            lastCameraPos = cameraTransform.position;

        if (rend != null && rend.sharedMaterial != null)
            initialOffset = rend.sharedMaterial.mainTextureOffset;
    }

    void LateUpdate()
    {
        if (rend == null || rend.sharedMaterial == null) return;
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                return;
        }

        Vector3 camDelta = cameraTransform.position - lastCameraPos;

        // Move texture offset opposite camera movement (so background appears to move slower)
        Vector2 offsetDelta = new Vector2(camDelta.x * parallaxMultiplier.x, camDelta.y * parallaxMultiplier.y);
        Vector2 newOffset = initialOffset + offsetDelta;

        // Accumulate offset so it persists across frames
        Material mat = rend.material; // gets instance so offset doesn't change shared asset at edit-time unless intended
        mat.mainTextureOffset = newOffset;

        // update lastCameraPos for next frame and accumulate initialOffset
        initialOffset = newOffset;
        lastCameraPos = cameraTransform.position;
    }
}