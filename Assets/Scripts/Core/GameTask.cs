using System;
using UnityEngine;

/// <summary>
/// Tek bir gorevi temsil eder. Kimlik ve baslik Inspector'dan ayarlanir,
/// tamamlanma durumu oyun sirasinda tutulur.
/// </summary>
[Serializable]
public class GameTask
{
    [Tooltip("Gorevi kod tarafinda bulmak icin benzersiz kimlik.")]
    [SerializeField] private string taskId;

    [Tooltip("Oyuncuya gosterilecek gorev basligi.")]
    [SerializeField] private string title;

    [TextArea]
    [Tooltip("Gorevin aciklamasi (opsiyonel).")]
    [SerializeField] private string description;

    public string TaskId => taskId;
    public string Title => title;
    public string Description => description;

    public bool IsCompleted { get; private set; }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }

    public void ResetTask()
    {
        IsCompleted = false;
    }
}
