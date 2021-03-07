using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolObject : MonoBehaviour
{
    public Pooling PoolMassive;
    public int ID;

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
