using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class CardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private Image artworkImage;

    public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
    public CanvasGroup CanvasGroup => canvasGroup != null ? canvasGroup : (canvasGroup = GetComponent<CanvasGroup>());

    private void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Ustawia właściwe dane karty.
    /// </summary>
    public void Setup(string title, string cardType, int cost)
    {
        if (titleText != null) titleText.text = title;
        if (typeText != null) typeText.text = cardType;
        if (costText != null) costText.text = cost.ToString();
    }

    /// <summary>
    /// Ustawia dane dla karty testowej / placeholdera.
    /// </summary>
    public void SetupPlaceholder(int cardId)
    {
        if (titleText != null) titleText.text = $"Card {cardId:00}";
        if (typeText != null) typeText.text = "Placeholder";
        if (costText != null) costText.text = "0";
    }
}