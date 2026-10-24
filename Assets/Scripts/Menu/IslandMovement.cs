using UnityEngine;

public class IslandMovement : MonoBehaviour
{
    private float speed;

    public void Initialize(float movementSpeed)
    {
        speed = Mathf.Max(0f, movementSpeed);
    }

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.unscaledDeltaTime;
    }
}
