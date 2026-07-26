using System.Collections;
using UnityEngine;

public class RocketEscapeFinalController : MonoBehaviour
{
    [Header("Boss References")]
    [SerializeField] private GameObject bossRoot;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private Transform bossTargetPoint;

    [Header("Attack References")]
    [SerializeField] private ToothRainSpawner toothRainSpawner;
    [SerializeField] private RocketDeathController deathController;

    [Header("Boss Entrance")]
    [Min(0.01f)]
    [SerializeField] private float bossEntranceDuration = 1.5f;

    [Min(0f)]
    [SerializeField] private float waitAfterBossEntrance = 0.75f;

    [Header("Forced Story Death")]
    [Tooltip(
        "Diş yağmuru başladıktan kaç saniye sonra "
        + "oyuncunun kesin olarak öldürüleceği."
    )]
    [Min(0f)]
    [SerializeField] private float forcedDeathDelay = 3f;

    private bool finalStarted;

    private void Awake()
    {
        finalStarted = false;

        if (bossRoot != null)
        {
            bossRoot.SetActive(false);
        }

        if (toothRainSpawner != null)
        {
            toothRainSpawner.StopRain();
        }
    }

    public void BeginFinalSequence()
    {
        if (finalStarted)
        {
            return;
        }

        finalStarted = true;

        StartCoroutine(FinalSequence());
    }

    private IEnumerator FinalSequence()
    {
        if (bossRoot == null
            || bossSpawnPoint == null
            || bossTargetPoint == null)
        {
            Debug.LogError(
                "RocketEscapeFinalController: Boss referansları eksik.",
                this
            );

            yield break;
        }

        bossRoot.SetActive(true);
        bossRoot.transform.position =
            bossSpawnPoint.position;

        Vector3 startingPosition =
            bossSpawnPoint.position;

        Vector3 targetPosition =
            bossTargetPoint.position;

        float elapsedTime = 0f;

        while (elapsedTime < bossEntranceDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / bossEntranceDuration
            );

            // SmoothStep bossun yavaş başlayıp
            // yavaş durmasını sağlar.
            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            bossRoot.transform.position = Vector3.Lerp(
                startingPosition,
                targetPosition,
                smoothProgress
            );

            yield return null;
        }

        bossRoot.transform.position = targetPosition;

        if (waitAfterBossEntrance > 0f)
        {
            yield return new WaitForSeconds(
                waitAfterBossEntrance
            );
        }

        if (toothRainSpawner != null)
        {
            toothRainSpawner.StartRain();
        }

        // Collider aralarından tesadüfen kurtulsa bile
        // hikâye gereği oyuncu kesin olarak ölür.
        if (forcedDeathDelay > 0f)
        {
            yield return new WaitForSeconds(
                forcedDeathDelay
            );
        }

        if (deathController != null
    && !deathController.IsDead)
{
    deathController.KillRocketAndLoadFinalScene();
}
    }
}