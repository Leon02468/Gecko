using UnityEngine;
using Pathfinding;
using System.Collections;

public class FlyingEnemy : MonoBehaviour
{
    public Transform homePosition;
    public Collider2D chaseZone;  // ChaseZone collider here
    public float updateRate = 0.5f;

    private Transform player;
    private AIDestinationSetter destinationSetter;
    private AIPath aiPath;
    private bool playerInZone = false;


    [SerializeField] private AudioClip flyingSound;
    private AudioSource audioSource;
    [SerializeField] private float soundRange = 10f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = flyingSound;
        audioSource.loop = true;
    }

    private void Start()
    {
        if (GameManager.Instance.PlayerInstance != null)
            player = GameManager.Instance.PlayerInstance.transform;

        destinationSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();
        destinationSetter.target = homePosition;

        StartCoroutine(UpdateTarget());
    }

    private void Update()
    {
        if (chaseZone != null && player != null)
        {
            playerInZone = chaseZone.bounds.Contains(player.position);
        }

        // Set volume from AudioManager before playing/stopping
        audioSource.volume = AudioManager.GlobalSFXVolume;

        // Play or stop flying sound based on playerInZone
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

        if (aiPath.desiredVelocity.x >= 0.01f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        else if (aiPath.desiredVelocity.x <= -0.01f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    IEnumerator UpdateTarget()
    {
        while (true)
        {
            if (playerInZone)
                destinationSetter.target = player;
            else
                destinationSetter.target = homePosition;

            yield return new WaitForSeconds(updateRate);
        }
    }

}
