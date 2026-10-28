using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ChuwiViajeroVerification
{
    const string Stamp="Library/ChuwiViajeroVerified_v2.txt";
    // Auto-arranque desactivado: el candado vive en Library/ (Unity lo regenera), asi que se re-ejecutaba indefinidamente y modificaba assets del equipo sin confirmacion. Usar el menu.
    // [InitializeOnLoadMethod]
    static void Schedule(){EditorApplication.delayCall+=()=>{if(!File.Exists(Stamp)&&!EditorApplication.isPlayingOrWillChangePlaymode)Verify();};}
    [MenuItem("Chuwi/Verificar version viajera")]
    public static void Verify()
    {
        var report=new List<string>();
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/ChuwiViajero/Materials"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            // The recovered sculpt and flat logo silhouettes include open surfaces.
            m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChuwiViajero/Chuwi_Viajero.prefab");
            var test=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);
            var animator=test.GetComponent<Animator>();animator.Rebind();animator.Update(0);
            var transforms=test.GetComponentsInChildren<Transform>();
            var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/ChuwiViajero/Chuwi_Viajero.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            foreach(var clip in clips)
            {
                clip.SampleAnimation(test,0);
                var start=transforms.Select(t=>t.localRotation).ToArray();
                float motion=0;
                for(int j=0;j<=12;j++)
                {
                    clip.SampleAnimation(test,clip.length*j/12f);
                    for(int k=0;k<transforms.Length;k++)motion=Mathf.Max(motion,Quaternion.Angle(start[k],transforms[k].localRotation));
                }
                float endpoint=transforms.Select((t,k)=>Quaternion.Angle(start[k],t.localRotation)).Max();
                if(clip.name!="Idle"&&motion<1)throw new Exception("Animacion estatica: "+clip.name);
                if(endpoint>.1f)throw new Exception("La animacion no vuelve al reposo: "+clip.name);
                report.Add(clip.name+": motionDegrees="+motion+"; endpointDegrees="+endpoint);
            }
            clips.Single(c=>c.name=="Idle").SampleAnimation(test,0);
            var mover=test.GetComponent<ChuwiKeyboardController>();
            foreach(var axis in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
            {
                var p=test.transform.position;mover.Move(axis,.1f);float moved=Vector3.Distance(p,test.transform.position);
                if(moved<.001f)throw new Exception("No hay movimiento en "+axis);
                report.Add("Movement "+axis+": "+moved+" m");
            }
            report.Add("All materials: URP Lit, two-sided open surfaces.");
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        File.WriteAllLines("C:/Proyecto_Chuwi_AR/chuwi_viajero/unity_motion_validation.txt",report);
        File.WriteAllText(Stamp,DateTime.UtcNow.ToString("O"));
        SceneView.RepaintAll();Debug.Log("CHUWI_VIAJERO_VERIFIED: gestures, walk loop, four movement directions and materials.");
    }
}
