using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmissionFading : MonoBehaviour
{
    public float FadeTime = 4;
    public string Parameter;
    public Color Lighten;
    public Color Darken;
    Renderer Rend;

    void Awake()
    {
        Rend = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        StartCoroutine(FadingRoutine());
    }


    IEnumerator FadingRoutine()
    {
        float value = 1f;
        while (value > 0)
        {
            value -= Time.deltaTime / FadeTime;
            Rend.material.SetColor(Parameter, Color.Lerp(Lighten, Darken, value));
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
