using UnityEngine;
using System;
using System.Reflection;

[RequireComponent(typeof(Collider2D))]
public class DamageOnContact : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Only objects on these layers will be considered for damage. Leave empty (none) to allow all layers.")]
    public LayerMask targetLayers = 0;

    [Tooltip("Damage applied to any IDamageable touched")]
    public int damage = 1;

    [Tooltip("Knockback force magnitude applied horizontally")]
    public float knockbackForce = 6f;

    [Tooltip("Upward portion multiplier for knockback")]
    public float knockbackUpMultiplier = 0.4f;

    [Tooltip("If true, will only apply damage once per collider until it leaves and re-enters")]
    public bool oneHitPerStay = true;

    [Header("Debug / Fallback")]
    [Tooltip("Enable verbose debug logs for contact events")]
    public bool debugLogs = false;
    [Tooltip("If no TakeDamage method is found, attempt SendMessage fallback (TakeDamage / ApplyKnockback)")]
    public bool allowSendMessageFallback = true;

    // Track colliders currently in contact to avoid repeat hits while staying in contact
    private readonly System.Collections.Generic.HashSet<Collider2D> currentHits = new System.Collections.Generic.HashSet<Collider2D>();

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        if (debugLogs) Debug.Log($"[DamageOnContact] OnCollisionEnter2D: {name} collided with {collision.collider.name} (layer:{LayerMask.LayerToName(collision.collider.gameObject.layer)})");
        HandleContact(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (debugLogs) Debug.Log($"[DamageOnContact] OnTriggerEnter2D: {name} triggered by {other.name} (layer:{LayerMask.LayerToName(other.gameObject.layer)})");
        HandleContact(other);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // useful for debugging or for continuous damage when oneHitPerStay == false
        if (collision == null || collision.collider == null) return;
        if (!oneHitPerStay)
        {
            HandleContact(collision.collider);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        currentHits.Remove(collision.collider);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        currentHits.Remove(other);
    }

    private void HandleContact(Collider2D col)
    {
        if (col == null) return;

        if (debugLogs) Debug.Log($"[DamageOnContact] HandleContact called for {col.name} (layer:{col.gameObject.layer})");

        // Filter by layer mask if configured
        if (targetLayers != 0 && (targetLayers.value & (1 << col.gameObject.layer)) == 0)
        {
            if (debugLogs) Debug.Log($"[DamageOnContact] Layer {LayerMask.LayerToName(col.gameObject.layer)} not in targetLayers mask ({targetLayers}). Skipping.");
            return;
        }

        // Do not damage ourselves or children
        if (col.gameObject == gameObject) return;
        if (col.transform.IsChildOf(transform)) return;

        if (oneHitPerStay && currentHits.Contains(col))
            return;

        // Compute horizontal knockback away from this mob
        float dir = Mathf.Sign(col.transform.position.x - transform.position.x);
        if (Mathf.Approximately(dir, 0f)) dir = 1f;
        Vector2 kb = new Vector2(dir * knockbackForce, knockbackForce * knockbackUpMultiplier);

        // First try explicit PlayerHealth fallback for reliability
        // Use GetComponentInParent by type name via reflection to avoid compile-time dependency
        var parentComponents = col.GetComponentsInParent<Component>(true);
        foreach (var c in parentComponents)
        {
            if (c == null) continue;
            if (c.GetType().Name == "PlayerHealth")
            {
                try
                {
                    var m = c.GetType().GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m != null)
                    {
                        if (debugLogs) Debug.Log($"[DamageOnContact] Invoking PlayerHealth.TakeDamage on {c.gameObject.name}");
                        // try common overloads
                        var ps = m.GetParameters();
                        if (ps.Length == 1)
                            m.Invoke(c, new object[] { damage });
                        else if (ps.Length == 2)
                            m.Invoke(c, new object[] { damage, (object)kb });
                        else if (ps.Length >= 3)
                            m.Invoke(c, new object[] { damage, (object)kb, null });

                        if (oneHitPerStay) currentHits.Add(col);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (debugLogs) Debug.LogWarning($"[DamageOnContact] Exception calling PlayerHealth.TakeDamage via reflection: {ex}");
                }
            }
        }

        // Look for any Component on the collided object or its parents that exposes TakeDamage methods
        var components = parentComponents;
        MethodInfo chosenMethod = null;
        Component chosenComponent = null;

        foreach (var c in components)
        {
            if (c == null) continue;
            var methods = c.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            // find all matching methods named TakeDamage
            foreach (var m in methods)
            {
                if (m == null) continue;
                if (!string.Equals(m.Name, "TakeDamage", StringComparison.Ordinal)) continue;

                // prefer methods whose first parameter is numeric (int/float/double) or whose param count matches common signatures
                var ps = m.GetParameters();
                if (ps.Length == 0) continue;
                Type p0 = ps[0].ParameterType;
                bool p0IsNumeric = IsNumericParameter(p0);

                // Choose best candidate: if we have no chosen yet, pick this; if current chosen is non-numeric but new is numeric, prefer numeric
                if (chosenMethod == null)
                {
                    chosenMethod = m;
                    chosenComponent = c;
                }
                else
                {
                    var chosenP0 = chosenMethod.GetParameters().Length > 0 ? chosenMethod.GetParameters()[0].ParameterType : null;
                    bool chosenP0IsNumeric = chosenP0 != null && IsNumericParameter(chosenP0);
                    if (!chosenP0IsNumeric && p0IsNumeric)
                    {
                        chosenMethod = m;
                        chosenComponent = c;
                    }
                    else if (p0IsNumeric == chosenP0IsNumeric)
                    {
                        // prefer method with more parameters (more specific)
                        if (m.GetParameters().Length > chosenMethod.GetParameters().Length)
                        {
                            chosenMethod = m;
                            chosenComponent = c;
                        }
                    }
                }
            }

            if (chosenMethod != null) break;
        }

        if (chosenMethod != null && chosenComponent != null)
        {
            if (debugLogs) Debug.Log($"[DamageOnContact] Selected TakeDamage on {chosenComponent.GetType().Name} (params: {chosenMethod.GetParameters().Length})");

            // Prepare parameters based on method signature
            var parameters = chosenMethod.GetParameters();
            object[] args = null;
            try
            {
                if (parameters.Length == 1)
                {
                    args = new object[] { damage };
                }
                else if (parameters.Length == 2)
                {
                    args = new object[] { damage, (object)kb };
                }
                else if (parameters.Length >= 3)
                {
                    args = new object[] { damage, (object)kb, null };
                }

                chosenMethod.Invoke(chosenComponent, args);
                if (debugLogs) Debug.Log($"[DamageOnContact] Invoked TakeDamage on {chosenComponent.name} with args: { (args != null ? string.Join(",", System.Array.ConvertAll(args, a => a?.ToString() ?? "null")) : "none") }");
            }
            catch (Exception ex)
            {
                if (debugLogs) Debug.LogWarning($"[DamageOnContact] Exception invoking chosen TakeDamage: {ex}");
            }

            if (oneHitPerStay) currentHits.Add(col);
            return;
        }

        if (debugLogs) Debug.Log($"[DamageOnContact] No TakeDamage method found on parents of {col.name}.");

        if (allowSendMessageFallback)
        {
            if (debugLogs) Debug.Log($"[DamageOnContact] Trying SendMessage fallback on {col.name}");
            // attempt to send legacy messages
            col.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            col.SendMessageUpwards("ApplyKnockback", kb, SendMessageOptions.DontRequireReceiver);

            if (oneHitPerStay) currentHits.Add(col);
        }
    }

    private bool IsNumericParameter(Type t)
    {
        if (t == null) return false;
        var ut = Nullable.GetUnderlyingType(t) ?? t;
        return ut == typeof(int) || ut == typeof(float) || ut == typeof(double) || ut == typeof(long) || ut == typeof(short);
    }
}
