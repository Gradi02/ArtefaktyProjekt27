using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScaleOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Image indicatorArrow;

    [SerializeField] private float inactiveIndicatorScale = 0.7f;
    [SerializeField] private float inactiveIndicatorAlpha = 0.3f;

    [Header("Skalowanie")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float speed = 15f;

    [SerializeField] private Vector3 originalScale = Vector3.one;

    [Header("Obrót")]
    private float hoverRotation = 2f;

    [Header("Sprite Swap")]
    [SerializeField] private Sprite hoveredSprite; // Nowe pole na podmieniany sprite
    [SerializeField] private Image targetImage;    // Obrazek, na którym podmieniamy (jeśli pusty, pobierze automatycznie z tego obiektu)
    private Sprite defaultSprite;

    private Vector3 targetScale;
    private Quaternion originalRotation;
    private bool isHovered = false;
    private bool isSelected = false;
    private float enableTime;
    private Selectable selectableComponent;
    private Button button;
    private static bool isNavigationMode = false;

    private void Awake()
    {
        selectableComponent = GetComponent<Selectable>();
        button = GetComponent<Button>();

        // Pobranie Image i zapamiętanie domyślnego sprite'a
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        if (targetImage != null)
        {
            defaultSprite = targetImage.sprite;
        }

        if (transform.localScale.sqrMagnitude < 0.01f)
        {
            originalScale = Vector3.one;
        }
        else
        {
            originalScale = transform.localScale;
        }
        targetScale = originalScale;
        originalRotation = transform.localRotation;

        if (indicatorArrow != null)
        {
            indicatorArrow.transform.localScale = Vector3.one * inactiveIndicatorScale;

            Color initialColor = indicatorArrow.color;
            initialColor.a = inactiveIndicatorAlpha;
            indicatorArrow.color = initialColor;
        }
    }

    private void Update()
    {
        if (button != null && !button.interactable) return;

        if (transform.localScale.x < 0.1f && !isHovered && !isSelected)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f)
        {
            if (isNavigationMode)
            {
                isNavigationMode = false;
            }
        }

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            isNavigationMode = true;
        }

        if (Gamepad.current != null)
        {
            bool gamepadMoved = Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f ||
                                Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f;

            bool gamepadButtonPressed = Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                        Gamepad.current.buttonEast.wasPressedThisFrame ||
                                        Gamepad.current.buttonWest.wasPressedThisFrame ||
                                        Gamepad.current.buttonNorth.wasPressedThisFrame;

            if (gamepadMoved || gamepadButtonPressed)
            {
                isNavigationMode = true;
            }
        }

        bool isActive = isHovered || (isSelected && isNavigationMode);

        // Podmiana sprite'a (jeśli hoveredSprite jest przypisany)
        if (targetImage != null && hoveredSprite != null)
        {
            Sprite desiredSprite = isActive ? hoveredSprite : defaultSprite;
            if (targetImage.sprite != desiredSprite)
            {
                targetImage.sprite = desiredSprite;
            }
        }

        targetScale = isActive ? originalScale * scaleMultiplier : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);

        Quaternion targetRotation = originalRotation * Quaternion.Euler(0f, 0f, isActive ? hoverRotation : 0f);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * speed);

        if (indicatorArrow != null)
        {
            Vector3 arrowTargetScale = isActive ? Vector3.one : Vector3.one * inactiveIndicatorScale;
            indicatorArrow.transform.localScale = Vector3.Lerp(
                indicatorArrow.transform.localScale,
                arrowTargetScale,
                Time.unscaledDeltaTime * speed
            );

            float targetAlpha = isActive ? 1f : inactiveIndicatorAlpha;
            Color currentColor = indicatorArrow.color;
            currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.unscaledDeltaTime * speed);
            indicatorArrow.color = currentColor;
        }
    }

    private void OnEnable()
    {
        enableTime = Time.unscaledTime;
    }

    private void OnDisable()
    {
        transform.localRotation = originalRotation;

        // Przywrócenie domyślnego sprite'a przy wyłączeniu obiektu
        if (targetImage != null && defaultSprite != null)
        {
            targetImage.sprite = defaultSprite;
        }

        if (indicatorArrow != null)
        {
            indicatorArrow.transform.localScale = Vector3.one * inactiveIndicatorScale;
            Color c = indicatorArrow.color;
            c.a = inactiveIndicatorAlpha;
            indicatorArrow.color = c;
        }

        isHovered = false;
        isSelected = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectableComponent != null && !selectableComponent.interactable)
        {
            return;
        }
        isHovered = true;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
        isNavigationMode = false;
        PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        PlayHoverSound();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    private void PlayHoverSound()
    {
        if (Time.unscaledTime - enableTime < 0.1f) return;

        //AudioManager.Instance.PlayUIHover();
    }

    public void SetState(bool hovered, bool selected)
    {
        isHovered = hovered;
        isSelected = selected;
    }
}