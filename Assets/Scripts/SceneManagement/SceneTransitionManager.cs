using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using TMPro;

public enum GameScene
{
    INIT,
    MENU,
    GAME,
    BATTLESCENE,
    MAP
}

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Visual Timing Settings")]
    [SerializeField] private float typewriterSpeed = 0.15f;       
    [SerializeField] private float textHoldDuration = 1.0f;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("UI Overlay Settings")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private TextMeshProUGUI loadingTextUI;

    private bool _isTransitioning = false;
    private string _targetSceneToLoad;
    private string _sceneToLoadAfterSingle;
    private string _sceneToUnload;
    private string _returnSceneName;
    private LoadSceneMode _targetLoadMode = LoadSceneMode.Single;
    private Action _onTargetSceneUnloaded;
    private Action _onSceneLoaded;
    private Action _onFadeOutComplete;

    private bool _canFadeOut = false;
    private bool _fadeOutRequested = false;
    private Coroutine _typewriterCoroutine;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void InitializeManager()
    {
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.gameObject.SetActive(false);
        }
        if (loadingTextUI != null) loadingTextUI.text = "";
    }

    #region Public API

    public void LoadSceneLocal(GameScene scene, string message = "", Action onLoaded = null)
    {
        LoadSceneLocal(scene.ToString(), message, onLoaded);
    }

    public bool LoadSceneLocalWithAdditiveScene(
        GameScene scene,
        GameScene additiveScene,
        string message = "",
        Action onLoaded = null)
    {
        string sceneName = scene.ToString();
        string additiveSceneName = additiveScene.ToString();

        if (_isTransitioning)
        {
            Debug.LogWarning("Cannot load scenes while another scene transition is in progress.");
            return false;
        }
        if (scene == additiveScene || SceneManager.GetSceneByName(additiveSceneName).isLoaded)
        {
            Debug.LogError($"Cannot load '{sceneName}' together with '{additiveSceneName}'.");
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not enabled in Build Settings.");
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(additiveSceneName))
        {
            Debug.LogError($"Scene '{additiveSceneName}' is not enabled in Build Settings.");
            return false;
        }

        StartVisualTransition(
            sceneName,
            message,
            onLoaded: onLoaded,
            sceneToLoadAfterSingle: additiveSceneName);
        return true;
    }

    public bool LoadSceneAdditive(GameScene scene, string message = "")
    {
        string sceneName = scene.ToString();
        if (_isTransitioning)
        {
            Debug.LogWarning("Cannot load a scene while another scene transition is in progress.");
            return false;
        }
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"Scene '{sceneName}' is already loaded.");
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not enabled in Build Settings.");
            return false;
        }

        StartVisualTransition(sceneName, message, LoadSceneMode.Additive);
        return true;
    }

    public bool UnloadSceneAdditive(
        GameScene scene,
        GameScene returnToScene,
        Action onUnloaded = null,
        string message = "")
    {
        string sceneName = scene.ToString();
        string returnSceneName = returnToScene.ToString();

        if (_isTransitioning)
        {
            Debug.LogWarning("Cannot unload a scene while another scene transition is in progress.");
            return false;
        }

        Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
        Scene returnScene = SceneManager.GetSceneByName(returnSceneName);
        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded)
        {
            Debug.LogError($"Cannot unload '{sceneName}' because it is not loaded.");
            return false;
        }
        if (sceneName == returnSceneName || !returnScene.IsValid() || !returnScene.isLoaded)
        {
            Debug.LogError($"Cannot return to '{returnSceneName}' while unloading '{sceneName}'.");
            return false;
        }

        _sceneToUnload = sceneName;
        _returnSceneName = returnSceneName;
        _onTargetSceneUnloaded = onUnloaded;
        _isTransitioning = true;

        if (loadingTextUI != null) loadingTextUI.text = "";

        FadeInOverlay(() =>
        {
            ShowLoadingMessage(message, null);
            StartCoroutine(UnloadSceneAsyncCoroutine());
        });
        return true;
    }

    public bool ReplaceSceneAdditive(
        GameScene currentScene,
        GameScene nextScene,
        Action onReplaced = null,
        string message = "")
    {
        string currentSceneName = currentScene.ToString();
        string nextSceneName = nextScene.ToString();

        if (_isTransitioning)
        {
            Debug.LogWarning("Cannot replace a scene while another scene transition is in progress.");
            return false;
        }

        Scene current = SceneManager.GetSceneByName(currentSceneName);
        if (currentScene == nextScene ||
            !current.IsValid() ||
            !current.isLoaded ||
            SceneManager.GetSceneByName(nextSceneName).isLoaded)
        {
            Debug.LogError($"Cannot replace scene '{currentSceneName}' with '{nextSceneName}'.");
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"Scene '{nextSceneName}' is not enabled in Build Settings.");
            return false;
        }

        StartVisualTransition(
            nextSceneName,
            message,
            LoadSceneMode.Additive,
            sceneToUnload: currentSceneName,
            returnSceneName: nextSceneName,
            onTargetSceneUnloaded: onReplaced);
        return true;
    }

    public void PlayTransitionWithoutSceneChange(string message = "", Action onMidTransition = null)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        if (loadingTextUI != null) loadingTextUI.text = "";

        FadeInOverlay(() =>
        {
            ShowLoadingMessage(message, () =>
            {
                onMidTransition?.Invoke();
            });
        });
    }

    public void SnapToBlackImmediate(string message = "")
    {
        if (_isTransitioning) return;
        SnapToBlack(message);
    }

    public void ForceFadeOut()
    {
        if (!_isTransitioning) return;
        FadeOut();
    }

    #endregion

    #region Private Scene Loading Logic

    private void LoadSceneLocal(string sceneName, string message = "", Action onLoaded = null)
    {
        if (_isTransitioning) return;

        if (loadingTextUI != null) loadingTextUI.text = "";

        StartVisualTransition(sceneName, message, onLoaded: onLoaded);
    }

    private void StartVisualTransition(
        string sceneName,
        string message,
        LoadSceneMode loadMode = LoadSceneMode.Single,
        Action onLoaded = null,
        string sceneToLoadAfterSingle = null,
        string sceneToUnload = null,
        string returnSceneName = null,
        Action onTargetSceneUnloaded = null)
    {
        _isTransitioning = true;
        _targetSceneToLoad = sceneName;
        _sceneToLoadAfterSingle = sceneToLoadAfterSingle;
        _sceneToUnload = sceneToUnload;
        _returnSceneName = returnSceneName;
        _onTargetSceneUnloaded = onTargetSceneUnloaded;
        _targetLoadMode = loadMode;
        _onSceneLoaded = onLoaded;

        FadeInOverlay(() =>
        {
            ShowLoadingMessage(message, null);
            OnFadeInComplete(sceneName);
        });
    }

    private void OnFadeInComplete(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine());
    }

    private IEnumerator LoadSceneAsyncCoroutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_targetSceneToLoad, _targetLoadMode);
        if (asyncLoad == null)
        {
            throw new InvalidOperationException($"Failed to start loading scene '{_targetSceneToLoad}'.");
        }

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isTransitioning ||
            mode != _targetLoadMode ||
            !string.Equals(scene.name, _targetSceneToLoad, StringComparison.Ordinal))
        {
            return;
        }

        if (mode == LoadSceneMode.Additive && !SceneManager.SetActiveScene(scene))
        {
            throw new InvalidOperationException($"Failed to activate additive scene '{scene.name}'.");
        }

        if (mode == LoadSceneMode.Additive && !string.IsNullOrEmpty(_sceneToUnload))
        {
            StartCoroutine(UnloadSceneAsyncCoroutine());
            return;
        }

        if (mode == LoadSceneMode.Single && !string.IsNullOrEmpty(_sceneToLoadAfterSingle))
        {
            string additiveSceneName = _sceneToLoadAfterSingle;
            _sceneToLoadAfterSingle = null;
            _targetSceneToLoad = additiveSceneName;
            _targetLoadMode = LoadSceneMode.Additive;
            StartCoroutine(LoadChainedAdditiveSceneCoroutine(additiveSceneName));
            return;
        }

        Action onLoaded = _onSceneLoaded;
        _onSceneLoaded = null;
        FadeOut(onLoaded);
    }

    private IEnumerator LoadChainedAdditiveSceneCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to start loading scene '{sceneName}'.");
            _onSceneLoaded = null;
            FadeOut();
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator UnloadSceneAsyncCoroutine()
    {
        Scene returnScene = SceneManager.GetSceneByName(_returnSceneName);
        if (!returnScene.IsValid() || !returnScene.isLoaded)
        {
            Debug.LogError($"Cannot return to '{_returnSceneName}' because it is not loaded.");
            _onTargetSceneUnloaded = null;
            FadeOut();
            yield break;
        }
        if (SceneManager.GetActiveScene() != returnScene && !SceneManager.SetActiveScene(returnScene))
        {
            Debug.LogError($"Failed to activate return scene '{_returnSceneName}'.");
            _onTargetSceneUnloaded = null;
            FadeOut();
            yield break;
        }

        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(_sceneToUnload);
        if (asyncUnload == null)
        {
            Debug.LogError($"Failed to start unloading scene '{_sceneToUnload}'.");
            _onTargetSceneUnloaded = null;
            FadeOut();
            yield break;
        }

        while (!asyncUnload.isDone)
        {
            yield return null;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (!_isTransitioning || !string.Equals(scene.name, _sceneToUnload, StringComparison.Ordinal))
        {
            return;
        }

        Action onUnloaded = _onTargetSceneUnloaded;
        _onTargetSceneUnloaded = null;
        onUnloaded?.Invoke();
        FadeOut();
    }

    private void SnapToBlack(string message = "")
    {
        _isTransitioning = true;

        ClearLoadingMessage();

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.DOKill();
            loadingCanvasGroup.gameObject.SetActive(true);
            loadingCanvasGroup.alpha = 1f;
        }

        if (loadingTextUI != null)
        {
            loadingTextUI.text = message;
            loadingTextUI.maxVisibleCharacters = message.Length;
            loadingTextUI.ForceMeshUpdate();
        }

        _canFadeOut = true;

        if (_fadeOutRequested)
        {
            FadeOut();
        }
    }

    private void FadeOut(Action onComplete = null)
    {
        if (onComplete != null)
        {
            _onFadeOutComplete += onComplete;
        }

        if (!_canFadeOut)
        {
            _fadeOutRequested = true;
            return;
        }

        _canFadeOut = false;
        _fadeOutRequested = false;

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.DOKill();
            loadingCanvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(CompleteFadeOut);
        }
        else
        {
            CompleteFadeOut();
        }
    }

    private void CompleteFadeOut()
    {
        if (loadingCanvasGroup != null) loadingCanvasGroup.gameObject.SetActive(false);

        _isTransitioning = false;
        Action callback = _onFadeOutComplete;
        _onFadeOutComplete = null;
        callback?.Invoke();
    }
    #endregion

    #region Animation and Typewriter Logic

    private void FadeInOverlay(Action onComplete)
    {
        ClearLoadingMessage();

        if (loadingCanvasGroup == null)
        {
            _canFadeOut = true;
            onComplete?.Invoke();
            return;
        }

        _canFadeOut = false;
        _fadeOutRequested = false;

        loadingCanvasGroup.DOKill();
        loadingCanvasGroup.gameObject.SetActive(true);
        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.DOFade(1f, fadeInDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }


    private void ShowLoadingMessage(string message, Action onComplete)
    {
        StopLoadingMessageCoroutine();

        if (string.IsNullOrEmpty(message) || loadingTextUI == null)
        {
            ClearLoadingMessage();

            DOVirtual.DelayedCall(textHoldDuration, () =>
            {
                _canFadeOut = true;
                onComplete?.Invoke();
                if (_fadeOutRequested)
                {
                    FadeOut();
                }
            }).SetUpdate(true);

            return;
        }

        loadingTextUI.text = message;
        loadingTextUI.maxVisibleCharacters = 0;
        loadingTextUI.ForceMeshUpdate();

        _typewriterCoroutine = StartCoroutine(TypewriterRoutine(message, onComplete));
    }

    private IEnumerator TypewriterRoutine(string message, Action onComplete)
    {
        yield return null;

        int totalCharacters = message.Length;

        for (int i = 1; i <= totalCharacters; i++)
        {
            loadingTextUI.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        if (textHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(textHoldDuration);
        }

        _canFadeOut = true;
        onComplete?.Invoke();

        if (_fadeOutRequested)
        {
            FadeOut();
        }
    }

    private void ClearLoadingMessage()
    {
        StopLoadingMessageCoroutine();

        if (loadingTextUI == null)
        {
            return;
        }

        loadingTextUI.text = "";
        loadingTextUI.maxVisibleCharacters = 0;
        loadingTextUI.ForceMeshUpdate();
    }

    private void StopLoadingMessageCoroutine()
    {
        if (_typewriterCoroutine == null)
        {
            return;
        }

        StopCoroutine(_typewriterCoroutine);
        _typewriterCoroutine = null;
    }

    #endregion
}