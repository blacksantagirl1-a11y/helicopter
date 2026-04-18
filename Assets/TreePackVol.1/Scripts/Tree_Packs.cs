#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class Startup
{
    static Startup()    
    {
        EditorPrefs.SetInt("showCounts_URPTreeModels20", EditorPrefs.GetInt("showCounts_URPTreeModels20") + 1);

        if (EditorPrefs.GetInt("showCounts_URPTreeModels20") == 1)       
        {
            Application.OpenURL("https://assetstore.unity.com/publishers/23606");
            // System.IO.File.Delete("Assets/SportCar/Racing_Game.cs");
        }
    }     
}
#endif
