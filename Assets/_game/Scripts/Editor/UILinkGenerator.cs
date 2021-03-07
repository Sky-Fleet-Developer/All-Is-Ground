using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;

public class UILinkGenerator
{
    public static string[] preset = new string[]
    {
        "using UnityEngine;",
        "using System.Collections;",
        "public partial class UILink",
        "[{0}",
        "    public StaticObjectTypes StaticObject = StaticObjectTypes.None;",
        "    public enum StaticObjectTypes",
        "    [",
        "        None = 1000,{1}",
        "    ]",
        "    public void OnInit()",
        "    [",
        "        switch (StaticObject)",
        "        [{2}",
        "        ]",
        "    ]",
        "]",
    };


    [MenuItem("Tools/Generator/Get UI links")]
    public static void Generate()
    {
        string filePath = Application.dataPath + "/_game/Scripts/Utilities/UILinkTable.cs";

        string fileText = AddStrings(string.Empty, preset);

        string[] names = UILinkNames.Instance.links;

        string fields = string.Empty;

        for (int i = 0; i < names.Length; i++)
        {
            fields += $"\n    public static UILink {names[i]};";
        }

        string enums = string.Empty;

        for(int i = 0; i < names.Length; i++)
        {
            enums += $"\n        {names[i]} = {i},";
        }

        string Switch = string.Empty;
        for (int i = 0; i < names.Length; i++)
        {
            Switch += $"\n            case StaticObjectTypes.{names[i]}:\n                {names[i]} = this;\n                break;";
        }

        fileText = string.Format(fileText, fields, enums, Switch).Replace('[', '{').Replace(']', '}');

        if (File.Exists(filePath))
            File.Delete(filePath);
        var stream = File.Create(filePath);
        var bytes = Encoding.Default.GetBytes(fileText);
        stream.Write(bytes, 0, bytes.Length);
        stream.Close();
        AssetDatabase.Refresh();
    }

    public static string AddStrings(string value, string[] strings)
    {
        foreach (var hit in strings)
        {
            value += hit + "\n";
        }
        return value;
    }
}