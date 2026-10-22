using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using TMPro;

public enum GameScene
{
    INIT,
    MENU,
    GAME
}

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Visual Timing Settings")]
    [SerializeField] private float typewriterSpeed = 0.15f;       
    [SerializeField] private float textHoldDuration = 1.0f;
    [SerializeField] private float imagePopInDuration = 0.8f;      
    [SerializeField] private float imagePopInDelayStep = 0.25f;    
    [SerializeField] private float imagePopOutDuration = 0.6f;     
    [SerializeField] private float imagePopOutDelayStep = 0.15f;   

    [Header("UI Overlay Settings")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private TextMeshProUGUI loadingTextUI;

    [Header("Transition Images")]
    [SerializeField] private List<Transform> transitionImages = new List<Transform>();
    private List<Vector3> originalImageScales = new List<Vector3>();

    private bool _isTransitioning = false;
    private string _targetSceneToLoad;
    private Sequence _transitionSequence;

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
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeManager()
    {
        originalImageScales.Clear();
        foreach (var img in transitionImages)
        {
            if (img != null)
            {
                Vector3 targetScale = img.localScale == Vector3.zero ? Vector3.one : img.localScale;
                originalImageScales.Add(targetScale);

                img.localScale = Vector3.zero;
                img.gameObject.SetActive(false);
            }
            else
            {
                originalImageScales.Add(Vector3.one);
            }
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.gameObject.SetActive(false);
        }
        if (loadingTextUI != null) loadingTextUI.text = "";
    }

    #region Public API

    public void LoadSceneLocal(GameScene scene, string message = "") => LoadSceneLocal(scene.ToString(), message);

    public void PlayTransitionWithoutSceneChange(string message = "", Action onMidTransition = null)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        
        if (loadingTextUI != null) loadingTextUI.text = "";

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.gameObject.SetActive(true);
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.DOFade(1f, 0.4f).SetUpdate(true);
        }

        AnimateImagesIn(() =>
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

    private void LoadSceneLocal(string sceneName, string message = "")
    {
        if (_isTransitioning) return;
        
        if (loadingTextUI != null) loadingTextUI.text = "";

        StartVisualTransition(sceneName, message);
    }

    private void StartVisualTransition(string sceneName, string message)
    {
        _isTransitioning = true;
        _targetSceneToLoad = sceneName;

        string currentScene = SceneManager.GetActiveScene().name;
        bool isInitToLobby = currentScene.Equals(GameScene.INIT.ToString(), System.StringComparison.OrdinalIgnoreCase);

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.gameObject.SetActive(true);
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.DOFade(1f, 0.4f).SetUpdate(true);
        }

        AnimateImagesIn(() => 
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
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_targetSceneToLoad);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isTransitioning) return;
        FadeOut();
    }

    private void SnapToBlack(string message = "")
    {
        if (_transitionSequence != null) _transitionSequence.Kill();

        _isTransitioning = true;

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

        for (int i = 0; i < transitionImages.Count; i++)
        {
            Transform img = transitionImages[i];
            if (img != null)
            {
                img.DOKill();
                img.gameObject.SetActive(true);
                img.localScale = (i < originalImageScales.Count) ? originalImageScales[i] : Vector3.one; 
            }
        }

        _canFadeOut = true;

        if (_fadeOutRequested)
        {
            FadeOut();
        }
    }

    private void FadeOut()
    {
        if (!_canFadeOut)
        {
            _fadeOutRequested = true;
            return;
        }

        _canFadeOut = false;
        _fadeOutRequested = false;

        if (_transitionSequence != null) _transitionSequence.Kill();
        _transitionSequence = DOTween.Sequence().SetUpdate(true);

        int count = transitionImages.Count;

        for (int i = 0; i < count; i++)
        {
            Transform img = transitionImages[i];
            if (img != null)
            {
                img.DOKill();
                
                int reverseIndex = (count - 1) - i;
                float delay = reverseIndex * imagePopOutDelayStep;

                _transitionSequence.Insert(delay, img.DOScale(Vector3.zero, imagePopOutDuration).SetEase(Ease.InBack));
            }
        }

        if (loadingCanvasGroup != null)
        {
            _transitionSequence.Insert(0f, loadingCanvasGroup.DOFade(0f, imagePopOutDuration).SetEase(Ease.Linear));
        }

        _transitionSequence.OnComplete(() =>
        {
            if (loadingCanvasGroup != null) loadingCanvasGroup.gameObject.SetActive(false);
            
            foreach (var img in transitionImages)
            {
                if (img != null) img.gameObject.SetActive(false);
            }

            _isTransitioning = false;
        });
    }
    #endregion

    #region Animation and Typewriter Logic

    private void AnimateImagesIn(Action onComplete)
    {
        if (transitionImages == null || transitionImages.Count == 0)
        {
            _canFadeOut = true;
            onComplete?.Invoke();
            return;
        }

        _canFadeOut = false;
        _fadeOutRequested = false;

        float maxAnimationTime = 0f;

        for (int i = 0; i < transitionImages.Count; i++)
        {
            Transform img = transitionImages[i];
            if (img != null)
            {
                img.gameObject.SetActive(true);
                img.localScale = Vector3.zero;
                
                img.DOKill();
                float delay = i * imagePopInDelayStep;
                float duration = imagePopInDuration;
                
                maxAnimationTime = Mathf.Max(maxAnimationTime, delay + duration);


                img.DOScale(originalImageScales[i], duration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(delay)
                    .SetUpdate(true);
            }
        }

        DOVirtual.DelayedCall(maxAnimationTime, () => onComplete?.Invoke()).SetUpdate(true);
    }


    private void ShowLoadingMessage(string message, Action onComplete)
    {
        if (string.IsNullOrEmpty(message) || loadingTextUI == null)
        {
            if (loadingTextUI != null) loadingTextUI.text = "";

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

        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
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

    #endregion
}