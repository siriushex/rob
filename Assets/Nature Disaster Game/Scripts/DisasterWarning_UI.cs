using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class DisasterWarning_UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI disasterDis;

   public void Init(Disaster cur_disaster)
    {
        disasterDis.text = cur_disaster.Discription;
        Invoke("StopShowWarning", 5f);
    }

    public void StopShowWarning()
    {
        Destroy(this.gameObject);
    }
}
