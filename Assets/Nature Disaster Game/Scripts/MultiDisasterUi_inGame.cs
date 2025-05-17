using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class MultiDisasterUi_inGame : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI disasterNumber_text;
    public void Initialize(int disasterNumber)
    {
        gameObject.SetActive(true);
        disasterNumber_text.text = disasterNumber.ToString();
        Invoke("Hide", 4f);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
