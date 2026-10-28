using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class ChuwiKeyboardSetup
{
    const string Folder = "Assets/Chuwi";
    const string Stamp = "Library/ChuwiKeyboardInstalled_v1.txt";

    // Auto-arranque desactivado: el candado vive en Library/ (Unity lo regenera), asi que se re-ejecutaba indefinidamente y modificaba assets del equipo sin confirmacion. Usar el menu.
    // [InitializeOnLoadMethod]
    static void InstallOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Stamp) && !EditorApplication.isPlayingOrWillChangePlaymode)
                Install();
        };
    }

    [MenuItem("Chuwi/Activar control por teclado")]
    public static void Install()
    {
        string prefabPath = Folder + "/Chuwi.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;
        Directory.CreateDirectory(Folder + "/Animations");
        string idlePath = Folder + "/Animations/Idle.anim";
        AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
        if (idle == null)
        {
            var greeting = AssetDatabase.LoadAllAssetsAtPath(Folder + "/Chuwi_Unity.fbx")
                .OfType<AnimationClip>().First(c => c.name == "Greeting");
            idle = new AnimationClip { name = "Idle", frameRate = 30 };
            foreach (var binding in AnimationUtility.GetCurveBindings(greeting))
            {
                if (binding.type != typeof(Transform)) continue;
                var curve = AnimationUtility.GetEditorCurve(greeting, binding);
                float value = curve.Evaluate(0);
                AnimationUtility.SetEditorCurve(idle, binding, AnimationCurve.Constant(0, 1, value));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(idle);settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idle, settings);
            AssetDatabase.CreateAsset(idle, idlePath);
        }
        string controllerPath = Folder + "/Chuwi_Keyboard.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            AssetDatabase.CopyAsset(Folder + "/Chuwi.controller", controllerPath);
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var walk = machine.states.First(s => s.state.name == "Walk").state;
            var rest = machine.AddState("Idle");rest.motion = idle;machine.defaultState = rest;
            Transition(rest, walk, AnimatorConditionMode.If, false);
            Transition(walk, rest, AnimatorConditionMode.IfNot, false);
            foreach (var item in machine.states.Where(s => s.state.name == "Greeting" || s.state.name == "Pointing"))
            {
                foreach (var exit in item.state.transitions.Where(t => t.destinationState == walk))
                    exit.AddCondition(AnimatorConditionMode.If, 0, "Moving");
                Transition(item.state, rest, AnimatorConditionMode.IfNot, true);
            }
        }
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var animator = root.GetComponent<Animator>();animator.runtimeAnimatorController = controller;animator.applyRootMotion = false;
            if (root.GetComponent<ChuwiKeyboardController>() == null) root.AddComponent<ChuwiKeyboardController>();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Stamp, "WASD/flechas; Idle/Walk; " + DateTime.UtcNow.ToString("O"));
        Debug.Log("CHUWI_KEYBOARD_READY: WASD y flechas. Pulsa Play y haz clic en Game.");
    }

    static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, bool exitTime)
    {
        var transition = from.AddTransition(to);transition.hasExitTime = exitTime;transition.exitTime = 1;
        transition.duration = .12f;transition.AddCondition(mode, 0, "Moving");
    }
}
