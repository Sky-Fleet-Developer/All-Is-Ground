using Modernizations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtectionManager : MonoBehaviourPlus
{
    public List<ArmorGroup> ArmorGroups;

    private void Start()
    {
        Apply();
    }

    [System.Serializable]
    public class ArmorGroup : IModifiable
    {
        [Modifiable("Толщина брони")]
        public float ArmorThickness = 20;
        [Modifiable("Качество брони")]
        public float ARC = 2200;
        public List<Damageble> Armor;
        public int group;

        public int GetGroup()
        {
            return group;
        }
    }


    public void Apply()
    {
        foreach(var Hit in ArmorGroups)
        {
            foreach(var hit in Hit.Armor)
            {
                hit.Armor = Hit.ArmorThickness;
                hit.ArmorResistanceCoefficient = Hit.ARC;
            }
        }
    }
}
