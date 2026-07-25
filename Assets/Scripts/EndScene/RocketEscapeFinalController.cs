using UnityEngine;

public class RocketEscapeFinalController : MonoBehaviour
{
    [Header("Future Boss Reference")]
    [Tooltip(
        "Diş boss hazır olduğunda Boss Root nesnesi "
        + "buraya bağlanacak."
    )]
    [SerializeField] private GameObject bossRoot;

    private bool finalStarted;

    private void Awake()
    {
        finalStarted = false;

        if (bossRoot != null)
        {
            bossRoot.SetActive(false);
        }
    }

    public void BeginFinalSequence()
    {
        if (finalStarted)
        {
            return;
        }

        finalStarted = true;

        Debug.Log(
            "FINAL BAŞLADI: Diş boss sekansı burada çalışacak.",
            this
        );

        if (bossRoot != null)
        {
            bossRoot.SetActive(true);
        }
    }
}