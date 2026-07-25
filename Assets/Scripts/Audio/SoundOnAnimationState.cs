using UnityEngine;

/// <summary>
/// Bir Animator state'ine takilir. State'e girildiginde (ve istenirse
/// cikildiginda) SoundManager uzerinden ilgili sesi calar.
///
/// Kullanim: Animator penceresinde bir state sec -> Inspector -> Add Behaviour
/// -> SoundOnAnimationState. Ardindan calmasini istedigin event adini yaz.
/// </summary>
public class SoundOnAnimationState : StateMachineBehaviour
{
    [Header("Sound Event Names")]
    [Tooltip("State'e girince calinacak sesin event adi. Bos ise calmaz.")]
    [SerializeField] private string onEnterSound;

    [Tooltip("State'ten cikinca calinacak sesin event adi. Bos ise calmaz.")]
    [SerializeField] private string onExitSound;

    [Tooltip("onEnterSound dongulu bir ses ise, cikista Stop ile durdur.")]
    [SerializeField] private bool stopEnterSoundOnExit = false;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(onEnterSound))
        {
            SoundManager.Instance.Play(onEnterSound);
        }
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        if (stopEnterSoundOnExit && !string.IsNullOrEmpty(onEnterSound))
        {
            SoundManager.Instance.Stop(onEnterSound);
        }

        if (!string.IsNullOrEmpty(onExitSound))
        {
            SoundManager.Instance.Play(onExitSound);
        }
    }
}
