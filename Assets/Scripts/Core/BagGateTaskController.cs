using System;
using UnityEngine;

/// <summary>
/// Birden fazla renkli kapiyi (TimedDoorGate) izler. Her kapi kendi renginde
/// gereken sayida bavul alinca "dolu" olur; butun kapilar dolunca TaskManager'da
/// ilgili gorevi tamamlar.
/// </summary>
[DisallowMultipleComponent]
public class BagGateTaskController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Bu goreve dahil renkli kapilar.")]
    [SerializeField] private TimedDoorGate[] gates;

    [Tooltip("Opsiyonel. Atanirsa butun kapilar dolunca gorev tamamlanir.")]
    [SerializeField] private TaskManager taskManager;

    [Header("Task")]
    [Tooltip("Butun kapilar dolunca tamamlanacak gorevin kimligi.")]
    [SerializeField] private string taskId = "bag_gates";

    [Header("Sound Events")]
    [Tooltip("Bir kapi dolunca calinacak ses. Bos ise calmaz.")]
    [SerializeField] private string gateSatisfiedSound = "Gate_Completed";

    [Tooltip("Butun kapilar dolunca calinacak ses. Bos ise calmaz.")]
    [SerializeField] private string allGatesSound = "";

    /// <summary>Butun kapilar dolunca (bir kez) tetiklenir.</summary>
    public event Action AllGatesSatisfied;

    private bool completed;

    private void OnEnable()
    {
        if (gates == null)
        {
            return;
        }

        for (int index = 0; index < gates.Length; index++)
        {
            if (gates[index] != null)
            {
                gates[index].Satisfied += HandleGateSatisfied;
            }
        }
    }

    private void OnDisable()
    {
        if (gates == null)
        {
            return;
        }

        for (int index = 0; index < gates.Length; index++)
        {
            if (gates[index] != null)
            {
                gates[index].Satisfied -= HandleGateSatisfied;
            }
        }
    }

    private void HandleGateSatisfied(TimedDoorGate gate)
    {
        PlaySound(gateSatisfiedSound);

        if (completed)
        {
            return;
        }

        if (!AreAllGatesSatisfied())
        {
            return;
        }

        completed = true;

        if (taskManager != null)
        {
            taskManager.CompleteTask(taskId);
        }

        PlaySound(allGatesSound);

        AllGatesSatisfied?.Invoke();

        Debug.Log(
            "BagGateTaskController: Butun kapilar dolduruldu.",
            this
        );
    }

    private bool AreAllGatesSatisfied()
    {
        if (gates == null || gates.Length == 0)
        {
            return false;
        }

        for (int index = 0; index < gates.Length; index++)
        {
            if (gates[index] == null || !gates[index].IsSatisfied)
            {
                return false;
            }
        }

        return true;
    }

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(soundName);
        }
    }
}
