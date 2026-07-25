using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameSignals'tan gelen sinyalleri seslere baglar. Butun "hangi olayda hangi
/// ses calar" karari burada, Inspector'daki listede toplanir. Kontrolculeri
/// degistirmeden yeni ses eklemek/degistirmek icin bu listeyi duzenle.
///
/// Moduler kullanim: listeye bir satir ekle, sinyal adini yaz, calmasini
/// istedigin sesin adini (SoundManager'daki Event Name) yaz.
/// </summary>
[DisallowMultipleComponent]
public class SoundEventBinder : MonoBehaviour
{
    public enum SoundAction
    {
        Play,
        Stop
    }

    [Serializable]
    public class SignalSoundBinding
    {
        [Tooltip("Dinlenecek sinyal adi (GameSignals'taki deger).")]
        public string signalName;

        [Tooltip("Calinacak/durdurulacak sesin adi (SoundManager'daki Event Name). " +
            "Bos birakilirsa sinyal adi ses adi olarak kullanilir.")]
        public string soundName;

        [Tooltip("Play: sesi calar (loop sesler icin baslatir). " +
            "Stop: calan (loop) sesi durdurur.")]
        public SoundAction action = SoundAction.Play;
    }

    [Header("Signal -> Sound Bindings")]
    [SerializeField]
    private List<SignalSoundBinding> bindings = new List<SignalSoundBinding>();

    // Bir sinyale birden fazla ses/aksiyon baglanabilsin diye liste tutuyoruz.
    private readonly Dictionary<string, List<SignalSoundBinding>> map =
        new Dictionary<string, List<SignalSoundBinding>>();

    private void Awake()
    {
        BuildMap();
    }

    private void OnEnable()
    {
        GameSignals.Signaled += HandleSignal;
    }

    private void OnDisable()
    {
        GameSignals.Signaled -= HandleSignal;
    }

    private void BuildMap()
    {
        map.Clear();

        for (int index = 0; index < bindings.Count; index++)
        {
            SignalSoundBinding binding = bindings[index];

            if (binding == null || string.IsNullOrEmpty(binding.signalName))
            {
                continue;
            }

            if (!map.TryGetValue(
                    binding.signalName,
                    out List<SignalSoundBinding> list))
            {
                list = new List<SignalSoundBinding>();
                map.Add(binding.signalName, list);
            }

            list.Add(binding);
        }
    }

    private void HandleSignal(string signalName)
    {
        if (!map.TryGetValue(signalName, out List<SignalSoundBinding> list))
        {
            return;
        }

        if (SoundManager.Instance == null)
        {
            return;
        }

        for (int index = 0; index < list.Count; index++)
        {
            SignalSoundBinding binding = list[index];

            string soundName =
                string.IsNullOrEmpty(binding.soundName)
                    ? binding.signalName
                    : binding.soundName;

            if (binding.action == SoundAction.Stop)
            {
                SoundManager.Instance.Stop(soundName);
            }
            else
            {
                SoundManager.Instance.Play(soundName);
            }
        }
    }
}
