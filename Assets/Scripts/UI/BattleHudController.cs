using System;
using System.Collections.Generic;
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
    private TextMeshProUGUI _programPreviewText;
    private Button _endTurnButton;
    private Button _clearProgramButton;
    private Button _pauseButton;
    private readonly List<Button> _basicCommandButtons = new List<Button>();
    private readonly List<ProgramSlotView> _programSlots = new List<ProgramSlotView>();
    private readonly HashSet<string> _queuedCardIds = new HashSet<string>();
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
        _programPreviewText.text = _fightManager.GetProgramPreview();
        SyncQueuedCards();

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

        bool canProgram =
            !_isPaused &&
            !_fightManager.IsFightOver &&
            !_fightManager.IsPresentationPending &&
            _fightManager.IsTurnStarted &&
            ReferenceEquals(_fightManager.CurrentActor, _fightManager.PlayerState);
        _endTurnButton.interactable = canProgram;
        _clearProgramButton.interactable = canProgram && _fightManager.Program.Count > 0;
        for (int index = 0; index < _basicCommandButtons.Count; index++)
        {
            _basicCommandButtons[index].interactable =
                canProgram && _fightManager.Program.Count < _fightManager.ProgrammingCapacity;
        }
        for (int index = 0; index < _programSlots.Count; index++)
        {
            ProgramSlotView slot = _programSlots[index];
            bool occupied = index < _fightManager.Program.Count;
            slot.Label.text = occupied
                ? $"{index + 1}. {_fightManager.Program[index].GetDisplayName()}"
                : $"{index + 1}. -";
            slot.LeftButton.interactable = canProgram && occupied && index > 0;
            slot.RightButton.interactable =
                canProgram && occupied && index < _fightManager.Program.Count - 1;
            slot.RemoveButton.interactable = canProgram && occupied;
        }
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
            "Zatwierdź program",
            new Vector2(-168f, -13f),
            new Vector2(220f, 56f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            OnCommitProgramClicked);

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

        BuildProgrammingControls(hudRoot);
        BuildPauseMenu(hudRoot);
    }

    private void BuildProgrammingControls(RectTransform hudRoot)
    {
        RectTransform programRoot = CreateRect(
            hudRoot,
            "Program Slots",
            new Vector2(0f, 360f),
            new Vector2(930f, 126f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f));
        Image programBackground = programRoot.gameObject.AddComponent<Image>();
        programBackground.color = new Color(0.04f, 0.06f, 0.09f, 0.82f);
        programBackground.raycastTarget = false;

        _programPreviewText = CreateText(
            programRoot,
            "Program Preview",
            string.Empty,
            15,
            new Vector2(0f, -8f),
            new Vector2(900f, 28f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            TextAlignmentOptions.Center);

        int slotCount = _fightManager.ProgrammingCapacity;
        float slotWidth = Mathf.Min(210f, 900f / slotCount);
        float controlSize = Mathf.Min(48f, slotWidth * 0.25f);
        float firstSlotX = -slotWidth * (slotCount - 1) * 0.5f;
        for (int index = 0; index < slotCount; index++)
        {
            int slotIndex = index;
            RectTransform slotRoot = CreateRect(
                programRoot,
                $"Program Slot {index + 1}",
                new Vector2(firstSlotX + index * slotWidth, -40f),
                new Vector2(slotWidth - 8f, 72f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));
            Image slotBackground = slotRoot.gameObject.AddComponent<Image>();
            slotBackground.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);
            slotBackground.raycastTarget = false;

            TextMeshProUGUI label = CreateText(
                slotRoot,
                "Action",
                $"{index + 1}. —",
                16,
                new Vector2(0f, 12f),
                new Vector2(slotWidth - 16f, 26f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                TextAlignmentOptions.Center);
            Button leftButton = CreateButton(
                slotRoot,
                "Move Left",
                "<",
                new Vector2(-controlSize - 2f, -18f),
                new Vector2(controlSize, 28f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                () => MoveProgramAction(slotIndex, -1));
            Button rightButton = CreateButton(
                slotRoot,
                "Move Right",
                ">",
                new Vector2(0f, -18f),
                new Vector2(controlSize, 28f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                () => MoveProgramAction(slotIndex, 1));
            Button removeButton = CreateButton(
                slotRoot,
                "Remove Action",
                "X",
                new Vector2(controlSize + 2f, -18f),
                new Vector2(controlSize, 28f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                () => RemoveProgramAction(slotIndex));

            _programSlots.Add(new ProgramSlotView(label, leftButton, rightButton, removeButton));
        }

        HexCommandType[] basicCommands =
        {
            HexCommandType.MoveForward,
            HexCommandType.MoveBackward,
            HexCommandType.TurnLeft,
            HexCommandType.TurnRight,
            HexCommandType.Wait,
            HexCommandType.Fire
        };
        string[] labels = { "Forward", "Backward", "Turn Left", "Turn Right", "Wait", "Fire" };
        float buttonWidth = 132f;
        float firstButtonX = -buttonWidth * (basicCommands.Length - 1) * 0.5f;
        for (int index = 0; index < basicCommands.Length; index++)
        {
            HexCommandType command = basicCommands[index];
            Button button = CreateButton(
                hudRoot,
                $"Basic Command {command}",
                labels[index],
                new Vector2(firstButtonX + index * buttonWidth, 305f),
                new Vector2(buttonWidth - 6f, 46f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                () => QueueBasicCommand(command));
            _basicCommandButtons.Add(button);
        }

        _clearProgramButton = CreateButton(
            hudRoot,
            "Clear Program Button",
            "Clear",
            new Vector2(445f, 305f),
            new Vector2(110f, 46f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            ClearProgram);
    }

    private void QueueBasicCommand(HexCommandType command)
    {
        _fightManager.TryQueueBasicCommand(command);
        Refresh();
    }

    private void MoveProgramAction(int index, int offset)
    {
        _fightManager.MoveProgramAction(index, index + offset);
        Refresh();
    }

    private void RemoveProgramAction(int index)
    {
        _fightManager.RemoveProgramAction(index);
        Refresh();
    }

    private void ClearProgram()
    {
        _fightManager.ClearProgram();
        Refresh();
    }

    private void SyncQueuedCards()
    {
        HashSet<string> desiredCardIds = new HashSet<string>();
        foreach (ProgrammedAction action in _fightManager.Program)
        {
            if (action.IsCard)
            {
                desiredCardIds.Add(action.CardInstanceId);
            }
        }

        foreach (string cardId in new List<string>(_queuedCardIds))
        {
            if (!desiredCardIds.Contains(cardId))
            {
                _handCardAnimator.SetCardQueued(cardId, false);
                _queuedCardIds.Remove(cardId);
            }
        }

        foreach (string cardId in desiredCardIds)
        {
            if (_queuedCardIds.Add(cardId))
            {
                _handCardAnimator.SetCardQueued(cardId, true);
            }
        }
    }

    private sealed class ProgramSlotView
    {
        public TextMeshProUGUI Label { get; }
        public Button LeftButton { get; }
        public Button RightButton { get; }
        public Button RemoveButton { get; }

        public ProgramSlotView(
            TextMeshProUGUI label,
            Button leftButton,
            Button rightButton,
            Button removeButton)
        {
            Label = label;
            LeftButton = leftButton;
            RightButton = rightButton;
            RemoveButton = removeButton;
        }
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

    private void OnCommitProgramClicked()
    {
        _fightManager.CommitProgram();
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
