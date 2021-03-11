using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    public int ID { get { return id; } }

    public bool IsMine { get { return !isCopy; } }

    public Rigidbody PhysShip {get { return rigidbody; }}
    
    [SerializeField] private Module[] modules;

    [SerializeField] private Rigidbody rigidbody;

    [SerializeField] private int id;

    private bool isCopy;

    public void Init(bool isCopy) {
        this.isCopy = isCopy;

        for (int i = 0; i < modules.Length; i++) {
            modules[i].InitFromShip(this);
        }
    }

    public T GetModule<T>() where T : Module {
        for (int i = 0; i < modules.Length; i++) {
            if (modules[i].GetType() == typeof(T)) {
                return modules[i] as T;
            }        
        }
        return null;
    }

    public Module[] GetModules() {
        return modules;
    }
}
