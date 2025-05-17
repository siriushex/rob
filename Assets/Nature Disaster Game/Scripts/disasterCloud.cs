using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class disasterCloud : MonoBehaviour
{
    [SerializeField] private GameObject cloudObj;
     private Animator anim;

    [Header("Fog Settings")]
    public float targetDensity = 0.05f;
    public float duration = 3f;

    private void OnEnable()
    {
        GameController.onCloud += CloudIsComing;
        GameController.onFog += GameController_onFog;
    }

    private void GameController_onFog()
    {
        StartCoroutine(FadeInFog());
    }

    private void OnDisable()
    {
    GameController.onCloud -= CloudIsComing;
        GameController.onFog -= GameController_onFog;
    }

 

    public IEnumerator FadeInFog()
    {
        RenderSettings.fog = true;

        float startDensity = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            RenderSettings.fogDensity = Mathf.Lerp(startDensity, targetDensity, t);
            yield return null;
        }

        RenderSettings.fogDensity = targetDensity;
    }

    public IEnumerator FadeOutFog()
    {
        float startDensity = RenderSettings.fogDensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            RenderSettings.fogDensity = Mathf.Lerp(startDensity, 0f, t);
            yield return null;
        }

        RenderSettings.fogDensity = 0f;
        RenderSettings.fog = false;
    }
    private void Start()
    {
        anim = cloudObj.GetComponent<Animator>();
    }
    public void CloudIsComing()
    {
        cloudObj.SetActive(true);
        anim.Play("CloudCome");
    }
}

