using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadShipVisual : MonoBehaviour, IDestroyeble
{
    public List<GameObject> ActiveOnDeath;
    public List<GameObject> ActiveOnLive;

    public void Death()
    {
        foreach (var hit in ActiveOnDeath)
            hit.SetActive(true);
        foreach (var hit in ActiveOnLive)
            hit.SetActive(false);
    }

    public void Spawn()
    {
        foreach (var hit in ActiveOnDeath)
            hit.SetActive(false);
        foreach (var hit in ActiveOnLive)
            hit.SetActive(true);
    }
}
