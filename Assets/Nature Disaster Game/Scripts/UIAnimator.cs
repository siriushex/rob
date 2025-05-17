using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using System;

[AddComponentMenu("UI/UI Animator")]
public class UIAnimator : MonoBehaviour
{
    [Header("Настройки анимации")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;
    [SerializeField] private float startScale = 0.5f;

    [Header("Дополнительные эффекты")]
    [SerializeField] private bool useFadeEffect = false;
    [SerializeField] private bool useScaleEffect = true;
    [SerializeField] private bool autoHideOnStart = false;

    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Sequence currentAnimation;

    private void Awake()
    {
        // Запоминаем оригинальный масштаб объекта
        originalScale = transform.localScale;

        // Ищем или добавляем CanvasGroup при необходимости
        if (useFadeEffect && !TryGetComponent(out canvasGroup))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // Автоматически скрываем объект при старте, если включена опция
        if (autoHideOnStart)
        {
            InstantHide();
        }
    }

    /// <summary>
    /// Показать элемент с анимацией увеличения
    /// </summary>
    /// <param name="onComplete">Действие после завершения анимации</param>
    public void Show(UnityAction onComplete = null)
    {
        // Остановить текущую анимацию, если она выполняется
        KillAnimation();

        gameObject.SetActive(true);

        // Создаем новую последовательность анимаций
        currentAnimation = DOTween.Sequence();

        if (useScaleEffect)
        {
            // Устанавливаем начальный масштаб
            transform.localScale = originalScale * startScale;

            // Анимация увеличения до оригинального размера
            currentAnimation.Join(transform.DOScale(originalScale, animationDuration).SetEase(easeType));
        }

        if (useFadeEffect && canvasGroup != null)
        {
            // Устанавливаем начальную прозрачность
            canvasGroup.alpha = 0;

            // Анимация проявления
            currentAnimation.Join(canvasGroup.DOFade(1, animationDuration).SetEase(Ease.OutQuad));
        }

        // Добавляем колбэк после завершения анимации
        if (onComplete != null)
        {
            currentAnimation.OnComplete(() => onComplete.Invoke());
        }
    }

    /// <summary>
    /// Скрыть элемент с анимацией уменьшения
    /// </summary>
    /// <param name="onComplete">Действие после завершения анимации</param>
    public void Hide(UnityAction onComplete = null)
    {
        // Остановить текущую анимацию, если она выполняется
        KillAnimation();

        // Создаем новую последовательность анимаций
        currentAnimation = DOTween.Sequence();

        if (useScaleEffect)
        {
            // Анимация уменьшения
            currentAnimation.Join(transform.DOScale(originalScale * startScale, animationDuration).SetEase(Ease.InQuad));
        }

        if (useFadeEffect && canvasGroup != null)
        {
            // Анимация исчезновения
            currentAnimation.Join(canvasGroup.DOFade(0, animationDuration).SetEase(Ease.InQuad));
        }

        // Добавляем действия после завершения анимации
        currentAnimation.OnComplete(() => {
            gameObject.SetActive(false);
            if (onComplete != null) onComplete.Invoke();
        });
    }

    /// <summary>
    /// Показать мгновенно, без анимации
    /// </summary>
    public void InstantShow()
    {
        KillAnimation();
        gameObject.SetActive(true);
        transform.localScale = originalScale;

        if (useFadeEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 1;
        }
    }

    /// <summary>
    /// Скрыть мгновенно, без анимации
    /// </summary>
    public void InstantHide()
    {
        KillAnimation();

        if (useScaleEffect)
        {
            transform.localScale = originalScale * startScale;
        }

        if (useFadeEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 0;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Переключение состояния (показать/скрыть)
    /// </summary>
    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// Остановить текущую анимацию
    /// </summary>
    private void KillAnimation()
    {
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }

    /// <summary>
    /// При уничтожении компонента останавливаем все анимации
    /// </summary>
    private void OnDestroy()
    {
        KillAnimation();
    }
}