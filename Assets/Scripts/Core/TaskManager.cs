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

        GameSignals.Raise(GameSignals.TaskCompleted);

        TaskCompleted?.Invoke(task);

        if (AreAllTasksCompleted)
        {
            GameSignals.Raise(GameSignals.AllTasksCompleted);

            AllTasksCompleted?.Invoke();
        }

        return true;
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
