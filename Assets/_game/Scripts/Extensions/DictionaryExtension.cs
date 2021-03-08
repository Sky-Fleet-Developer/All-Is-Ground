using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DictionaryExtension
{
    public static V Get<K,V>(this Dictionary<K,V> d, K key)
    {
        d.TryGetValue(key, out V value);
        return value;
    }
}