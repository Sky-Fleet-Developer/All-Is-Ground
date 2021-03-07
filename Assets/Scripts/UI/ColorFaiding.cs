using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorFaiding : MonoBehaviour
{
    public IEnumerator Faiding(UILink element, float timer, PoollingStringLine stringLine)
    {
        float t = timer;
        while (t > 0f)
        {
            if (element.Image)
            {
                element.Image.color = new Color(element.Image.color.r, element.Image.color.g, element.Image.color.b, element.Image.color.a - Time.fixedDeltaTime / timer);
            }
            if (element.Text)
            {
                element.Text.color = new Color(element.Text.color.r, element.Text.color.g, element.Text.color.b, element.Text.color.a - Time.fixedDeltaTime / timer);
            }
            t -= Time.fixedDeltaTime;
            if (t < -0f)
                stringLine.RemoveElement(element);
            yield return new WaitForFixedUpdate();
        }
    }
}
