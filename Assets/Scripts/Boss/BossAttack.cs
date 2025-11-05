using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Attack")]
public class BossAttack : ScriptableObject
{
    [Header("Timings (seconds)")]
    public float windup = 0.4f;
    public float active = 0.25f;
    public float recovery = 0.6f;

    [Header("Movement")]
    public Vector2 dashVelocity;
    public bool lockFacingDuringAttack = true;

    [Header("Hitbox")]
    public string hitboxName;
    public float damage = 1;
    public float knockback = 8f;

    [Header("FX")]
    public AudioClip sfxWindup;
    public AudioClip sfxSwing;
    public Color flashColor = Color.white;
    public float flashIntensity = 0.6f;

    [Header("AI hint")]
    public float preferredRange = 2.5f;
}
