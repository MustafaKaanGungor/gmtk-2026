using System;
using UnityEngine;

/// <summary>
/// Birden fazla renkli kapiyi (ColorZone) izler. Her kapi kendi renginde
/// gereken sayida bavul alinca "dolu" olur; butun kapilar dolunca TaskManager'da
/// ilgili gorevi tamamlar.
/// </summary>
[DisallowMultipleComponent]
public class BagGateTaskController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Bu goreve dahil renkli kapilar.")]
    [SerializeField] private ColorZone[] gates;

    [Tooltip("Opsiyonel. Atanirsa butun kapilar dolunca gorev tamamlanir.")]
    [SerializeField] private TaskManager taskManager;

    [Header("Task")]
    [Tooltip("Butun kapilar dolunca tamamlanacak gorevin kimligi.")]
    [SerializeField] private string taskId = "bag_gates";

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

    private void HandleGateSatisfied(ColorZone gate)
    {
        GameSignals.Raise(GameSignals.GateSatisfied);

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

        GameSignals.Raise(GameSignals.AllGatesSatisfied);

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
}
