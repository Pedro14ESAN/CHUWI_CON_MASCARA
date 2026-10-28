using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ChuwiMapSetup
{
    const string Stamp = "Library/ChuwiMapTravelInstalled.txt";
    // Auto-arranque desactivado: el candado vive en Library/ (Unity lo regenera), asi que se re-ejecutaba indefinidamente y modificaba assets del equipo sin confirmacion. Usar el menu.
    /* [InitializeOnLoadMethod] */ static void Schedule()
    {
        EditorApplication.delayCall += () => {
            if (!File.Exists(Stamp) && !EditorApplication.isPlayingOrWillChangePlaymode &&
                SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Any(t => t.name == "Mapa_Coloreado")) Install();
        };
    }
    [MenuItem("Chuwi/Preparar mapa y destinos")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var target = all.FirstOrDefault(t => t.name == "ImageTarget");
        var map = all.FirstOrDefault(t => t.name == "Mapa_Coloreado");
        var character = all.FirstOrDefault(t => t.name == "Chuwi_ViajeroV2");
        if (target == null || map == null || character == null) throw new Exception("Se necesitan ImageTarget, Mapa_Coloreado y Chuwi_ViajeroV2 en la escena.");
        if (target.GetComponent<ChuwiMapTravel>() != null) return;
        var mf = map.GetComponentsInChildren<MeshFilter>().OrderByDescending(m => m.sharedMesh != null ? m.sharedMesh.vertexCount : 0).First();
        var mesh = mf.sharedMesh;
        var vertices = mesh.vertices;
        var materials = mf.GetComponent<Renderer>().sharedMaterials;
        Vector3 Center(string name)
        {
            int sub = Array.FindIndex(materials, m => m != null && m.name.Contains(name));
            if (sub < 0) throw new Exception("No se encontró material de continente: " + name);
            var ids = mesh.GetIndices(sub).Distinct().ToArray();
            Vector3 sum = Vector3.zero; foreach (int i in ids) sum += vertices[i];
            return sum / ids.Length;
        }
        Vector3 na = Center("Norteamerica"), sa = Center("Sudamerica"), asia = Center("Asia");
        var b = mesh.bounds;
        int[] axes = Enumerable.Range(0, 3).OrderByDescending(i => b.size[i]).ToArray();
        Vector3 east = Vector3.zero, north = Vector3.zero, normal = Vector3.zero;
        east[axes[0]] = Mathf.Sign((asia - na)[axes[0]]);
        north[axes[1]] = Mathf.Sign((na - sa)[axes[1]]);
        normal[axes[2]] = 1;
        if (Vector3.Dot(mf.transform.TransformDirection(normal), target.up) < 0) normal = -normal;
        if (Mathf.Abs(Vector3.Dot(mf.transform.TransformDirection(normal).normalized, target.up)) < .95f)
            throw new Exception("El mapa debe estar horizontal respecto al ImageTarget antes de instalar los destinos.");
        Directory.CreateDirectory("Assets/ChuwiBackups");
        string backup = "Assets/ChuwiBackups/Antes_Mapa_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new Exception("No se pudo guardar respaldo.");
        Undo.RecordObject(map, "Preparar mapa"); map.SetParent(target, true);
        character.SetParent(target, true);
        var travel = Undo.AddComponent<ChuwiMapTravel>(target.gameObject);
        travel.character = character;
        var pins = new GameObject("Destinos_Universitarios").transform;
        Undo.RegisterCreatedObjectUndo(pins.gameObject, "Crear destinos"); pins.SetParent(target, false); travel.pins = pins;
        float width = target.InverseTransformVector(mf.transform.TransformVector(east * b.size[axes[0]])).magnitude;
        travel.speed = width * .13f;
        float size = width * .009f; travel.pinSize = size;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Directory.CreateDirectory("Assets/Chuwi/Materials");
        string materialPath = "Assets/Chuwi/Materials/Destino_Pin.mat";
        var pinMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (pinMaterial == null) { pinMaterial = new Material(shader); pinMaterial.color = new Color(.86f,.02f,.22f); AssetDatabase.CreateAsset(pinMaterial, materialPath); }
        string[] names = { "Perú · ESAN", "Canadá", "Estados Unidos", "México", "Colombia", "Brasil", "Chile", "Argentina", "China", "Corea del Sur", "Japón", "Taiwán", "Tailandia" };
        string[] regions = { "América del Sur", "América del Norte", "América del Norte", "América del Norte", "América del Sur", "América del Sur", "América del Sur", "América del Sur", "Asia", "Asia", "Asia", "Asia", "Asia" };
        // Coordenadas aproximadas sobre el mapa artístico, ajustables moviendo cada ancla.
        Vector2[] uv = { new Vector2(.29f,.25f), new Vector2(.20f,.73f), new Vector2(.20f,.60f), new Vector2(.25f,.47f), new Vector2(.30f,.39f), new Vector2(.36f,.29f), new Vector2(.32f,.15f), new Vector2(.36f,.12f), new Vector2(.76f,.65f), new Vector2(.84f,.65f), new Vector2(.90f,.63f), new Vector2(.85f,.55f), new Vector2(.78f,.48f) };
        travel.destinations = new ChuwiMapTravel.Destination[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var anchor = new GameObject(names[i]).transform; anchor.SetParent(pins, false);
            Vector3 local = b.center + east * ((uv[i].x - .5f) * b.size[axes[0]]) + north * ((uv[i].y - .5f) * b.size[axes[1]]) + normal * b.extents[axes[2]];
            anchor.position = mf.transform.TransformPoint(local);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name = "Marcador"; head.transform.SetParent(anchor, false); head.transform.localPosition = Vector3.up * size * 2; head.transform.localScale = Vector3.one * size * 2; head.GetComponent<Renderer>().sharedMaterial = pinMaterial;
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder); stem.name = "Ubicacion"; stem.transform.SetParent(anchor, false); stem.transform.localPosition = Vector3.up * size; stem.transform.localScale = new Vector3(size * .35f, size, size * .35f); stem.GetComponent<Renderer>().sharedMaterial = pinMaterial;
            travel.destinations[i] = new ChuwiMapTravel.Destination { country = names[i], continent = regions[i], anchor = anchor };
        }
        // Reducir el personaje solamente si cubre una parte excesiva del mapa.
        var renderers = character.GetComponentsInChildren<Renderer>();
        Bounds cb = new Bounds(target.InverseTransformPoint(character.position), Vector3.zero);
        foreach (var r in renderers) { cb.Encapsulate(target.InverseTransformPoint(r.bounds.min)); cb.Encapsulate(target.InverseTransformPoint(r.bounds.max)); }
        if (cb.size.y > width * .23f) character.localScale *= width * .23f / cb.size.y;
        float bottom = float.PositiveInfinity;
        foreach (var r in renderers) bottom = Mathf.Min(bottom, target.InverseTransformPoint(r.bounds.min).y);
        travel.feetOffset = target.InverseTransformPoint(character.position).y - bottom;
        character.position = travel.destinations[0].anchor.position + target.up * travel.feetOffset * target.lossyScale.y;
        EditorUtility.SetDirty(travel); EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("No se pudo guardar la escena configurada.");
        File.WriteAllText(Stamp, "13 destinos configurados. Respaldo: " + backup);
        Debug.Log("Chuwi: mapa listo con 13 puntos. Escanea el marcador y elige región y país.");
    }
}
