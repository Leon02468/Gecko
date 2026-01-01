using UnityEngine;
using System.Collections;

/// <summary>
/// Helper class to run coroutines when the original GameObject might be inactive.
/// This ensures coroutines can always run regardless of GameObject state.
/// </summary>
public class CoroutineHelper : MonoBehaviour
{
    private static CoroutineHelper _instance;
    
    public static CoroutineHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                // Create a new GameObject with CoroutineHelper
                GameObject go = new GameObject("CoroutineHelper");
                _instance = go.AddComponent<CoroutineHelper>();
                DontDestroyOnLoad(go);
                Debug.Log("[CoroutineHelper] Created CoroutineHelper instance");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Start a coroutine through the helper
    /// </summary>
    public void StartHelperCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

    /// <summary>
    /// Stop a coroutine through the helper
    /// </summary>
    public void StopHelperCoroutine(IEnumerator coroutine)
    {
        StopCoroutine(coroutine);
    }
}
