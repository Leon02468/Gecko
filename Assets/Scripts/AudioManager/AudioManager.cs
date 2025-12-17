using UnityEngine;


public class AudioManager : MonoBehaviour
{
    [Header("-------Audio Source--------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    [Header("-------Audio Clip--------")]
    [SerializeField] private AudioClip background;
    [SerializeField] private AudioClip jumpPad;
    [Header("Inventory")]
    [SerializeField] private AudioClip openInventory;
    [SerializeField] private AudioClip closeInventory;
    [Header("Player")]
    [SerializeField] private AudioClip playerRunning;
    [SerializeField] private AudioClip playerAttacking;
    [SerializeField] private AudioClip playerUseItemToHeal;
    [SerializeField] private AudioClip playerGetHit;
    [Header("Enemy")]
    [SerializeField] private AudioClip enemyGetHit;
    [Header("ShopAreaMusic")]
    [SerializeField] private AudioClip shopMusic;
    private AudioClip previousMusic;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayMusic();
    }

    //THIS ONE IS FOR BACKGROUND MUSIC
    public void PlayMusic()
    {
        if (musicSource != null && background != null)
        {
            musicSource.clip = background;
            musicSource.Play();
            Debug.Log($"AudioManager: Playing background music - {background.name}");
        }
        else
        {
            Debug.LogWarning($"AudioManager: Cannot play music. musicSource={musicSource}, background={background}");
        }
    }

    // Start running sfx
    public void StartPlayerRunning()
    {
        if (sfxSource != null && playerRunning != null)
        {
            if (!sfxSource.isPlaying || sfxSource.clip != playerRunning)
            {
                sfxSource.clip = playerRunning;
                sfxSource.loop = true;
                sfxSource.Play();
            }
        }
    }
    //when stop runnning -> stop sfx
    public void StopPlayerRunning()
    {
        if (sfxSource != null && sfxSource.clip == playerRunning)
        {
            sfxSource.Stop();
            sfxSource.clip = null;
            sfxSource.loop = false;
        }
    }


    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
            Debug.Log($"AudioManager: Playing SFX - {clip.name}");
        }
        else
        {
            Debug.LogWarning($"AudioManager: Cannot play SFX. sfxSource={sfxSource}, clip={clip}");
        }
    }

    // THIS PART IS FOR SHOP AREA
    public void PlayShopMusic()
    {
        if (musicSource != null && shopMusic != null)
        {
            previousMusic = musicSource.clip;
            musicSource.clip = shopMusic;
            musicSource.Play();
        }
    }

    public void RestorePreviousMusic()
    {
        if (musicSource != null && previousMusic != null)
        {
            musicSource.clip = previousMusic;
            musicSource.Play();
        }
    }


    // Convenience methods for specific SFX
    public void PlayJumpPad() => PlaySFX(jumpPad);
    public void PlayOpenInventory() => PlaySFX(openInventory);
    public void PlayCloseInventory() => PlaySFX(closeInventory);
    //public void PlayFlyingBee() => PlaySFX(flyingBee);
    //public void PlayCrawlingCaterpillar() => PlaySFX(crawlingCaterpillar);
    //public void PlayCrawlingAnt() => PlaySFX(crawlingAnt);
    public void PlayPlayerRunning() => PlaySFX(playerRunning);
    public void PlayPlayerAttacking() => PlaySFX(playerAttacking);
    public void PlayPlayerUseItemToHeal() => PlaySFX(playerUseItemToHeal);
    public void PlayPlayerGetHit() => PlaySFX(playerGetHit);
    public void PlayEnemyGetHit() => PlaySFX(enemyGetHit);

}
