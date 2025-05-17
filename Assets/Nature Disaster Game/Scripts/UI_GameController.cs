using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameController : MonoBehaviour
{
    [Header("SurvivePlayer")]
    [SerializeField] private UIAnimator survivalPlayerPanel;
    [SerializeField] private Transform containerList;
    [SerializeField] private SingleSurviveUI_player surviveUI_Player;

    [Header("MapPreview")]
    [SerializeField] private MapPreview mapPreview;
   public void OpenSurvivePlayersList()
    {
        survivalPlayerPanel.Show();
        ClearSurvivePlayer();
        foreach(PlayerParametrs playerParametrs in GameController.Instance.alivePlayer_list)
        {
            if(playerParametrs.isDie == false)
            {
                if(playerParametrs.playerId > 0)
                {
                    playerParametrs.health = Random.Range(10, 100);
                }
                else
                {
                    if(Invector.vGameController.instance.currentController != null)
                    {
                        playerParametrs.health = Invector.vGameController.instance.currentController.currentHealth;
                    }
                    else
                    {
                        playerParametrs.health = Random.Range(20, 100);
                    }
                  
                }
                SingleSurviveUI_player survivePlayer = Instantiate(surviveUI_Player, containerList);
                survivePlayer.Init(playerParametrs);
            }
          
        }
    }

    public void CloseSurvivePlayersList()
    {
        survivalPlayerPanel.Hide();
        ClearSurvivePlayer();
    }


    public IEnumerator MapPreview(MapsData maps)
    {
        mapPreview.gameObject.GetComponent<UIAnimator>().Show();
        mapPreview.Init(maps);
        yield return new WaitForSeconds(3f);
        mapPreview.gameObject.SetActive(false);

    }

    void ClearSurvivePlayer()
    {
        foreach(Transform child in containerList)
        {
            Destroy(child.gameObject);
        }
    }
}
