using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Instala el Chuwi nuevo (Meshy + Mixamo) en lugar de Chuwi_ViajeroV2.
// Menu: Chuwi > Instalar Chuwi Mixamo. Se ejecuta solo una vez al abrir Unity (marca en Library).
public static class ChuwiMixamoSetup
{
    const string Folder = "Assets/Animaciones de chuwi";
    const string NewName = "Chuwi_Mixamo";
    const string Stamp = "Library/ChuwiMixamoInstalled_v2.txt";

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(Stamp) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (FindBaseModel() == null) return;
            try { Install(); } catch (Exception e) { Debug.LogError("CHUWI_MIXAMO: " + e.Message); }
        };
    }

    static string FindBaseModel()
    {
        return Directory.GetFiles(Folder, "*.fbx").Select(p => p.Replace('\\', '/'))
            .FirstOrDefault(p => !Path.GetFileName(p).Contains("@"));
    }

    static string FindAnim(params string[] keys)
    {
        var files = Directory.GetFiles(Folder, "*@*.fbx").Select(p => p.Replace('\\', '/')).ToArray();
        foreach (var k in keys)
        {
            var f = files.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Split('@')[1].Equals(k, StringComparison.OrdinalIgnoreCase));
            if (f != null) return f;
        }
        foreach (var k in keys)
        {
            var f = files.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Split('@')[1].IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
            if (f != null) return f;
        }
        return null;
    }

    [MenuItem("Chuwi/Instalar Chuwi Mixamo")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string basePath = FindBaseModel();
        if (basePath == null) throw new Exception("No hay FBX base (sin @) en " + Folder);

        // 1) Modelo base como Humanoid
        var bi = (ModelImporter)AssetImporter.GetAtPath(basePath);
        bi.animationType = ModelImporterAnimationType.Human;
        bi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        bi.importAnimation = false;
        bi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        bi.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(basePath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new Exception("El avatar Humanoid no es valido. Revisa el Rig del FBX base.");

        // 2) Materiales URP con la textura horneada
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new Exception("No se encontro URP Lit.");
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Chuwi_Albedo.png");
        Directory.CreateDirectory(Folder + "/Materiales");
        var body = GetMat(Folder + "/Materiales/Chuwi_Cuerpo.mat", shader);
        if (tex != null) { body.SetTexture("_BaseMap", tex); body.SetTexture("_MainTex", tex); }
        body.SetColor("_BaseColor", Color.white); body.SetFloat("_Smoothness", .15f); body.SetFloat("_Metallic", 0);
        var lens = GetMat(Folder + "/Materiales/Chuwi_Lentes.mat", shader);
        lens.SetColor("_BaseColor", new Color(.02f, .02f, .025f)); lens.SetFloat("_Smoothness", .85f); lens.SetFloat("_Metallic", .3f);
        EditorUtility.SetDirty(body); EditorUtility.SetDirty(lens);
        foreach (var src in AssetDatabase.LoadAllAssetsAtPath(basePath).OfType<Material>())
            bi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name),
                src.name.IndexOf("Lente", StringComparison.OrdinalIgnoreCase) >= 0 ? lens : body);
        bi.SaveAndReimport();

        // 3) Animaciones: Humanoid copiando el avatar, en el sitio
        var wanted = new Dictionary<string, string[]> {
            {"Idle",     new[]{"Breathing Idle","Happy Idle","Idle"}},
            {"Walk",     new[]{"Walking","Walk In Circle","Walk"}},
            {"Greeting", new[]{"Waving","Wave"}},
            {"Pointing", new[]{"Pointing"}},
            {"Dance",    new[]{"Hip Hop Dancing","Dancing"}},
        };
        var clips = new Dictionary<string, AnimationClip>();
        var log = new List<string>();
        foreach (var kv in wanted)
        {
            string p = FindAnim(kv.Value);
            if (p == null) { log.Add(kv.Key + ": FALTA"); continue; }
            var ai = (ModelImporter)AssetImporter.GetAtPath(p);
            ai.animationType = ModelImporterAnimationType.Human;
            ai.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            ai.sourceAvatar = avatar;
            ai.importAnimation = true;
            ai.materialImportMode = ModelImporterMaterialImportMode.None;
            var cs = ai.defaultClipAnimations;
            bool loop = kv.Key == "Idle" || kv.Key == "Walk" || kv.Key == "Dance";
            foreach (var c in cs)
            {
                c.name = kv.Key; c.loopTime = loop; c.loopPose = loop;
                // En Walk el giro y avance del clip NO se hornean: van a root motion y se descartan
                // (applyRootMotion = false), asi el paso queda recto y el script mueve a Chuwi.
                bool w = kv.Key == "Walk";
                c.lockRootRotation = !w; c.lockRootHeightY = true; c.lockRootPositionXZ = !w;
                c.keepOriginalOrientation = !w; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = !w;
            }
            ai.clipAnimations = cs; ai.SaveAndReimport();
            var clip = AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
            if (clip != null) { clips[kv.Key] = clip; log.Add(kv.Key + ": " + Path.GetFileName(p)); }
        }
        foreach (var k in new[] { "Walk", "Greeting", "Pointing" })
            if (!clips.ContainsKey(k)) throw new Exception("Falta la animacion " + k + " en " + Folder);

        // 4) Animator Controller (mismos parametros que usa ChuwiMapTravel)
        string cp = Folder + "/Chuwi_Mixamo.controller";
        AssetDatabase.DeleteAsset(cp);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(cp);
        ctrl.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle");
        if (clips.ContainsKey("Idle")) idle.motion = clips["Idle"];
        else { idle.motion = clips["Greeting"]; idle.speed = 0; log.Add("AVISO: sin Idle, se usa el primer cuadro de Waving quieto."); }
        sm.defaultState = idle;
        var walk = sm.AddState("Walk"); walk.motion = clips["Walk"];
        Tr(idle, walk, AnimatorConditionMode.If, false); Tr(walk, idle, AnimatorConditionMode.IfNot, false);
        foreach (var n in new[] { "Greeting", "Pointing", "Dance" })
        {
            if (!clips.ContainsKey(n)) continue;
            ctrl.AddParameter(n, AnimatorControllerParameterType.Trigger);
            var st = sm.AddState(n); st.motion = clips[n];
            var e = sm.AddAnyStateTransition(st); e.hasExitTime = false; e.duration = .15f; e.canTransitionToSelf = false;
            e.AddCondition(AnimatorConditionMode.If, 0, n);
            if (n == "Dance") { var t = st.AddTransition(walk); t.hasExitTime = false; t.duration = .15f; t.AddCondition(AnimatorConditionMode.If, 0, "Moving"); }
            else { Tr(st, idle, AnimatorConditionMode.IfNot, true); Tr(st, walk, AnimatorConditionMode.If, true); }
        }
        AssetDatabase.SaveAssets();

        // 5) Escena: reemplazar el Chuwi actual (se conserva desactivado)
        var scene = SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var target = all.FirstOrDefault(t => t.name == "ImageTarget");
        if (target == null) throw new Exception("No se encontro ImageTarget en la escena activa (abre SampleScene).");
        var prev = target.Find(NewName);
        if (prev != null) Undo.DestroyObjectImmediate(prev.gameObject);
        var travel = target.GetComponentInChildren<ChuwiMapTravel>(true);
        if (travel == null) travel = UnityEngine.Object.FindFirstObjectByType<ChuwiMapTravel>(FindObjectsInactive.Include);
        Transform old = travel != null && travel.character != null && travel.character.name != NewName ? travel.character : null;
        if (old == null) old = all.FirstOrDefault(t => t.name == "Chuwi_ViajeroV2");

        Directory.CreateDirectory("Assets/ChuwiBackups");
        string backup = "Assets/ChuwiBackups/Antes_Mixamo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new Exception("No se pudo respaldar la escena.");

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(basePath), scene);
        inst.name = NewName;
        var anim = inst.GetComponent<Animator>();
        if (anim == null) anim = inst.AddComponent<Animator>();
        anim.avatar = avatar; anim.runtimeAnimatorController = ctrl; anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (inst.GetComponent<ChuwiKeyboardController>() == null) inst.AddComponent<ChuwiKeyboardController>();
        inst.transform.SetParent(old != null ? old.parent : target, false);
        if (old != null)
        {
            old.gameObject.SetActive(true);
            var ob = BoundsOf(old.gameObject);
            inst.transform.position = old.position; inst.transform.rotation = old.rotation; inst.transform.localScale = Vector3.one;
            var nb = BoundsOf(inst);
            if (nb.size.y > .0001f) inst.transform.localScale = Vector3.one * (ob.size.y / nb.size.y);
            nb = BoundsOf(inst);
            inst.transform.position += Vector3.up * (ob.min.y - nb.min.y);
            Undo.RecordObject(old.gameObject, "Desactivar Chuwi anterior"); old.gameObject.SetActive(false);
        }
        if (travel != null)
        {
            Undo.RecordObject(travel, "Usar Chuwi Mixamo");
            var ip = inst.transform.position;
            float bottom = target.InverseTransformPoint(new Vector3(ip.x, BoundsOf(inst).min.y, ip.z)).y;
            travel.character = inst.transform;
            travel.feetOffset = target.InverseTransformPoint(inst.transform.position).y - bottom;
            EditorUtility.SetDirty(travel);
        }
        Undo.RegisterCreatedObjectUndo(inst, "Agregar Chuwi Mixamo");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        log.Insert(0, "Backup: " + backup);
        log.Add("Altura final (m): " + BoundsOf(inst).size.y);
        File.WriteAllText(Stamp, DateTime.Now.ToString("O") + "\n" + string.Join("\n", log));
        File.WriteAllLines(Folder + "/chuwi_mixamo_log.txt", log);
        Selection.activeGameObject = inst;
        Debug.Log("CHUWI_MIXAMO_READY\n" + string.Join("\n", log));
    }

    static Material GetMat(string path, Shader sh)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh; return m;
    }
    static Bounds BoundsOf(GameObject root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    static void Tr(AnimatorState from, AnimatorState to, AnimatorConditionMode cond, bool exit)
    {
        var t = from.AddTransition(to); t.hasExitTime = exit; t.exitTime = 1; t.duration = .14f; t.AddCondition(cond, 0, "Moving");
    }
}
