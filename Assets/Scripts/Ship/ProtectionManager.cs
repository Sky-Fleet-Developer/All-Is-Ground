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
    public class ArmorGroup : IComponent
    {
        public float ArmorThickness = 20;
        public float ARC = 2200;
        public List<Damageble> Armor;

        public System.Type GetFieldType(string name)
        {

            System.Type type = GetType();
            var field = type.GetField(name);
            return field.GetType();
        }

        public void SetField(string name, object value, object zero)
        {
            System.Type type = GetType();

            var field = type.GetField(name);

            object Val = 0f;
            if (field != null)
            {
                if (value == null)
                    field.SetValue(this, zero);
                field.SetValue(this, value);
            }
        }
        public object GetField(string name)
        {
            System.Type type = GetType();
            var field = type.GetField(name);
            return field.GetValue(this);
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
