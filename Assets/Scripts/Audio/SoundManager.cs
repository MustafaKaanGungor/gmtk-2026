using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inspector'dan tanimlanan bir ses kutuphanesini yonetir. Her giris bir
/// "event adi" ile bir veya birden fazla AudioClip esler. Play("eventAdi")
/// cagrisiyla (koddan, UnityEvent'ten veya Animation Event'ten) ses calar.
///
/// Moduler yapi: yeni bir ses eklemek icin Inspector'daki "Sounds" listesine
/// bir giris ekle, event adini yaz ve altina klip(ler)i surukle.
/// </summary>
[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    /// <summary>Kolay erisim icin global ornek (opsiyonel).</summary>
    public static SoundManager Instance { get; private set; }

    [Header("Sound Library")]
    [Tooltip("Her giris bir event adini ses(ler)e baglar.")]
    [SerializeField] private SoundEntry[] sounds;

    [Header("Audio Source Pool")]
    [Tooltip("Ayni anda calabilecek ses sayisi. Yetmezse otomatik buyur.")]
    [Min(1)]
    [SerializeField] private int poolSize = 8;

    [Header("Master")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Tooltip("Sahne degisince yok olmasin (kalici SoundManager).")]
    [SerializeField] private bool persistAcrossScenes = false;

    private readonly Dictionary<string, SoundEntry> lookup =
        new Dictionary<string, SoundEntry>();

    private readonly List<AudioSource> pool =
        new List<AudioSource>();

    private readonly Dictionary<string, AudioSource> activeLoops =
        new Dictionary<string, AudioSource>();

    public float MasterVolume
    {
        get => masterVolume;
        set => masterVolume = Mathf.Clamp01(value);
    }

    private void Awake()
    {
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

        BuildLookup();
        CreatePool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BuildLookup()
    {
        lookup.Clear();

        if (sounds == null)
        {
            return;
        }

        for (int index = 0; index < sounds.Length; index++)
        {
            SoundEntry entry = sounds[index];

            if (entry == null || string.IsNullOrEmpty(entry.EventName))
            {
                continue;
            }

            if (lookup.ContainsKey(entry.EventName))
            {
                Debug.LogWarning(
                    "SoundManager: '" + entry.EventName +
                    "' event adi birden fazla kez tanimlanmis.",
                    this
                );

                continue;
            }

            lookup.Add(entry.EventName, entry);
        }
    }

    private void CreatePool()
    {
        for (int index = 0; index < poolSize; index++)
        {
            CreateSource();
        }
    }

    private AudioSource CreateSource()
    {
        GameObject sourceObject = new GameObject(
            "PooledAudioSource_" + pool.Count
        );

        sourceObject.transform.SetParent(transform);

        AudioSource source =
            sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;

        pool.Add(source);

        return source;
    }

    private AudioSource GetFreeSource()
    {
        for (int index = 0; index < pool.Count; index++)
        {
            if (!pool[index].isPlaying)
            {
                return pool[index];
            }
        }

        // Hepsi mesgulse havuzu buyut.
        return CreateSource();
    }

    /// <summary>
    /// Verilen event adina bagli sesi calar. Ana giris noktasi;
    /// Animation Event ve UnityEvent'ler de bunu string ile cagirabilir.
    /// </summary>
    public void Play(string eventName)
    {
        PlayInternal(eventName, false, Vector3.zero);
    }

    /// <summary>Sesi belirli bir dunya konumunda calar (3D icin).</summary>
    public void PlayAtPosition(string eventName, Vector3 worldPosition)
    {
        PlayInternal(eventName, true, worldPosition);
    }

    private void PlayInternal(
        string eventName,
        bool usePosition,
        Vector3 worldPosition)
    {
        if (!lookup.TryGetValue(eventName, out SoundEntry entry))
        {
            Debug.LogWarning(
                "SoundManager: '" + eventName +
                "' adinda bir ses bulunamadi.",
                this
            );

            return;
        }

        AudioClip clip = entry.GetClip();

        if (clip == null)
        {
            Debug.LogWarning(
                "SoundManager: '" + eventName +
                "' icin atanmis klip yok.",
                this
            );

            return;
        }

        AudioSource source = GetFreeSource();

        source.clip = clip;
        source.volume = entry.Volume * masterVolume;
        source.pitch = entry.GetPitch();
        source.loop = entry.Loop;
        source.spatialBlend = entry.SpatialBlend;

        if (usePosition)
        {
            source.transform.position = worldPosition;
        }
        else
        {
            source.transform.localPosition = Vector3.zero;
        }

        source.Play();

        if (entry.Loop)
        {
            activeLoops[eventName] = source;
        }
    }

    /// <summary>Dongu halindeki bir sesi durdurur.</summary>
    public void Stop(string eventName)
    {
        if (activeLoops.TryGetValue(eventName, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
            }

            activeLoops.Remove(eventName);
        }
    }

    /// <summary>Butun sesleri durdurur.</summary>
    public void StopAll()
    {
        for (int index = 0; index < pool.Count; index++)
        {
            pool[index].Stop();
        }

        activeLoops.Clear();
    }
}

/// <summary>
/// Tek bir ses tanimini tutar: event adi + klip(ler) + calma ayarlari.
/// Birden fazla klip verilirse her calmada rastgele biri secilir.
/// </summary>
[Serializable]
public class SoundEntry
{
    [Tooltip("Play(...) ile cagrilacak benzersiz event adi. Ornek: 'jump'.")]
    [SerializeField] private string eventName;

    [Tooltip("Calinacak klip(ler). Birden fazlaysa rastgele biri secilir.")]
    [SerializeField] private AudioClip[] clips;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Tooltip("Pitch alt siniri (cesitlilik icin).")]
    [SerializeField] private float minPitch = 1f;

    [Tooltip("Pitch ust siniri (cesitlilik icin).")]
    [SerializeField] private float maxPitch = 1f;

    [Tooltip("Ses donguye alinsin mi. Durdurmak icin Stop(eventName).")]
    [SerializeField] private bool loop = false;

    [Tooltip("0 = 2D (her yerde ayni), 1 = 3D (konuma bagli).")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 0f;

    public string EventName => eventName;
    public bool Loop => loop;
    public float SpatialBlend => spatialBlend;

    // Not: Inspector'da listeye yeni eleman eklenince [Serializable] alanlarin
    // '= 1f' varsayilanlari uygulanmaz; deger 0 kalir. Bu yuzden 0 (ayarlanmamis)
    // gelen Volume/Pitch degerlerini 1 kabul ediyoruz ki ses sessiz calmasin.
    public float Volume => volume <= 0f ? 1f : volume;

    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        if (clips.Length == 1)
        {
            return clips[0];
        }

        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    public float GetPitch()
    {
        float min = minPitch <= 0f ? 1f : minPitch;
        float max = maxPitch <= 0f ? 1f : maxPitch;

        if (Mathf.Approximately(min, max))
        {
            return min;
        }

        return UnityEngine.Random.Range(min, max);
    }
}
