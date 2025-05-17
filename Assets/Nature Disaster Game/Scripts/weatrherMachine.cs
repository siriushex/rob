using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class weatrherMachine : MonoBehaviour
{
    [SerializeField] private GameObject keyToStart;
    [SerializeField] private KeyCode key;
    [SerializeField] private GameObject machinePnale_ui;
    [SerializeField] private TextMeshProUGUI multiDisaster_text;

    public static System.Action addMultipleDisaster;

    [SerializeField] private GameObject smokeEffect;

    int disasterCount = 0;
    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Player")
        {
            keyToStart.SetActive(true);
            if(Input.GetKeyDown(key))
            {
                machinePnale_ui.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        keyToStart.SetActive(false);
    }

    public void AddDisaster()
    {
        disasterCount++;
        addMultipleDisaster?.Invoke();
        multiDisaster_text.text = disasterCount.ToString();

        if(disasterCount == 2)
        {
            smokeEffect.SetActive(true);
        }
    }
}
