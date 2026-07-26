using UnityEngine;

/// <summary>
/// UnityEvent'lerden GameSignals'a kopru. Inspector'da bir UnityEvent'e
/// (ornegin ArrowSequenceMinigame.onMinigameOpened) bu bilesenin Raise(string)
/// metodunu baglayip sinyal adini yazarsin; sinyal SoundEventBinder'a duser.
/// Boylece UnityEvent tabanli minigame'ler de merkezi ses sistemini kullanir.
/// </summary>
[DisallowMultipleComponent]
public class GameSignalEmitter : MonoBehaviour
{
    /// <summary>UnityEvent'ten cagrilir; verilen adla sinyal yayinlar.</summary>
    public void Raise(string signalName)
    {
        GameSignals.Raise(signalName);
    }
}
