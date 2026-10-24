#if UNITY_EDITOR_WIN
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

// Vuforia loads this driver by filename. Preload the package's exact DLL so
// Windows can resolve that filename even when the package is outside PATH.
[InitializeOnLoad]
internal static class ChuwiVuforiaDriverLoader
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryW(string fileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string moduleName);

    static ChuwiVuforiaDriverLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode ||
            GetModuleHandleW("UnityDriver.dll") != IntPtr.Zero) return;

        var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
            "Packages/com.ptc.vuforia.engine/package.json");
        if (package == null) return;

        var driverPath = Path.Combine(package.resolvedPath,
            "Vuforia", "Plugins", "Windows", "x64", "UnityDriver.dll");
        if (!File.Exists(driverPath))
        {
            Debug.LogError("Chuwi: no se encuentra el controlador de Vuforia: " + driverPath);
            return;
        }

        // Keep the module loaded for the editor process lifetime: Vuforia owns
        // native callbacks that may outlive a managed domain reload.
        if (LoadLibraryW(driverPath) == IntPtr.Zero)
            Debug.LogError("Chuwi: Windows no pudo cargar UnityDriver.dll. Error " +
                Marshal.GetLastWin32Error());
        else
            Debug.Log("Chuwi: UnityDriver.dll cargado desde el paquete Vuforia.");
    }
}
#endif
