using UnityEngine;

public class AntCrawlingEnemy : MonoBehaviour
{
    public Transform player;
    public Collider2D soundZone; // Assign a trigger/collider for the sound range

    [SerializeField] private AudioClip crawlingSound;
    private AudioSource audioSource;
    private bool playerInZone = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        audioSource.clip = crawlingSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (soundZone != null && player != null)
        {
            playerInZone = soundZone.bounds.Contains(player.position);
        }

        if (playerInZone)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}
