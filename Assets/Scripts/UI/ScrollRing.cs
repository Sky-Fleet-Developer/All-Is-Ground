using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace UnityEngine.UI
{
    public class ScrollRing : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        bool IsPressed;
        public float ScrollSpeed = 1;
        int value;
        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                SetValue(value);
            }
        }
        public int Minimum = 0;
        public int Maximum = 11;
        public float AnglePerFrame = 30;
        public UnityEvent OnValueChainge;

        RectTransform Tr;

        void SetValue(int val)
        {
            StopAllCoroutines();
            StartCoroutine(ScrollTo(val));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
            StopAllCoroutines();
            StartCoroutine(Scroll());
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
        }

        IEnumerator Scroll()
        {
            float startValue = value;
            Vector2 startMousePose = Input.mousePosition;
            float scrolling = value;
            while (Application.isPlaying && IsPressed)
            {
                yield return new WaitForSeconds(Time.fixedDeltaTime);
                scrolling = Mathf.Clamp(startValue - (Input.mousePosition.x - startMousePose.x) * ScrollSpeed / Screen.width, Minimum, Maximum);
                Tr.localEulerAngles = new Vector3(0, 0, scrolling * AnglePerFrame);
            }

            float rVel = 0f;
            value = Mathf.CeilToInt(scrolling - 0.5f);
            if (startValue != value)
                OnValueChainge.Invoke();
            while (Application.isPlaying && !scrolling.AlmostEquals(value, 0.01f))
            {
                yield return new WaitForSeconds(Time.fixedDeltaTime);
                rVel += (value - scrolling) * Time.fixedDeltaTime * 8;
                scrolling += rVel;
                rVel *= 0.02f;
                Tr.localEulerAngles = new Vector3(0, 0, scrolling * AnglePerFrame);
            }
        }

        IEnumerator ScrollTo(int val)
        {
            float startValue = value;
            float scrolling = value;
            float rVel = 0f;
            value = val;
            if (startValue != value)
                OnValueChainge.Invoke();
            while (Application.isPlaying && !scrolling.AlmostEquals(value, 0.01f))
            {
                yield return new WaitForSeconds(Time.fixedDeltaTime);
                rVel += (value - scrolling) * Time.fixedDeltaTime * 8;
                scrolling += rVel;
                rVel *= 0.02f;
                Tr.localEulerAngles = new Vector3(0, 0, scrolling * AnglePerFrame);
            }
        }

        void Awake()
        {
            Tr = transform as RectTransform;
        }

    }
}