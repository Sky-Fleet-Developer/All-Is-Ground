using System.Linq;
using UnityEngine;

[System.Serializable]
public class Account
{
    public string Name;
    public int Experience;
    public int FreeExperience;
    public Garage.ShipSet ShoosedMachine;
    public Garage.ShipSet AIMachine;

    public Account(string name, int experience, int freeExp, Storage storage)
    {
        Name = name;
        Experience = experience;
        Storage.SetGlobalResourceValue("Experience", experience);
        FreeExperience = freeExp;
        ShoosedMachine = Garage.Instance.Ships.Where(x => x.PrefabName == PlayerPrefs.GetString("PlayerMachine", "MMZ")).SingleOrDefault();
        AIMachine = Garage.Instance.Ships.Where(x => x.PrefabName == PlayerPrefs.GetString("AIMachine", "MMZ")).SingleOrDefault();

        foreach (var hit in Object.FindObjectsOfType<MonoBehaviourPlus>())
        {
            var ae = hit.GetComponent<IAccountEvents>();
            if (ae != null)
                ae.OnEnterAccount(this);
        }
    }
}