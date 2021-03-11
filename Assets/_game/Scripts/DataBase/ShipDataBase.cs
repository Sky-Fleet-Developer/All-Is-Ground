using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName ="ShipData",menuName ="DataBase/Ship")]
public class ShipDataBase : ScriptableObject
{
    public int Count => ships.Length;

    [SerializeField] private Garage.ShipSet[] ships;

    public Garage.ShipSet[] GetArray() {
        return ships;
    }

    public Garage.ShipSet GetShipID(int id) {
        return ships.Where((x) => x.ShipPrefab.ID == id).FirstOrDefault();
    }

    public Garage.ShipSet GetShipIndex(int i)
    {
        return ships[i];
    }
}
