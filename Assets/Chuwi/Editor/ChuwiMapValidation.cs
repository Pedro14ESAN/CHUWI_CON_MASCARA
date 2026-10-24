using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ChuwiMapValidation
{
    [InitializeOnLoadMethod] static void Schedule() { EditorApplication.delayCall += Validate; }
    [MenuItem("Chuwi/Validar movimiento del mapa")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("NavigationValidation"); SceneManager.MoveGameObjectToScene(root, preview);
            root.transform.rotation = Quaternion.Euler(0, 137, 0); root.transform.localScale = Vector3.one * 2;
            var ch = new GameObject("Character").transform; ch.SetParent(root.transform, false);
            var a = new GameObject("Start").transform; a.SetParent(root.transform, false);
            var b = new GameObject("Goal").transform; b.SetParent(root.transform, false); b.localPosition = new Vector3(10,0,15);
            var nav = root.AddComponent<ChuwiMapTravel>(); nav.character = ch; nav.speed = 5; nav.feetOffset = 1;
            nav.destinations = new[] { new ChuwiMapTravel.Destination { country="Inicio", anchor=a }, new ChuwiMapTravel.Destination { country="Destino", anchor=b } };
            typeof(ChuwiMapTravel).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(nav,null);
            var start = ch.position; nav.SelectDestination(1); nav.Tick(1);
            if (Vector3.Distance(start,ch.position) > .001f) throw new Exception("Se mueve sin tracking.");
            nav.previewWithoutMarker = true; nav.SelectDestination(1); nav.Tick(.1f);
            if (Vector3.Distance(start,ch.position) < .1f) throw new Exception("No inicia movimiento.");
            nav.previewWithoutMarker = false; var paused = ch.position; nav.Tick(1);
            if (Vector3.Distance(paused,ch.position) > .001f) throw new Exception("No pausa al perder tracking.");
            nav.previewWithoutMarker = true;
            for (int i=0;i<100;i++) nav.Tick(.1f);
            if (Vector3.Distance(ch.localPosition,new Vector3(10,1,15)) > .01f) throw new Exception("No llega al destino.");
            nav.SelectDestination(-1); nav.SelectDestination(999); nav.Tick(0);
            File.WriteAllText("Library/ChuwiMapValidation.txt", "PASS: sin tracking no se mueve; seleccion; pausa; reanudacion; llegada; altura de pies; raiz rotada y escalada; indices invalidos.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
}
