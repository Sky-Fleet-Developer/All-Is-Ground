using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputEvents : Singleton<InputEvents>
{
    public List<Axes> ReInput;
    [System.Serializable]
    public class Axes
    {
        public string Name;
        public UnityEvent AxesDown;
        public Action axesDown;
        public UnityEvent AxesStay;
        public Action<float> axesStay;
        public UnityEvent AxesUp;
        public Action axesUp;

        public Axes(string name)
        {
            Name = name;
            AxesDown = new UnityEvent();
            AxesStay = new UnityEvent();
            AxesUp = new UnityEvent();
        }
    }

    void Update()
    {
        foreach (Axes Hit in ReInput)
        {
            if (Input.GetButtonDown(Hit.Name))
            {
                Hit.AxesDown.Invoke();
                Hit.axesDown?.Invoke();
            }

            if (Input.GetButton(Hit.Name))
            {
                float value = Input.GetAxis(Hit.Name);
                Hit.AxesStay.Invoke();
                Hit.axesStay?.Invoke(value);
            }

            if (Input.GetButtonUp(Hit.Name))
            {
                Hit.AxesUp.Invoke();
                Hit.axesUp?.Invoke();
            }
        }
    }

    public UnityEvent OnButtonDown(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.AxesDown;
        }
        var New = new Axes(Name);
        ReInput.Add(New);
        return New.AxesDown;
    }
    public UnityEvent OnButton(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.AxesStay;
        }
        var New = new Axes(Name);
        ReInput.Add(New);
        return New.AxesStay;
    }
    public UnityEvent OnButtonUp(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.AxesUp;
        }
        var New = new Axes(Name);
        ReInput.Add(New);
        return New.AxesUp;
    }
    public Action onButtonDown(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.axesDown;
        }
        var New = new Axes(Name);
        ReInput.Add(New);
        return New.axesDown;
    }
    public Action<float> onButton(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.axesStay;
        }
        var New = new Axes(Name);
        ReInput.Add(New);
        return New.axesStay;
    }
    public Action onButtonUp(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.axesUp;
        }
        var New = new Axes(Name);
        ReInput.Add(New);
        return New.axesUp;
    }
}
