using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UIHelpers/PoollingStringLine")]
public class PoollingStringLine : MonoBehaviour
{
    public GameObject Item;
    public int MassiveSize;
   // public bool AddImage;
    public float FaidingTime;
    public Vector2 ElementSize;
    public Font Font;
    public TextAnchor TextAnchor;
    public int FontSize;
    public FontStyle FontStyle;
    public Directions Direction;
    public static Dictionary<string, PoollingStringLine> Instances = new Dictionary<string, PoollingStringLine>();

    public enum Directions
    {
        Up = 0,
        Down = 1
    }

    public List<UILink> DisabledElements;
    public List<UILink> EnabledElements;

    Transform Tr;
    void Awake()
    {
        if (Instances == null)
            Instances = new Dictionary<string, PoollingStringLine>();
        if (Instances.ContainsKey(gameObject.name))
            Instances[gameObject.name] = this;
        else
            Instances.Add(gameObject.name, this);
        Tr = transform;
        GameObject Element = null;
        UILink UIElement = null;
        switch (Item == null)
        {
            case true:
                Element = new GameObject();
                Element.transform.parent = Tr;
                Element.transform.localPosition = Vector3.zero;
                Element.transform.localRotation = Quaternion.identity;
                Element.transform.localScale = Vector3.one;
                /*if (AddImage)
                    Element.AddComponent<Image>();*/
                Element.AddComponent<Text>().font = Font;
                Element.GetComponent<Text>().alignment = TextAnchor;
                Element.GetComponent<Text>().fontSize = FontSize;
                Element.GetComponent<Text>().fontStyle = FontStyle;
                Element.AddComponent<ColorFaiding>();
                Element.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
                Element.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
                Element.GetComponent<RectTransform>().sizeDelta = ElementSize;
                UIElement = Element.AddComponent<UILink>();
                break;
            case false:
                Element = Item;
                break;
        }


        for (int i = 0; i < MassiveSize; i++)
        {
            DisabledElements.Add(Instantiate(UIElement, Tr));
        }
        if(Item == null)
        Element.SetActive(false);
    }

    public void Write(string text, Color color)
    {
        UILink element = null;
        switch (DisabledElements.Count > 0)
        {
            case true:
                element = DisabledElements[0];
                DisabledElements.Remove(element);
                element.gameObject.SetActive(true);
                element.Text.text = text;
                element.Text.color = color;
                EnabledElements.Add(element);
                element.ColorFaiding.StartCoroutine(element.ColorFaiding.Faiding(element, FaidingTime, this));
                break;
            case false:
                element = EnabledElements[0];
                EnabledElements.Remove(element);
                element.ColorFaiding.StopAllCoroutines();
                element.Text.text = text;
                element.Text.color = color;
                EnabledElements.Add(element);
                element.ColorFaiding.StartCoroutine(element.ColorFaiding.Faiding(element, FaidingTime, this));
                break;
        }
        switch (Direction)
        {
            case Directions.Up:
                element.transform.SetSiblingIndex(0);
                break;
            case Directions.Down:
                int i = EnabledElements.Count;
                foreach (UILink Hit in EnabledElements)
                {
                    Hit.transform.SetSiblingIndex(i--);
                }
                break;
        }

    }

    public void RemoveElement(UILink element)
    {
        EnabledElements.Remove(element);
        element.gameObject.SetActive(false);
        DisabledElements.Add(element);
    }
}
