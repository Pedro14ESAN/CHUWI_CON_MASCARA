using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// Prepara los recursos de la interfaz (sprites y fuentes) y agrega ChuwiUI + EventSystem a la escena.
// Menu: Chuwi > Instalar interfaz. Corre sola una vez (marca en Library).
public static class ChuwiUIInstaller
{
    const string Res = "Assets/Chuwi/UI/Resources/ChuwiUI";
    const string Stamp = "Library/ChuwiUIInstalled_v1.txt";

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(Stamp) || EditorApplication.isPlayingOrWillChangePlaymode || !Directory.Exists(Res)) return;
            try { Install(); } catch (Exception e) { Debug.LogError("CHUWI_UI: " + e); }
        };
    }

    [MenuItem("Chuwi/Instalar interfaz")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // 1) Sprites
        foreach (var path in Directory.GetFiles(Res, "*.png").Select(p => p.Replace('\\', '/')))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) continue;
            string n = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            int b = n == "ui_round" || n == "ui_round_outline" ? 44 : n == "ui_shadow" ? 76 : 0;
            ti.spriteBorder = new Vector4(b, b, b, b);
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s); s.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
        AssetDatabase.Refresh();

        // 2) Escena
        var scene = SceneManager.GetActiveScene();
        var travel = UnityEngine.Object.FindFirstObjectByType<ChuwiMapTravel>(FindObjectsInactive.Include);
        if (travel == null) throw new Exception("No se encontro ChuwiMapTravel en la escena activa (abre SampleScene).");
        Directory.CreateDirectory("Assets/ChuwiBackups");
        string backup = "Assets/ChuwiBackups/Antes_Interfaz_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        EditorSceneManager.SaveScene(scene, backup, true);

        var ui = UnityEngine.Object.FindFirstObjectByType<ChuwiUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            var go = new GameObject("ChuwiUI");
            SceneManager.MoveGameObjectToScene(go, scene);
            ui = go.AddComponent<ChuwiUI>();
            Undo.RegisterCreatedObjectUndo(go, "Agregar interfaz Chuwi");
        }
        ui.travel = travel; EditorUtility.SetDirty(ui);
        Undo.RecordObject(travel, "Ocultar menu de depuracion");
        travel.showDebugMenu = false; EditorUtility.SetDirty(travel);

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            SceneManager.MoveGameObjectToScene(es, scene);
            Undo.RegisterCreatedObjectUndo(es, "Agregar EventSystem");
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Stamp, DateTime.Now.ToString("O") + "\nBackup: " + backup);
        Selection.activeGameObject = ui.gameObject;
        Debug.Log("CHUWI_UI_READY: interfaz instalada. Dale Play. Backup: " + backup);
    }
}
