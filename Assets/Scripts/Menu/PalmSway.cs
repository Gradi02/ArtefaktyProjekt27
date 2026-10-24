using UnityEngine;

public class PalmSway : MonoBehaviour
{
    private float swayAngle = 7f;
    private float swaySpeed = 1f;

    private Quaternion startingRotation;
    private float timeOffset;

    private void Awake()
    {
        startingRotation = transform.localRotation;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float sway = Mathf.Sin(Time.unscaledTime * swaySpeed + timeOffset) * swayAngle;
        transform.localRotation = startingRotation * Quaternion.Euler(0f, 0f, sway);
    }

    private void OnDisable()
    {
        transform.localRotation = startingRotation;
    }
}
