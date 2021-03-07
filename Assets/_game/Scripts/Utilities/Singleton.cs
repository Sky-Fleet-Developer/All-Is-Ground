using System.Linq;
using UnityEngine;

public class DontDestroyOnLoad : System.Attribute { }

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    protected static T _instance;
    private static Object _lock = new Object();
    
    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    Instantiate();
                }

                return _instance;
            }
        }
    }
    
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            Setup();
        }
    }

    public static void ResetInstance()
    {
        _instance = null;
    }
    
    protected static T Instantiate()
    {
        _instance = FindObjectOfType<T>();
        if (_instance != null)
        {
            _instance.Setup();
        }
        else
        {
            var go = new GameObject($"[{typeof(T)}]");
            _instance = go.AddComponent<T>();
        }

        var attribute = typeof(T).GetCustomAttributes(true).FirstOrDefault(x => x is DontDestroyOnLoad);
        if (attribute != null)
        {
            DontDestroyOnLoad(_instance);
        }
        
        return _instance;
    }

    protected virtual void Setup()
    {
        
    }
}