using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ChuwiViajeroV2Setup
{
    const string Folder = "Assets/ChuwiViajeroV2";
    const string ModelPath = Folder + "/Chuwi_ViajeroV2.fbx";
    const string Stamp = "Library/ChuwiViajeroV2Installed_v1.txt";
    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.delayCall += () => {
            if (!File.Exists(Stamp) && !EditorApplication.isPlayingOrWillChangePlaymode &&
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null) Install();
        };
    }

    [MenuItem("Chuwi/Instalar version V2 corregida")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetActiveScene();
        var target = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name=="ImageTarget");
        if (target == null) throw new Exception("No se encontro ImageTarget en la escena activa.");
        var existing = target.Find("Chuwi_ViajeroV2");
        if (existing != null) { Debug.Log("Chuwi viajero ya esta instalado."); return; }

        Directory.CreateDirectory("Assets/ChuwiBackups");
        string backup = "Assets/ChuwiBackups/Antes_Viajero_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new Exception("No se pudo respaldar la escena.");
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.motionNodeName="Root";importer.importAnimation=true;
        importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.importNormals=ModelImporterNormals.Import;
        var clipsSettings=importer.defaultClipAnimations;
        foreach(var setting in clipsSettings)
        {
            var name=new[]{"Idle","Walk","Greeting","Pointing"}.FirstOrDefault(n=>setting.takeName.Contains(n));
            if(name!=null) setting.name=name;
            setting.loopTime=name=="Idle"||name=="Walk";setting.loopPose=setting.loopTime;
            setting.lockRootRotation=true;setting.lockRootPositionXZ=true;setting.lockRootHeightY=true;
            setting.keepOriginalOrientation=true;setting.keepOriginalPositionXZ=true;setting.keepOriginalPositionY=true;
        }
        importer.clipAnimations=clipsSettings;importer.SaveAndReimport();
        Directory.CreateDirectory(Folder+"/Materials");
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) throw new Exception("No se encontro el shader URP Lit.");
        var colors=new Dictionary<string,string>{
            {"Fur","D87820"},{"Face","FFF0D8"},{"Ear","D88773"},{"Hoodie","BD0823"},
            {"RibbedRed","A50D27"},{"Luggage","20365D"},{"Backpack","172A49"},
            {"Frames","111722"},{"Lenses","132F43"},{"Mouth","491E1E"},{"Tongue","E86D75"},
            {"Logo","FFF9ED"},{"HandleMetal","A6B4C4"},{"Wheels","171B25"},{"Nose","43251A"}};
        foreach(var source in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>())
        {
            string key=source.name.Replace("Viajero_","");
            string path=Folder+"/Materials/"+source.name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
            m.shader=shader;m.SetFloat("_Cull",0);
            Color c;ColorUtility.TryParseHtmlString("#"+(colors.ContainsKey(key)?colors[key]:"FFFFFF"),out c);
            m.SetColor("_BaseColor",c);m.SetColor("_Color",c);
            float gloss=key=="Lenses"?.78f:key=="Frames"?.65f:key=="HandleMetal"?.76f:key=="Luggage"?.4f:.18f;
            m.SetFloat("_Smoothness",gloss);m.SetFloat("_Metallic",key=="HandleMetal"?.75f:key=="Lenses"?.3f:0);
            EditorUtility.SetDirty(m);importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),m);
        }
        importer.SaveAndReimport();
        var clips=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        foreach(string n in new[]{"Idle","Walk","Greeting","Pointing"})
            if(!clips.Any(c=>c.name==n))throw new Exception("Falta animacion "+n);
        string cp=Folder+"/Chuwi_ViajeroV2.controller";
        var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);
        if(ctrl==null)
        {
            ctrl=AnimatorController.CreateAnimatorControllerAtPath(cp);ctrl.AddParameter("Moving",AnimatorControllerParameterType.Bool);
            var sm=ctrl.layers[0].stateMachine;
            var idle=sm.AddState("Idle");idle.motion=clips.Single(c=>c.name=="Idle");sm.defaultState=idle;
            var walk=sm.AddState("Walk");walk.motion=clips.Single(c=>c.name=="Walk");
            Transition(idle,walk,AnimatorConditionMode.If,false);Transition(walk,idle,AnimatorConditionMode.IfNot,false);
            foreach(string n in new[]{"Greeting","Pointing"})
            {
                ctrl.AddParameter(n,AnimatorControllerParameterType.Trigger);var st=sm.AddState(n);st.motion=clips.Single(c=>c.name==n);
                var enter=sm.AddAnyStateTransition(st);enter.hasExitTime=false;enter.duration=.15f;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,n);
                Transition(st,idle,AnimatorConditionMode.IfNot,true);Transition(st,walk,AnimatorConditionMode.If,true);
            }
        }
        var old=target.Find("Chuwi_Viajero") ?? target.Find("Chuwi");
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);instance.name="Chuwi_ViajeroV2";
        var animator=instance.GetComponent<Animator>();if(animator==null)animator=instance.AddComponent<Animator>();
        animator.runtimeAnimatorController=ctrl;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        instance.AddComponent<ChuwiKeyboardController>();
        Validate(instance,clips);
        PrefabUtility.SaveAsPrefabAssetAndConnect(instance,Folder+"/Chuwi_ViajeroV2.prefab",InteractionMode.AutomatedAction);
        Undo.RegisterCreatedObjectUndo(instance,"Agregar Chuwi viajero");
        instance.transform.SetParent(target,false);
        if(old!=null)
        {
            var oldBounds=BoundsOf(old.gameObject);instance.transform.localPosition=old.localPosition;instance.transform.localRotation=old.localRotation;
            instance.transform.localScale=old.localScale;
            var newBounds=BoundsOf(instance);
            if(newBounds.size.y>.001f)instance.transform.localScale*=oldBounds.size.y/newBounds.size.y;
            Undo.RecordObject(old.gameObject,"Conservar version anterior desactivada");old.gameObject.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Stamp,DateTime.Now.ToString("O")+"\nBackup: "+backup);
        Selection.activeGameObject=instance; ChuwiViajeroV2Verification.Verify();
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("CHUWI_VIAJERO_READY: nueva version instalada, anterior conservada desactivada. Backup: "+backup);
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true);var b=renderers[0].bounds;
        foreach(var r in renderers)b.Encapsulate(r.bounds);return b;
    }
    static void Transition(AnimatorState from,AnimatorState to,AnimatorConditionMode cond,bool exit)
    {
        var t=from.AddTransition(to);t.hasExitTime=exit;t.exitTime=1;t.duration=.14f;t.AddCondition(cond,0,"Moving");
    }
    static void Validate(GameObject instance,AnimationClip[] clips)
    {
        var renderers=instance.GetComponentsInChildren<SkinnedMeshRenderer>();
        if(renderers.Length==0)throw new Exception("No hay mallas con rig.");
        if(renderers.Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name!="Universal Render Pipeline/Lit")))throw new Exception("Material incompatible.");
        var transforms=instance.GetComponentsInChildren<Transform>();
        var lines=new List<string>();
        foreach(var clip in clips)
        {
            clip.SampleAnimation(instance,0);var start=transforms.Select(t=>t.localToWorldMatrix).ToArray();
            foreach(float t in new[]{0f,.25f,.5f,.75f,1f})
            {
                clip.SampleAnimation(instance,t*clip.length);
                foreach(var r in renderers)
                {
                    var mesh=new Mesh();r.BakeMesh(mesh);
                    bool valid=mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z));
                    UnityEngine.Object.DestroyImmediate(mesh);
                    if(!valid)throw new Exception("Deformacion invalida: "+clip.name);
                }
            }
            lines.Add(clip.name+": "+clip.length+" seconds; finite skinning; loop="+clip.isLooping);
        }
        clips.Single(c=>c.name=="Idle").SampleAnimation(instance,0);
        lines.Add("Skinned renderers: "+renderers.Length);
        lines.Add("Vertices: "+renderers.Sum(r=>r.sharedMesh.vertexCount));
        lines.Add("Height: "+BoundsOf(instance).size.y);
        File.WriteAllLines("C:/Proyecto_Chuwi_AR/chuwi_viajero_v2/unity_validation.txt",lines);
    }
}

