using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidRainController : MonoBehaviour
{
    [SerializeField] private GameObject rainParticle;
    public void StartAcidRain()
    {
        rainParticle.SetActive(true);
        FindObjectOfType<AcidRainReceiver>().EnableAcidRain();
        
    }

    public void StopAcidRain()
    {
        rainParticle.SetActive(false);
        FindObjectOfType<AcidRainReceiver>().DisableAcidRain();
    }
}
