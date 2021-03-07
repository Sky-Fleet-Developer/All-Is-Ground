using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Waiters;

namespace Location
{
    public class DynamicPool : Singleton<DynamicPool>
    {
        private static List<(GameObject, Component)> pool;
        private static List<(GameObject, Component)> free;
        private static List<(GameObject, Component)> all;

        private void Awake()
        {
            pool = new List<(GameObject, Component)>();
            all = new List<(GameObject, Component)>();
            free = new List<(GameObject, Component)>();
        }

        public static (GameObject, Component) Find(GameObject prefab, bool remuve = false)
        {
            var inst = Instance;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Item1 == prefab)
                {
                    if (remuve)
                    {
                        var value = pool[i];
                        pool.RemoveRange(i, 1);
                        return value;
                    }
                    return pool[i];
                }
            }
            return (null, null);
        }

        public T Get<T>(T source) where T : Component
        {
            return Get<T>(source.gameObject);
        }

        public T Get<T>(GameObject prefab) where T : Component
        {
            var target = Find(prefab, true);
            if (target.Item1 == null)
            {
                var instance = Instantiate(prefab);
                instance.name = prefab.name + $"({all.Count(x => x.Item1 == prefab)})";
                target = (prefab, instance.GetComponent<T>());
                all.Add(target);
            }
            free.Add(target);
            target.Item2.gameObject.SetActive(true);
            target.Item2.transform.SetParent(null);
            return target.Item2 as T;
        }

        public void Return(Component component, float delay)
        {
            this.Wait(delay, () => Return(component));
        }

        public void Return(Component component)
        {
            for (int i = 0; i < free.Count; i++)
            {
                if (free[i].Item2 == component)
                {
                    if (!component) return;
                    component.transform.SetParent(Instance.transform);
                    component.gameObject.SetActive(false);
                    pool.Add(free[i]);
                    free.RemoveRange(i, 1);
                    return;
                }
            }
            Debug.LogError($"Has no component {component} in pool!");
        }
    }
}