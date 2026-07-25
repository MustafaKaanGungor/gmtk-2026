using System;

/// <summary>
/// Merkezi, statik sinyal (event) merkezi. Kontrolculer oyun olaylarini burada
/// anlamli isimlerle yayinlar (Raise). Kim dinlerse dinlesin; kontrolculer ses
/// sistemini tanimaz. SoundEventBinder bu sinyalleri dinleyip seslere baglar.
///
/// Kod tarafinda yazim hatasini onlemek icin sabit isimleri kullan:
/// GameSignals.Raise(GameSignals.BagDelivered);
/// SoundEventBinder Inspector'inda ayni string degerleri kullanilir.
/// </summary>
public static class GameSignals
{
    // --- Bilinen sinyal adlari ---
    public const string BagDelivered = "Bag_Delivered";
    public const string BagMissed = "Bag_Missed";
    public const string GameOver = "Game_Over";
    public const string TaskCompleted = "Task_Completed";
    public const string AllTasksCompleted = "All_Tasks_Completed";
    public const string PressureSuccess = "Pressure_Success";
    public const string GateSatisfied = "Gate_Satisfied";
    public const string AllGatesSatisfied = "All_Gates_Satisfied";

    // Loop sesleri: Start ile baslar, Stop ile durur.
    public const string PumpLoopStart = "Pump_Loop_Start";
    public const string PumpLoopStop = "Pump_Loop_Stop";

    /// <summary>Bir sinyal yayinlandiginda tetiklenir. Parametre sinyal adidir.</summary>
    public static event Action<string> Signaled;

    /// <summary>Verilen adla bir sinyal yayinlar.</summary>
    public static void Raise(string signal)
    {
        if (string.IsNullOrEmpty(signal))
        {
            return;
        }

        Signaled?.Invoke(signal);
    }
}
