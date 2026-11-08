using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class MenuStormLightDOTween : MonoBehaviour
{
    [Header("Baza (Klimat tła)")]
    [SerializeField] private Color baseColor = new Color(0.1f, 0.12f, 0.22f, 1f);
    [SerializeField, Range(0f, 1f)] private float baseIntensity = 0.15f;

    [Header("Główny Błysk (Piorun)")]
    [SerializeField] private Color flashColor = new Color(0.92f, 0.97f, 1f, 1f);
    [SerializeField, Range(1f, 6f)] private float maxFlashIntensity = 3.2f;

    [Header("Czas i Płynność (DOTween)")]
    [Tooltip("Czas trwania wejścia błysku (bardzo krótki dla uderzenia)")]
    [SerializeField] private float strikeInDuration = 0.02f;
    [Tooltip("Czas płynnego gaśnięcia pioruna (powidok po uderzeniu)")]
    [SerializeField] private float fadeOutDuration = 0.45f;
    [Tooltip("Krzywa gaśnięcia światła (OutQuad lub OutCubic dają najbardziej naturalny efekt)")]
    [SerializeField] private Ease fadeOutEase = Ease.OutQuad;

    [Header("Dynamika (Mikro-błyski)")]
    [Tooltip("Minimalna i maksymalna liczba dodatkowych mignięć w jednym uderzeniu")]
    [SerializeField] private Vector2Int flickersRange = new Vector2Int(1, 4);
    [Tooltip("Czas trwania pojedynczego mikro-mignięcia")]
    [SerializeField] private float flickerDuration = 0.04f;

    [Header("Odstępy między wyładowaniami")]
    [SerializeField] private float minStrikeInterval = 3f;
    [SerializeField] private float maxStrikeInterval = 8f;

    private Light2D stormLight;
    private Coroutine stormCoroutine;
    private Sequence lightningSequence;

    private void Awake()
    {
        stormLight = GetComponent<Light2D>();
        if (stormLight == null)
        {
            stormLight = gameObject.AddComponent<Light2D>();
        }

        stormLight.lightType = Light2D.LightType.Global;
        stormLight.blendStyleIndex = 0;
        
        // Ustawienie początkowych wartości
        stormLight.color = baseColor;
        stormLight.intensity = baseIntensity;
    }

    private void OnEnable()
    {
        stormCoroutine = StartCoroutine(StormLoop());
    }

    private void OnDisable()
    {
        if (stormCoroutine != null) 
            StopCoroutine(stormCoroutine);

        // Zawsze czyścimy animacje tween przy wyłączeniu obiektu
        lightningSequence?.Kill();
    }

    private IEnumerator StormLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minStrikeInterval, maxStrikeInterval);
            yield return new WaitForSeconds(waitTime);

            PlayLightningSequence();
        }
    }

    private void PlayLightningSequence()
    {
        // Anulujemy poprzednią sekwencję, jeśli jeszcze trwała
        lightningSequence?.Kill();
        lightningSequence = DOTween.Sequence();

        int flickers = Random.Range(flickersRange.x, flickersRange.y + 1);

        // 1. Seria szybkich mikro-uderzeń na start
        for (int i = 0; i < flickers; i++)
        {
            // Losujemy lekkie wahania mocy pojedynczego uderzenia
            float subIntensity = Random.Range(maxFlashIntensity * 0.5f, maxFlashIntensity);

            // Błysk w górę
            lightningSequence.Append(stormLight.DOIntensity(subIntensity, strikeInDuration));
            lightningSequence.Join(stormLight.DOColor(flashColor, strikeInDuration));

            // Szybki spadek (ale nie do zera, tylko lekko przygasa)
            float dipIntensity = Random.Range(baseIntensity, baseIntensity * 2.5f);
            lightningSequence.Append(stormLight.DOIntensity(dipIntensity, flickerDuration));
        }

        // 2. Finałowy, najmocniejszy błysk
        lightningSequence.Append(stormLight.DOIntensity(maxFlashIntensity, strikeInDuration));

        // 3. Organiczne, płynne wygaszanie światła i powrót do barwy nocy
        lightningSequence.Append(stormLight.DOIntensity(baseIntensity, fadeOutDuration).SetEase(fadeOutEase));
        lightningSequence.Join(stormLight.DOColor(baseColor, fadeOutDuration * 0.8f).SetEase(Ease.InQuad));
    }
}

// Rozszerzenia DOTween dla Light2D, by pisać czysty kod bez kombinowania
public static class Light2DExtensions
{
    public static TweenerCore<float, float, FloatOptions> DOIntensity(this Light2D light, float endValue, float duration)
    {
        return DOTween.To(() => light.intensity, x => light.intensity = x, endValue, duration);
    }

    public static TweenerCore<Color, Color, ColorOptions> DOColor(this Light2D light, Color endValue, float duration)
    {
        return DOTween.To(() => light.color, x => light.color = x, endValue, duration);
    }
}