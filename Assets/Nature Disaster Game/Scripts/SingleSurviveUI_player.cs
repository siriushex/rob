using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SingleSurviveUI_player : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private Image player_icon;
    [SerializeField] private Slider playerHealth;

    public void Init(PlayerParametrs parametrs)
    {
        playerName.text = parametrs.player_Name;
        player_icon.sprite = parametrs.player_Icon;
        playerHealth.value = parametrs.health;
    }
}
