using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyundaki gorevleri tutar ve tamamlanmalarini takip eder.
/// Minigame'ler tamamlandiginda CompleteTask cagirarak buraya haber verir.
/// </summary>
[DisallowMultipleComponent]
public class TaskManager : MonoBehaviour
{
    [Header("Tasks")]
    [SerializeField]
    private List<GameTask> tasks = new List<GameTask>();

    [Header("Sound Events")]
    [Tooltip("Bir gorev tamamlaninca calinacak sesin adi. Bos ise calmaz.")]
    [SerializeField]
    private string taskCompletedSound = "Task_Completed";

    [Tooltip("Butun gorevler bitince calinacak sesin adi. Bos ise calmaz.")]
    [SerializeField]
    private string allTasksCompletedSound = "All_Tasks_Completed";

    /// <summary>Bir gorev tamamlandiginda tetiklenir.</summary>
    public event Action<GameTask> TaskCompleted;

    /// <summary>Butun gorevler tamamlandiginda tetiklenir.</summary>
    public event Action AllTasksCompleted;

    public IReadOnlyList<GameTask> Tasks => tasks;

    public int TotalTaskCount => tasks.Count;

    public int CompletedTaskCount
    {
        get
        {
            int count = 0;

            for (int index = 0; index < tasks.Count; index++)
            {
                if (tasks[index] != null && tasks[index].IsCompleted)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public bool AreAllTasksCompleted =>
        tasks.Count > 0 &&
        CompletedTaskCount >= tasks.Count;

    private void Start()
    {
        // Oyun basinda tum gorevleri temiz baslat.
        ResetAllTasks();
    }

    /// <summary>
    /// Verilen kimlige sahip gorevi tamamlanmis olarak isaretler.
    /// Basariyla isaretlerse true doner.
    /// </summary>
    public bool CompleteTask(string taskId)
    {
        GameTask task = GetTask(taskId);

        if (task == null)
        {
            Debug.LogWarning(
                "TaskManager: '" + taskId +
                "' kimlikli gorev bulunamadi.",
                this
            );

            return false;
        }

        if (task.IsCompleted)
        {
            // Zaten tamamlanmis, tekrar tetiklemiyoruz.
            return false;
        }

        task.MarkCompleted();

        PlaySound(taskCompletedSound);

        TaskCompleted?.Invoke(task);

        if (AreAllTasksCompleted)
        {
            PlaySound(allTasksCompletedSound);

            AllTasksCompleted?.Invoke();
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

    public GameTask GetTask(string taskId)
    {
        for (int index = 0; index < tasks.Count; index++)
        {
            GameTask task = tasks[index];

            if (task != null && task.TaskId == taskId)
            {
                return task;
            }
        }

        return null;
    }

    public bool IsTaskCompleted(string taskId)
    {
        GameTask task = GetTask(taskId);

        return task != null && task.IsCompleted;
    }

    public void ResetAllTasks()
    {
        for (int index = 0; index < tasks.Count; index++)
        {
            if (tasks[index] != null)
            {
                tasks[index].ResetTask();
            }
        }
    }
}
