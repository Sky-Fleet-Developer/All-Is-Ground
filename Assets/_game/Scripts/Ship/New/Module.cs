using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Module : MonoBehaviour
{
    // Start is called before the first frame update

    protected Ship ship;

    public void InitFromShip(Ship ship) {
        this.ship = ship;
        OnInit();
    }

    protected virtual void OnInit() { 
    
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual object[] GetPacketPhoton() {
        return new object[0];
    }

    public virtual void SetPacketPhoton(object[] packet) { 
    
    }


    public virtual void DestroyModule() { 
    }
}
