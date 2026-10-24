using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Rendering;

public static class ChuwiSetup
{
    const string Folder = "Assets/Chuwi";
    const string ModelPath = Folder + "/Chuwi_Unity.fbx";

    [MenuItem("Chuwi/Preparar materiales y Animator")]
    public static void Configure()
    {
        AssetDatabase.Refresh();
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        if (importer == null) throw new Exception("No se encuentra " + ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.motionNodeName = "Root";
        importer.importAnimation = true;
        importer.globalScale = 1f;
        importer.importNormals = ModelImporterNormals.Import;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        var settings = importer.defaultClipAnimations;
        foreach (var c in settings)
        {
            var name = new[] { "Walk", "Greeting", "Pointing" }.FirstOrDefault(n => c.takeName.Contains(n));
            if (name != null) c.name = name;
            c.loopTime = name == "Walk";
            c.loopPose = name == "Walk";
            c.lockRootRotation = true;
            c.lockRootPositionXZ = true;
            c.lockRootHeightY = true;
            c.keepOriginalOrientation = true;
            c.keepOriginalPositionXZ = true;
            c.keepOriginalPositionY = true;
        }
        importer.clipAnimations = settings;
        importer.SaveAndReimport();
        Directory.CreateDirectory(Folder + "/Materials");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/Chuwi_BaseColor.png");
        var shader = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Standard");
        var materialSources = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>()
            .Concat(AssetDatabase.FindAssets("t:Material", new[] { Folder + "/Materials" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid))))
            .Where(m => m != null).GroupBy(m => m.name).Select(g => g.First()).ToArray();
        foreach (var embedded in materialSources)
        {
            string path = Folder + "/Materials/" + embedded.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            Color color = Color.white;
            if (embedded.name.Contains("Orange")) ColorUtility.TryParseHtmlString("#C85C13", out color);
            else if (embedded.name.Contains("Dark Red")) ColorUtility.TryParseHtmlString("#8D0A20", out color);
            else if (embedded.name.Contains("Red")) ColorUtility.TryParseHtmlString("#B90925", out color);
            else if (embedded.name.Contains("Ivory")) ColorUtility.TryParseHtmlString("#FFF9ED", out color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (embedded.name.Contains("Palette"))
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .48f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .48f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), material);
        }
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
        foreach (string expected in new[] { "Walk", "Greeting", "Pointing" })
            if (!clips.Any(c => c.name == expected)) throw new Exception("Falta el clip " + expected);
        string controllerPath = Folder + "/Chuwi.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var walk = machine.AddState("Walk");walk.motion = clips.Single(c => c.name == "Walk");machine.defaultState = walk;
            foreach (var name in new[] { "Greeting", "Pointing" })
            {
                controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
                var state = machine.AddState(name);state.motion = clips.Single(c => c.name == name);
                var enter = machine.AddAnyStateTransition(state);enter.hasExitTime = false;enter.duration = .12f;enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.If, 0, name);
                var leave = state.AddTransition(walk);leave.hasExitTime = true;leave.exitTime = 1;leave.duration = .15f;
            }
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Chuwi";
        var animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;animator.applyRootMotion = false;
        PrefabUtility.SaveAsPrefabAsset(instance, Folder + "/Chuwi.prefab");
        UnityEngine.Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
    }

    [Serializable] public class ClipReport { public string name; public float seconds; public float fps; public bool loop; public float motionMeters; public float endpointDifferenceMeters; }
    [Serializable] public class Report { public string unityVersion; public int renderers; public int vertices; public int triangles; public int skeletonTransforms; public float restHeightMeters; public bool finiteSkinning; public bool allMaterialsAssigned; public ClipReport[] clips; }

    public static void ValidateAndPackage()
    {
        Configure();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Chuwi.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (renderers.Length == 0) throw new Exception("No skinned renderers imported");
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
        var report = new Report { unityVersion = Application.unityVersion, renderers = renderers.Length,
            vertices = renderers.Sum(r => r.sharedMesh.vertexCount), triangles = renderers.Sum(r => r.sharedMesh.triangles.Length / 3),
            skeletonTransforms = instance.GetComponentsInChildren<Transform>().Length, finiteSkinning = true,
            allMaterialsAssigned = renderers.All(r => r.sharedMaterials.All(m => m != null && m.shader != null)) };
        var transforms = instance.GetComponentsInChildren<Transform>();
        var entries = new List<ClipReport>();
        foreach (var clip in clips)
        {
            clip.SampleAnimation(instance, 0);
            var start = transforms.Select(t => t.position).ToArray();
            float maxMotion = 0;
            for (int i = 0; i <= 30; i++)
            {
                clip.SampleAnimation(instance, clip.length * i / 30f);
                maxMotion = Mathf.Max(maxMotion, transforms.Select((t,k) => Vector3.Distance(t.position, start[k])).Max());
                foreach (var renderer in renderers)
                {
                    var baked = new Mesh();renderer.BakeMesh(baked);
                    if (baked.vertices.Any(v => float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.z))) report.finiteSkinning = false;
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            clip.SampleAnimation(instance, clip.length);
            float endpoint = transforms.Select((t,k) => Vector3.Distance(t.position,start[k])).Max();
            entries.Add(new ClipReport { name=clip.name,seconds=clip.length,fps=clip.frameRate,loop=clip.isLooping,motionMeters=maxMotion,endpointDifferenceMeters=endpoint });
            if (maxMotion < .005f) throw new Exception("Static animation: " + clip.name);
            if (endpoint > .003f) throw new Exception("Endpoint mismatch: " + clip.name + " " + endpoint);
        }
        clips[0].SampleAnimation(instance,0);
        Bounds bounds = new Bounds();bool first=true;
        foreach(var renderer in renderers)
        {
            var mesh=new Mesh();renderer.BakeMesh(mesh);
            foreach(var v in mesh.vertices) { var world=renderer.transform.TransformPoint(v);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world); }
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        report.restHeightMeters=bounds.size.y;report.clips=entries.ToArray();
        if (!report.finiteSkinning || !report.allMaterialsAssigned || report.restHeightMeters < 1.1f || report.restHeightMeters > 1.3f) throw new Exception("Invalid model import");
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../chuwi_output"));
        File.WriteAllText(Path.Combine(output,"unity_validation.json"),JsonUtility.ToJson(report,true));
        UnityEngine.Object.DestroyImmediate(instance);
        AssetDatabase.ExportPackage(Folder,Path.Combine(output,"Chuwi_Unity.unitypackage"),ExportPackageOptions.Recurse);
        Debug.Log("CHUWI_VALIDATION_SUCCESS " + JsonUtility.ToJson(report));
    }
}
