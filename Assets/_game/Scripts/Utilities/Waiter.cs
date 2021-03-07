using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Waiters
{
    public delegate T Getter<out T>();
    public enum UpdateModes
    {
        Update = 0,
        FixedUpdate = 1,
        Realtime = 2
    }
    public class Lerper
    {
        private MonoBehaviour owner;
        private Coroutine current;

        private Getter<float> getter;
        private Action<float> setter;
        private Action onComplete;
        private float duration;
        private float target;
        private UpdateModes mode;

        public Lerper(MonoBehaviour owner, Getter<float> getter, Action<float> setter, float duration, float target)
        {
            this.getter = getter;
            this.setter = setter;
            this.duration = duration;
            this.target = target;
            this.owner = owner;
            current = null;
            mode = UpdateModes.FixedUpdate;
            current = owner.StartCoroutine(Job());
        }

        private float GetDeltaTime()
        {
            switch (mode)
            {
                case UpdateModes.Update:
                    return Time.deltaTime;
                case UpdateModes.FixedUpdate:
                    return Time.fixedDeltaTime;
                case UpdateModes.Realtime:
                    return Time.fixedDeltaTime;
            }
            return 0;
        }

        private IEnumerator Job()
        {
            float value = getter();
            float t = 0;
            float T = Time.time;
            do
            {
                switch (mode)
                {
                    case UpdateModes.Update:
                        t = Mathf.MoveTowards(t, 1, GetDeltaTime() / duration);
                        yield return new WaitForSeconds(Time.deltaTime);
                        break;
                    case UpdateModes.FixedUpdate:
                        yield return new WaitForFixedUpdate();
                        t = Mathf.MoveTowards(t, 1, GetDeltaTime() / duration);
                        break;
                    case UpdateModes.Realtime:
                        t = Mathf.MoveTowards(t, 1, GetDeltaTime() / duration);
                        yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
                        break;
                }
                setter.Invoke(Mathf.Lerp(value, target, t));
            } while (t < 1);
            onComplete?.Invoke();
        }

        public Lerper SetUpdate(UpdateModes mode)
        {
            this.mode = mode;
            return this;
        }

        public Lerper OnComplete(Action action)
        {
            this.onComplete = action;
            return this;
        }

        public void Kill()
        {
            if (current != null && owner)
                owner.StopCoroutine(current);
        }
    }

    public class Waiter
    {
        private MonoBehaviour owner;
        private Coroutine current;
        public Waiter(MonoBehaviour owner, float delay, Action onConplete)
        {
            this.owner = owner;
            current = null;
            current = owner.StartCoroutine(WaitRoutine(delay, onConplete));
        }
        public Waiter(MonoBehaviour owner, Getter<bool> canContinue, Action onConplete)
        {
            this.owner = owner;
            current = null;
            current = owner.StartCoroutine(WaitForRoutine(canContinue, onConplete));
        }

        private IEnumerator WaitRoutine(float delay, Action onConplete)
        {
            yield return new WaitForSeconds(delay);
            onConplete?.Invoke();
        }
        private IEnumerator WaitForRoutine(Getter<bool> canContinue, Action onConplete)
        {
            while (canContinue() == false)
            {
                yield return new WaitForFixedUpdate();
            }
            onConplete?.Invoke();
        }
        public void Kill()
        {
            if (current != null && owner)
                owner.StopCoroutine(current);
        }
    }

    public static class MonoBehaviourExtensions
    {
        public static Lerper Lerp(this MonoBehaviour owner, Getter<float> getter, Action<float> setter, float targetValue, float duration)
        {
            return new Lerper(owner, getter, setter, duration, targetValue);
        }
        public static Waiter Wait(this MonoBehaviour owner, float delay, Action onConplete)
        {
            return new Waiter(owner, delay, onConplete);
        }
        public static Waiter WaitFor(this MonoBehaviour owner, Getter<bool> canContinue, Action onConplete)
        {
            return new Waiter(owner, canContinue, onConplete);
        }
    }
}
