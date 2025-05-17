using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class MapPreview : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI mapName;
   public void Init(MapsData mapsData)
    {
        icon.sprite = mapsData.icon;
        mapName.text = mapsData.name;
    }
}
