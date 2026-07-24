using UnityEngine;

public class AimController : MonoBehaviour
{
    [Header("Angle Settings")]
    [SerializeField] private float minimumAngle = 105f;
    [SerializeField] private float maximumAngle = 165f;
    [SerializeField] private float rotationSpeed = 60f;

    private float currentAngle;
    private int movementDirection = 1;
    private bool isAiming;

    public float CurrentAngle => currentAngle;
    public Vector2 AimDirection => transform.right;

    private void Awake()
    {
        currentAngle = minimumAngle;
        ApplyRotation();
    }

    private void Update()
    {
        if (!isAiming)
        {
            return;
        }

        MoveAimArrow();
    }

    private void MoveAimArrow()
    {
        currentAngle +=
            rotationSpeed *
            movementDirection *
            Time.deltaTime;

        if (currentAngle >= maximumAngle)
        {
            currentAngle = maximumAngle;
            movementDirection = -1;
        }
        else if (currentAngle <= minimumAngle)
        {
            currentAngle = minimumAngle;
            movementDirection = 1;
        }

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        transform.rotation =
            Quaternion.Euler(0f, 0f, currentAngle);
    }

    public void StartAiming()
    {
        isAiming = true;
        gameObject.SetActive(true);
    }

    public void StopAiming()
    {
        isAiming = false;
    }

    public void HideArrow()
    {
        isAiming = false;
        gameObject.SetActive(false);
    }

    public void ResetAim()
    {
        currentAngle = minimumAngle;
        movementDirection = 1;
        ApplyRotation();
    }
}