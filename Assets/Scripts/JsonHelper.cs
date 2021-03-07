﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JsonHelper{
        // https://stackoverflow.com/questions/36239705/serialize-and-deserialize-json-and-json-array-in-unity
        // //Convert to JSON
        // string playerToJson = JsonHelper.ToJson(playerInstance, true);
        // Player[] player = JsonHelper.FromJson<Player>(jsonString);
    
        public static T[] FromJson<T>(string json){
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array){
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint){
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [System.Serializable]
        private class Wrapper<T>{
            public T[] Items;
        }
}
