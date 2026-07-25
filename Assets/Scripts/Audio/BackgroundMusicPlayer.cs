using UnityEngine;

/// <summary>
/// Oyun boyunca surekli calan arka plan muzigi. Kendi AudioSource'unu kullanir
/// (SoundManager'daki efektlerden bagimsiz). Singleton + DontDestroyOnLoad
/// sayesinde sahne degisince muzik yeniden baslamaz, kesintisiz devam eder.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class BackgroundMusicPlayer : MonoBehaviour
{
    public static BackgroundMusicPlayer Instance { get; private set; }

    [Header("Music")]
    [Tooltip("Surekli calacak arka plan muzigi.")]
    [SerializeField] private AudioClip musicClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    [Tooltip("Baslangicta otomatik calmaya baslasin mi.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Sahne degisince muzik korunsun mu (kesintisiz devam).")]
    [SerializeField] private bool persistAcrossScenes = true;

    private AudioSource source;

    public float Volume
    {
        get => volume;
        set
        {
            volume = Mathf.Clamp01(value);

            if (source != null)
            {
                source.volume = volume;
            }
        }
    }

    private void Awake()
    {
        // Ikinci bir kopya olusursa (ornegin 2. sahnede de varsa) yok et,
        // mevcut muzik kesintisiz devam etsin.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        source = GetComponent<AudioSource>();
        source.clip = musicClip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume;
    }

    private void Start()
    {
        if (playOnStart && musicClip != null)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Play()
    {
        if (source.clip == null)
        {
            return;
        }

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    public void Stop()
    {
        source.Stop();
    }

    public void Pause()
    {
        source.Pause();
    }

    public void Resume()
    {
        source.UnPause();
    }

    /// <summary>Muzigi degistirir ve (istenirse) hemen calmaya baslar.</summary>
    public void ChangeMusic(AudioClip newClip, bool playImmediately = true)
    {
        musicClip = newClip;
        source.clip = newClip;

        if (playImmediately && newClip != null)
        {
            source.Play();
        }
    }
}
