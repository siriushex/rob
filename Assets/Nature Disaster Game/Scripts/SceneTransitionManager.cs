using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Events;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Настройки переходов")]
    [SerializeField] private Color transitionColor = Color.black;
    [SerializeField] private float defaultFadeDuration = 1.0f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool startWithFadedScreen = true;
    
    // UI элементы
    private Canvas canvas;
    private CanvasGroup fadeGroup;
    private Image fadeImage;
    
    // Состояние
    private bool isTransitioning = false;
    private Coroutine activeTransition = null;

    private void Awake()
    {
        // Настройка синглтона
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupTransitionUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Если требуется, запускаем экран с затемнения
        if (startWithFadedScreen)
        {
            fadeGroup.alpha = 1f;
            FadeOut(defaultFadeDuration);
        }
    }

    private void SetupTransitionUI()
    {
        // Создаём Canvas, если его нет
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        
        // Настраиваем Canvas для работы в оверлее
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // Максимальный порядок сортировки
        
        // Добавляем CanvasScaler для правильной работы с разными разрешениями
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Создаём объект для панели затемнения
        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(transform, false);
        
        // Настраиваем RectTransform для панели
        RectTransform panelRect = fadePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero; // Растягиваем на весь экран
        panelRect.localPosition = Vector3.zero;
        
        // Создаём Image для затемнения
        fadeImage = fadePanel.AddComponent<Image>();
        fadeImage.color = transitionColor;
        
        // Добавляем CanvasGroup для контроля прозрачности
        fadeGroup = fadePanel.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0;
        fadeGroup.blocksRaycasts = false;
        
        // Устанавливаем высокий приоритет при блокировке взаимодействия
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null) gameObject.AddComponent<GraphicRaycaster>();
    }

    #region Публичные методы переходов

    /// <summary>
    /// Переход к новой сцене с задержкой
    /// </summary>
    /// <param name="sceneName">Имя загружаемой сцены</param>
    /// <param name="delay">Задержка перед началом перехода (сек)</param>
    /// <param name="fadeDuration">Длительность затемнения (сек)</param>
    /// <param name="onTransitionComplete">Событие после завершения перехода</param>
    public void TransitionToScene(string sceneName, float delay = 0, float fadeDuration = -1, UnityAction onTransitionComplete = null)
    {
        if (isTransitioning) return;
        
        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        StartTransitionProcess(() => StartCoroutine(TransitionToSceneRoutine(sceneName, delay, fadeDuration, onTransitionComplete)));
    }

    /// <summary>
    /// Переход к новой сцене по индексу с задержкой
    /// </summary>
    /// <param name="sceneIndex">Индекс загружаемой сцены</param>
    /// <param name="delay">Задержка перед началом перехода (сек)</param>
    /// <param name="fadeDuration">Длительность затемнения (сек)</param>
    /// <param name="onTransitionComplete">Событие после завершения перехода</param>
    public void TransitionToScene(int sceneIndex, float delay = 0, float fadeDuration = -1, UnityAction onTransitionComplete = null)
    {
        if (isTransitioning) return;
        
        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;
        StartTransitionProcess(() => StartCoroutine(TransitionToSceneRoutine(sceneIndex, delay, fadeDuration, onTransitionComplete)));
    }

    /// <summary>
    /// Показать эффект затемнения без смены сцены
    /// </summary>
    /// <param name="duration">Длительность затемнения (сек)</param>
    /// <param name="delay">Задержка перед началом (сек)</param>
    /// <param name="onComplete">Событие после завершения</param>
    public void FadeIn(float duration = -1, float delay = 0, UnityAction onComplete = null)
    {
        if (isTransitioning) return;
        
        if (duration < 0) duration = defaultFadeDuration;
        StartTransitionProcess(() => StartCoroutine(FadeInRoutine(duration, delay, onComplete)));
    }

    /// <summary>
    /// Убрать эффект затемнения
    /// </summary>
    /// <param name="duration">Длительность (сек)</param>
    /// <param name="delay">Задержка перед началом (сек)</param>
    /// <param name="onComplete">Событие после завершения</param>
    public void FadeOut(float duration = -1, float delay = 0, UnityAction onComplete = null)
    {
        if (isTransitioning) return;
        
        if (duration < 0) duration = defaultFadeDuration;
        StartTransitionProcess(() => StartCoroutine(FadeOutRoutine(duration, delay, onComplete)));
    }

    #endregion

    #region Корутины переходов

    private IEnumerator TransitionToSceneRoutine(string sceneName, float delay, float fadeDuration, UnityAction onComplete)
    {
        // Задержка перед началом перехода
        if (delay > 0) yield return new WaitForSeconds(delay);
        
        // Затемнение экрана
        yield return FadeInRoutine(fadeDuration, 0, null);
        
        // Загрузка сцены
        SceneManager.LoadScene(sceneName);
        
        // Короткая пауза для загрузки сцены
        yield return new WaitForSeconds(0.1f);
        
        // Плавное осветление (через некоторое время после загрузки новой сцены)
        yield return FadeOutRoutine(fadeDuration, 0, onComplete);
        
        CompleteTransition();
    }

    private IEnumerator TransitionToSceneRoutine(int sceneIndex, float delay, float fadeDuration, UnityAction onComplete)
    {
        // Задержка перед началом перехода
        if (delay > 0) yield return new WaitForSeconds(delay);
        
        // Затемнение экрана
        yield return FadeInRoutine(fadeDuration, 0, null);
        
        // Загрузка сцены
        SceneManager.LoadScene(sceneIndex);
        
        // Короткая пауза для загрузки сцены
        yield return new WaitForSeconds(0.1f);
        
        // Плавное осветление
        yield return FadeOutRoutine(fadeDuration, 0, onComplete);
        
        CompleteTransition();
    }

    private IEnumerator FadeInRoutine(float duration, float delay, UnityAction onComplete)
    {
        // Задержка перед началом
        if (delay > 0) yield return new WaitForSeconds(delay);
        
        // Активируем блокировку взаимодействия во время затемнения
        fadeGroup.blocksRaycasts = true;
        
        float timer = 0;
        float startAlpha = fadeGroup.alpha;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float evaluatedT = fadeCurve.Evaluate(t);
            
            // Плавно меняем прозрачность от текущей до полностью непрозрачной
            fadeGroup.alpha = Mathf.Lerp(startAlpha, 1f, evaluatedT);
            
            yield return null;
        }
        
        fadeGroup.alpha = 1f;
        
        // Вызываем колбэк завершения, если он указан
        if (onComplete != null) onComplete();
        
        CompleteTransition();
    }

    private IEnumerator FadeOutRoutine(float duration, float delay, UnityAction onComplete)
    {
        // Задержка перед началом
        if (delay > 0) yield return new WaitForSeconds(delay);
        
        float timer = 0;
        float startAlpha = fadeGroup.alpha;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float evaluatedT = fadeCurve.Evaluate(t);
            
            // Плавно меняем прозрачность от текущей до полностью прозрачной
            fadeGroup.alpha = Mathf.Lerp(startAlpha, 0f, evaluatedT);
            
            yield return null;
        }
        
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false; // Отключаем блокировку взаимодействия
        
        // Вызываем колбэк завершения, если он указан
        if (onComplete != null) onComplete();
        
        CompleteTransition();
    }

    #endregion

    #region Вспомогательные методы

    private void StartTransitionProcess(System.Func<Coroutine> startAction)
    {
        // Если уже идёт переход, останавливаем его
        if (isTransitioning && activeTransition != null)
        {
            StopCoroutine(activeTransition);
        }
        
        isTransitioning = true;
        activeTransition = startAction();
    }

    private void CompleteTransition()
    {
        isTransitioning = false;
        activeTransition = null;
    }

    #endregion
}