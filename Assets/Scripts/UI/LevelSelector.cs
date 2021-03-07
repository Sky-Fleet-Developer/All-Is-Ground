using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    UsersDATA Data;
    GameValues Values;
    Pooling pool;
    void Start()
    {
        Values = Resources.Load<GameValues>("GameValues");
        pool = GetComponent<Pooling>();
        Data = FindObjectOfType<UsersDATA>();

        for(int i = 0; i < Values.levels.Count; i++)
        {
            var item = pool.Use().GetComponent<UILink>();
            var img = item.GetChildByName("Image").Image;
            var Name = item.GetChildByName("Name").Text;

            img.sprite = Values.levels[i].Image;
            Name.text = Values.levels[i].Name;
            int n = i;
            item.Button.onClick.AddListener(delegate { Data.SelectCreationLevel(n); });
        }
    }
}
