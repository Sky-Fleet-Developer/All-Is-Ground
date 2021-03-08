using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolObject : MonoBehaviour
{
    public Pooling PoolMassive;
    public UILink UILink;
    public int ID;
    private void Awake()
    {
        UILink = GetComponent<UILink>();
    }

    public void Use(Pooling pooling)
    {
        PoolMassive = pooling;
    }
    public IEnumerator Deactive(float Delay, Pooling pooling)
    {
        yield return new WaitForSeconds(Delay);
        Deactivate();
    }
    public void Deactivate()
    {
        PoolMassive.Deactive(this);
    }
}
