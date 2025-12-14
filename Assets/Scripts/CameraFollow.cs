using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Target to follow (player).")]
    public Transform target;

    [Tooltip("Camera world offset from target.")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("Smooth time. Set to 0 for no smoothing.")]
    public float smoothTime = 0.04f;

    public Transform BG;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (BG != null)
        {
            BG.position = new Vector3(transform.position.x, transform.position.y, BG.position.z);
        }

        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        if (smoothTime > 0f)
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        else
            transform.position = targetPos;
    }
}