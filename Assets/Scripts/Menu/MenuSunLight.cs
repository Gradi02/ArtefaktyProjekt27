using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MenuSunLight : MonoBehaviour
{
    [SerializeField] private Color lightColor = new Color(1f, 0.82f, 0.58f, 1f);
    [SerializeField, Range(0f, 2f)] private float intensity = 0.25f;

    private Light2D sunLight;

    private void Awake()
    {
        sunLight = gameObject.GetComponent<Light2D>();
        if (sunLight == null)
        {
            sunLight = gameObject.AddComponent<Light2D>();
        }

        sunLight.lightType = Light2D.LightType.Global;
        sunLight.color = lightColor;
        sunLight.intensity = intensity;
        sunLight.blendStyleIndex = 0;
    }
}
