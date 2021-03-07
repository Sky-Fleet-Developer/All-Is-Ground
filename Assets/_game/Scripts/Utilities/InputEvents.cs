using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputEvents : MonoBehaviour
{
    public static InputEvents Instance;
    public List<Axes> ReInput;
    [System.Serializable]
    public class Axes
    {
        public string Name;
        public UnityEvent AxesDown;
        public UnityEvent AxesStay;
        public UnityEvent AxesUp;
    }

    public void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        foreach (Axes Hit in ReInput)
        {
            if (Input.GetButtonDown(Hit.Name))
                Hit.AxesDown.Invoke();

            if (Input.GetButton(Hit.Name))
                Hit.AxesStay.Invoke();

            if (Input.GetButtonUp(Hit.Name))
                Hit.AxesUp.Invoke();
        }
    }

    public UnityEvent OnButtonDown(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.AxesDown;
        }
        return null;
    }
    public UnityEvent OnButton(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.AxesStay;
        }
        return null;
    }
    public UnityEvent OnButtonUp(string Name)
    {
        foreach (Axes Hit in ReInput)
        {
            if (Hit.Name == Name)
                return Hit.AxesUp;
        }
        return null;
    }
}
