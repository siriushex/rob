using System.Collections;
using UnityEngine;
using TMPro;
using System;
public class DisasterTimer : MonoBehaviour
{
    [SerializeField] private GameObject Timer;
    private TextMeshProUGUI timeText;

    private Coroutine timerCoroutine;

    public static DisasterTimer Instance;

    public Action DisasterEnd;

    private void Awake()
    {
            Instance = this;

        timeText = Timer.GetComponentInChildren<TextMeshProUGUI>();
        Timer.SetActive(false);
    }

    public void StartTime(int seconds, bool isPreDisaster)
    {

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        Timer.SetActive(true);
        timerCoroutine = StartCoroutine(Countdown(seconds, isPreDisaster));
    }
public void ResetAndHideTimer()
{
    // Останавливаем текущий отсчет, если он есть
    if (timerCoroutine != null)
    {
        StopCoroutine(timerCoroutine);
        timerCoroutine = null;
    }
    
    // Скрываем таймер
    Timer.SetActive(false);
}
    private IEnumerator Countdown(int seconds, bool isPreDisaster)
    {
        Debug.Log("CountdownStart");
        int timeLeft = seconds;

        while (timeLeft >= 0)
        {
            int minutes = timeLeft / 60;
            int secs = timeLeft % 60;

            timeText.text = $"{minutes:00}:{secs:00}";

            yield return new WaitForSeconds(1f);
            timeLeft--;
        }

        // ����� ������ ����������
        Timer.SetActive(false);

        if (isPreDisaster)
        {
            Debug.Log("��������������� ����� �����������. ���������� ��������!");
            StartCoroutine (GameController.Instance.GenerateDisaster());
        }
        else
        {
            Debug.Log("�������� ���������.");
            gameObject.SetActive(false);
            DisasterEnd.Invoke();
            // ����� ����� ������� ����� ��������
        }
    }
}
