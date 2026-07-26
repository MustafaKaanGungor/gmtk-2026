using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LadderZone : MonoBehaviour
{
    private void Reset()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }
}