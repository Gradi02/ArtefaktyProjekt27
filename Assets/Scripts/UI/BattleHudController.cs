using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleHudController : MonoBehaviour
{
    private Canvas _canvas;
    private FightManager _fightManager;
    private HandCardAnimator _handCardAnimator;
    private PresentationPlaybackHost _presentationPlaybackHost;
    private UnitStatusPanel _playerPanel;
    private UnitStatusPanel _enemyPanel;
    private TextMeshProUGUI _turnText;
    private TextMeshProUGUI _deckText;
    private Button _endTurnButton;
    private Button _pauseButton;
    private GameObject _pauseOverlay;
    private GameObject _pauseMainPage;
    private GameObject _pauseSettingsPage;
    private GameObject _pauseConfirmPage;
    private TextMeshProUGUI _playbackSpeedText;
    private bool _initialized;
    private bool _isPaused;

    public void Initialize(
        Canvas canvas,
        FightManager fightManager,
        HandCardAnimator handCardAnimator,
        PresentationPlaybackHost presentationPlaybackHost)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Battle HUD has already been initialized.");
        }
        if (canvas == null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }
        if (fightManager == null)
        {
            throw new ArgumentNullException(nameof(fightManager));
        }
        if (handCardAnimator == null)
        {
            throw new ArgumentNullException(nameof(handCardAnimator));
        }
        if (presentationPlaybackHost == null)
        {
            throw new ArgumentNullException(nameof(presentationPlaybackHost));
        }

        _canvas = canvas;
        _fightManager = fightManager;
        _handCardAnimator = handCardAnimator;
        _presentationPlaybackHost = presentationPlaybackHost;
        BuildHud();
        _initialized = true;
        Refresh();
    }

    public void Refresh()
    {
        if (!_initialized)
        {
            return;
        }

        _playerPanel.Refresh();
        _enemyPanel.Refresh();
        _deckText.text =
            $"Ręka: {_fightManager.PlayerDeck.Hand.Count}   " +
            $"Talia: {_fightManager.PlayerDeck.DrawPile.Count}   " +
            $"Odrzucone: {_fightManager.PlayerDeck.DiscardPile.Count}";

        if (_fightManager.IsFightOver)
        {
            _turnText.text = "Walka zakończona";
        }
        else if (_fightManager.IsPresentationPending)
        {
            _turnText.text = "Odtwarzanie akcji...";
        }
        else if (_fightManager.IsTurnStarted &&
                 ReferenceEquals(_fightManager.CurrentActor, _fightManager.PlayerState))
        {
            _turnText.text = "Twoja tura";
        }
        else
        {
            _turnText.text = "Tura przeciwnika";
        }

        _endTurnButton.interactable =
            !_isPaused &&
            !_fightManager.IsFightOver &&
            !_fightManager.IsPresentationPending &&
            _fightManager.IsTurnStarted &&
            ReferenceEquals(_fightManager.CurrentActor, _fightManager.PlayerState);
        _pauseButton.interactable = !_fightManager.IsFightOver;
    }

    private void OnDestroy()
    {
        if (_isPaused)
        {
            if (_presentationPlaybackHost != null)
            {
                _presentationPlaybackHost.Resume();
            }
            if (_handCardAnimator != null)
            {
                _handCardAnimator.SetUiInputEnabled(true);
            }
        }
    }

    private void BuildHud()
    {
        RectTransform hudRoot = CreateRect(
            _canvas.transform,
            "Battle HUD",
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one);

        Image topBar = CreateImage(hudRoot, "Top Bar", new Color(0.04f, 0.05f, 0.08f, 0.88f));
        RectTransform topBarRect = topBar.rectTransform;
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = Vector2.zero;
        topBarRect.sizeDelta = new Vector2(0f, 82f);

        _turnText = CreateText(
            hudRoot,
            "Turn State",
            string.Empty,
            26,
            new Vector2(24f, -13f),
            new Vector2(440f, 32f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));
        _deckText = CreateText(
            hudRoot,
            "Deck Counts",
            string.Empty,
            16,
            new Vector2(24f, -48f),
            new Vector2(650f, 22f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        _pauseButton = CreateButton(
            hudRoot,
            "Pause Button",
            "Pauza",
            new Vector2(-24f, -13f),
            new Vector2(130f, 56f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            TogglePause);
        _endTurnButton = CreateButton(
            hudRoot,
            "End Turn Button",
            "Zakończ turę",
            new Vector2(-168f, -13f),
            new Vector2(220f, 56f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            OnEndTurnClicked);

        _playerPanel = UnitStatusPanel.Create(
            hudRoot,
            "Player Status Panel",
            "GRACZ",
            _fightManager.PlayerState,
            new Vector2(380f, 230f));
        PositionPanel(_playerPanel.transform, new Vector2(24f, -100f), new Vector2(0f, 1f));

        _enemyPanel = UnitStatusPanel.Create(
            hudRoot,
            "Enemy Status Panel",
            "PRZECIWNIK",
            _fightManager.EnemyState,
            new Vector2(380f, 230f));
        PositionPanel(_enemyPanel.transform, new Vector2(-24f, -100f), new Vector2(1f, 1f));

        BuildPauseMenu(hudRoot);
    }

    private void BuildPauseMenu(RectTransform hudRoot)
    {
        _pauseOverlay = new GameObject("Pause Overlay", typeof(RectTransform), typeof(Image));
        _pauseOverlay.transform.SetParent(hudRoot, false);
        RectTransform overlayRect = _pauseOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = _pauseOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.72f);
        overlayImage.raycastTarget = true;
        _pauseOverlay.SetActive(false);

        RectTransform dialog = CreateRect(
            overlayRect,
            "Pause Dialog",
            Vector2.zero,
            new Vector2(500f, 450f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
        Image dialogImage = dialog.gameObject.AddComponent<Image>();
        dialogImage.color = new Color(0.08f, 0.1f, 0.14f, 1f);
        dialogImage.raycastTarget = false;

        _pauseMainPage = CreatePausePage(dialog, "Pause Main Page");
        CreateText(
            (RectTransform)_pauseMainPage.transform,
            "Pause Title",
            "PAUZA",
            34,
            new Vector2(0f, -24f),
            new Vector2(420f, 46f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            TextAlignmentOptions.Center);
        CreateButton(
            (RectTransform)_pauseMainPage.transform,
            "Resume Button",
            "Wznów",
            new Vector2(0f, -105f),
            new Vector2(300f, 58f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            () => SetPaused(false));
        CreateButton(
            (RectTransform)_pauseMainPage.transform,
            "Settings Button",
            "Ustawienia",
            new Vector2(0f, -180f),
            new Vector2(300f, 58f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            ShowSettingsPage);
        CreateButton(
            (RectTransform)_pauseMainPage.transform,
            "Abandon Run Button",
            "Porzuć run",
            new Vector2(0f, -255f),
            new Vector2(300f, 58f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            ShowAbandonConfirmation);

        _pauseSettingsPage = CreatePausePage(dialog, "Pause Settings Page");
        CreateText(
            (RectTransform)_pauseSettingsPage.transform,
            "Settings Title",
            "USTAWIENIA",
            30,
            new Vector2(0f, -28f),
            new Vector2(420f, 42f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            TextAlignmentOptions.Center);
        CreateText(
            (RectTransform)_pauseSettingsPage.transform,
            "Playback Speed Label",
            "Szybkość animacji prezentacji",
            18,
            new Vector2(0f, -110f),
            new Vector2(400f, 28f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            TextAlignmentOptions.Center);
        _playbackSpeedText = CreateText(
            (RectTransform)_pauseSettingsPage.transform,
            "Playback Speed Value",
            string.Empty,
            18,
            new Vector2(0f, -150f),
            new Vector2(400f, 28f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            TextAlignmentOptions.Center);
        CreateButton(
            (RectTransform)_pauseSettingsPage.transform,
            "Slow Playback Button",
            "0.75x",
            new Vector2(-125f, -205f),
            new Vector2(110f, 52f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            () => SetPlaybackSpeed(0.75f));
        CreateButton(
            (RectTransform)_pauseSettingsPage.transform,
            "Normal Playback Button",
            "1x",
            new Vector2(0f, -205f),
            new Vector2(110f, 52f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            () => SetPlaybackSpeed(1f));
        CreateButton(
            (RectTransform)_pauseSettingsPage.transform,
            "Fast Playback Button",
            "1.5x",
            new Vector2(125f, -205f),
            new Vector2(110f, 52f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            () => SetPlaybackSpeed(1.5f));
        CreateButton(
            (RectTransform)_pauseSettingsPage.transform,
            "Settings Back Button",
            "Wróć",
            new Vector2(0f, -330f),
            new Vector2(200f, 54f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            ShowMainPausePage);

        _pauseConfirmPage = CreatePausePage(dialog, "Pause Confirm Page");
        CreateText(
            (RectTransform)_pauseConfirmPage.transform,
            "Abandon Title",
            "Porzucić ten run?",
            28,
            new Vector2(0f, -64f),
            new Vector2(420f, 42f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            TextAlignmentOptions.Center);
        CreateText(
            (RectTransform)_pauseConfirmPage.transform,
            "Abandon Description",
            "Postęp bieżącego runu zostanie utracony.",
            17,
            new Vector2(0f, -120f),
            new Vector2(420f, 34f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            TextAlignmentOptions.Center);
        CreateButton(
            (RectTransform)_pauseConfirmPage.transform,
            "Confirm Abandon Button",
            "Porzuć run",
            new Vector2(0f, -205f),
            new Vector2(300f, 58f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            AbandonRun);
        CreateButton(
            (RectTransform)_pauseConfirmPage.transform,
            "Cancel Abandon Button",
            "Anuluj",
            new Vector2(0f, -285f),
            new Vector2(300f, 58f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            ShowMainPausePage);
    }

    private void OnEndTurnClicked()
    {
        _fightManager.EndPlayerTurn();
        Refresh();
    }

    private void TogglePause()
    {
        SetPaused(!_isPaused);
    }

    private void SetPaused(bool isPaused)
    {
        if (_isPaused == isPaused)
        {
            return;
        }

        _isPaused = isPaused;
        _pauseOverlay.SetActive(isPaused);
        _handCardAnimator.SetUiInputEnabled(!isPaused);
        if (isPaused)
        {
            _presentationPlaybackHost.Pause();
            ShowMainPausePage();
        }
        else
        {
            _presentationPlaybackHost.Resume();
        }

        Refresh();
    }

    private void ShowSettingsPage()
    {
        SetPausePage(_pauseSettingsPage);
        UpdatePlaybackSpeedText();
    }

    private void ShowAbandonConfirmation()
    {
        SetPausePage(_pauseConfirmPage);
    }

    private void ShowMainPausePage()
    {
        SetPausePage(_pauseMainPage);
    }

    private void SetPausePage(GameObject page)
    {
        _pauseMainPage.SetActive(page == _pauseMainPage);
        _pauseSettingsPage.SetActive(page == _pauseSettingsPage);
        _pauseConfirmPage.SetActive(page == _pauseConfirmPage);
    }

    private void SetPlaybackSpeed(float speed)
    {
        _presentationPlaybackHost.PlaybackSpeed = speed;
        UpdatePlaybackSpeedText();
    }

    private void UpdatePlaybackSpeedText()
    {
        _playbackSpeedText.text =
            $"Aktualnie: {_presentationPlaybackHost.PlaybackSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}x";
    }

    private void AbandonRun()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("Cannot abandon the run because GameManager is unavailable.", this);
            return;
        }

        gameManager.EndRun();
    }

    private static GameObject CreatePausePage(RectTransform parent, string objectName)
    {
        GameObject page = new GameObject(objectName, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return page;
    }

    private static Button CreateButton(
        RectTransform parent,
        string objectName,
        string label,
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Action onClick)
    {
        RectTransform rect = CreateRect(parent, objectName, position, size, anchorMin, anchorMax);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.32f, 0.48f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.16f, 0.32f, 0.48f, 1f);
        colors.highlightedColor = new Color(0.24f, 0.44f, 0.62f, 1f);
        colors.pressedColor = new Color(0.1f, 0.24f, 0.38f, 1f);
        colors.disabledColor = new Color(0.2f, 0.22f, 0.25f, 0.8f);
        button.colors = colors;

        TextMeshProUGUI buttonText = CreateText(
            rect,
            "Label",
            label,
            18,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            TextAlignmentOptions.Center);
        RectTransform labelRect = buttonText.rectTransform;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
        return button;
    }

    private static TextMeshProUGUI CreateText(
        RectTransform parent,
        string objectName,
        string text,
        int fontSize,
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        RectTransform rect = CreateRect(parent, objectName, position, size, anchorMin, anchorMax);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateRect(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(
            (anchorMin.x + anchorMax.x) * 0.5f,
            (anchorMin.y + anchorMax.y) * 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void PositionPanel(Transform panel, Vector2 position, Vector2 anchor)
    {
        RectTransform rect = (RectTransform)panel;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
    }
}
