using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Attack")]
public class BossAttack : ScriptableObject
{
    [Header("Timings")]
    public float windup = 0.4f;
    public float active = 0.25f;
    public float recovery = 0.7f;

    [Header("Movement")]
    public Vector2 dashVelocity = Vector2.zero;
    public bool lockFacingDuringAttack = true;

    [Header("Hitbox")]
    public string hitboxName;  // path under boss root, e.g. "Hitboxes/ClawFront"

    [Header("Damage")]
    public float damage = 1f;

    [Header("AI hint")]
    public float preferredRange = 2.5f;
}
