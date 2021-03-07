using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


    // Работа с Enums
    public static class Enums{
        // V2.0 Вывод в лист значений заданного Enumа 
        static public List<string> EnumList(object en){
            var eType=en.GetType();
            return System.Enum.GetNames(eType).ToList();	
        }

        // V2.0 Возврfn ID (позиции) enumа
        static public int EnumId(object en){
            var eType=en.GetType();
            int number = (int)System.Convert.ChangeType(en, System.Enum.GetUnderlyingType(eType));	
            //Debug.LogFormat("EnumId: {0}  return: {1}",en,number);
            return number;
        }
        
        // V2.0 Возврат enum по его id 
        static public object EnumValue(object en, int num){
            var eType=en.GetType();
            return System.Enum.ToObject(eType,num);
        }

        // V2.0 Кол во enumов
        static public int EnumCount(object en){
            var eType=en.GetType();
            return System.Enum.GetNames(eType).Length;
        }
        
        // V2.0 Next EnumValue
        static public object EnumNext(object en, bool loop=true){
            int id=EnumId(en);
            int count=EnumCount(en);
            if (id<count-1)
                return EnumValue(en,id+1);
            else
                if (loop){
                    //Debug.LogFormat(" EnumNext loop ID: {0}",id);
                    return EnumValue(en,0);
                }	
            return en;
        }

        // V2.0 Prev EnumValue
        static public object EnumPrev(object en, bool loop=true){
            int id=EnumId(en);
            int count=EnumCount(en);
            if (id>0)
                return EnumValue(en,id-1);
            else
                if (loop){
                    //Debug.LogFormat(" EnumPrev loop ID: {0}",id);
                    return EnumValue(en,count-1);
                }	
            return en;
        }
    }
