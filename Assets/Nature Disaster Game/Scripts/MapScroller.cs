using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapScroller : MonoBehaviour
{
    public RectTransform content;        // Контейнер, где лежат карты
    public float spinSpeed = 800f;       // начальная скорость
    public float slowDownRate = 500f;    // как быстро замедляется
    public float stopThreshold = 100f;   // скорость, при которой останавливается

    private float currentSpeed;
    private bool isSpinning = false;
    private int chosenIndex;

   

    public void Start()
    {
        isSpinning = true;
        currentSpeed = spinSpeed;
        chosenIndex = Random.Range(0, content.childCount);
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        while (currentSpeed > stopThreshold)
        {
            content.anchoredPosition -= new Vector2(currentSpeed * Time.deltaTime, 0);
            currentSpeed -= slowDownRate * Time.deltaTime;

            // зацикливаем если ушло далеко влево
            if (content.anchoredPosition.x < -GetTotalWidth())
                content.anchoredPosition = Vector2.zero;

            yield return null;
        }

        // финальная прокрутка к выбранному элементу
        float finalX = -chosenIndex * GetElementWidth();
        StartCoroutine(SmoothSnapTo(finalX));
        isSpinning = false;
    }

    private IEnumerator SmoothSnapTo(float targetX)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector2 start = content.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            content.anchoredPosition = Vector2.Lerp(start, new Vector2(targetX, start.y), t);
            yield return null;
        }

        content.anchoredPosition = new Vector2(targetX, start.y);
        Debug.Log("Выбрана карта: " + chosenIndex);
    }

    private float GetElementWidth()
    {
        if (content.childCount == 0) return 100;
        return ((RectTransform)content.GetChild(0)).rect.width;
    }

    private float GetTotalWidth()
    {
        return GetElementWidth() * content.childCount;
    }
}
