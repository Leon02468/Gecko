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
    //[SerializeField] private AudioClip flyingBee;
    //[SerializeField] private AudioClip crawlingCaterpillar;
    //[SerializeField] private AudioClip crawlingAnt;
    [SerializeField] private AudioClip enemyGetHit;

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

    public void PlayMusic()
    {
        if (musicSource != null && background != null)
        {
            musicSource.clip = background;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
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
