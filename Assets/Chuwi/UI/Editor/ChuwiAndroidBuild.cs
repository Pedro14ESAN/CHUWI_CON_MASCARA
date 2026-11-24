using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menu Chuwi > Configurar Android / Generar APK Android.
/// Deja el proyecto listo para tablets Android (IL2CPP + ARM64, que exige Vuforia) y genera Builds/ChuwiAR.apk.
/// </summary>
public static class ChuwiAndroidBuild
{
    const string PackageId = "pe.edu.esan.chuwiar";
    const string IconPath = "Assets/Chuwi/UI/Icono/chuwi_icon_cabeza.png"; // ícono anterior: chuwi_icon.png
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string ApkPath = "Builds/ChuwiAR.apk";

    [MenuItem("Chuwi/Configurar Android")]
    public static void Configure()
    {
        PlayerSettings.companyName = "ESAN";
        PlayerSettings.productName = "Chuwi AR";
        PlayerSettings.bundleVersion = "1.0";
        if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

        var android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, PackageId);
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if ((int)PlayerSettings.Android.minSdkVersion < 29)
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29; // Vuforia exige Android 10+

        // Solo horizontal: el mapa se ve completo y la interfaz se adapta a celular o tablet
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        EditorUserBuildSettings.buildAppBundle = false; // APK, no AAB
        EditorUserBuildSettings.development = false;

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        SetIcon();
        AssetDatabase.SaveAssets();
        Debug.Log("[Chuwi] Android configurado: " + PackageId + ", IL2CPP, ARM64, minSdk " + (int)PlayerSettings.Android.minSdkVersion);
    }

    const string IconFrente = "Assets/Chuwi/UI/Icono/chuwi_icon_frente.png"; // capa delantera del ícono adaptativo
    const string IconFondo = "Assets/Chuwi/UI/Icono/chuwi_icon_fondo.png";   // capa de fondo del ícono adaptativo

    static Texture2D LoadIcon(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning("[Chuwi] No encontré el ícono en " + path); return null; }
        if (imp.textureType != TextureImporterType.Default || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed
            || !imp.alphaIsTransparency || imp.npotScale != TextureImporterNPOTScale.None)
        {
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void SetIcon()
    {
        var tex = LoadIcon(IconPath);
        if (tex == null) return;
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);

        // Android 8+ usa íconos adaptativos y redondos; si quedan vacíos, Unity pone su logo por defecto.
        var frente = LoadIcon(IconFrente); var fondo = LoadIcon(IconFondo);
        var android = NamedBuildTarget.Android;
        foreach (var kind in PlayerSettings.GetSupportedIconKinds(android))
        {
            var icons = PlayerSettings.GetPlatformIcons(android, kind);
            if (icons == null || icons.Length == 0) continue;
            string k = kind.ToString().ToLowerInvariant();
            foreach (var icon in icons)
            {
                if (k.Contains("adaptive"))
                {
                    if (frente == null || fondo == null) continue;
                    // capa 0 = fondo, capa 1 = frente
                    var layers = new Texture2D[icon.maxLayerCount];
                    layers[0] = fondo; if (layers.Length > 1) layers[1] = frente;
                    icon.SetTextures(layers);
                }
                else icon.SetTexture(tex); // Round y Legacy
            }
            PlayerSettings.SetPlatformIcons(android, kind, icons);
            Debug.Log("[Chuwi] Ícono Android " + kind + ": " + icons.Length + " tamaños");
        }
    }

    [MenuItem("Chuwi/Generar APK Android")]
    public static void BuildApk()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorUtility.DisplayDialog("Falta Android Build Support",
                "Abre Unity Hub > Installs > tu versión de Unity > Add modules y marca \"Android Build Support\" " +
                "(con OpenJDK y Android SDK & NDK Tools). Luego reinicia Unity y vuelve a usar este menú.", "Entendido");
            return;
        }

        Configure();
        WriteLog("Inicio. Plataforma activa: " + EditorUserBuildSettings.activeBuildTarget);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUtility.DisplayDialog("Cambiar a Android",
                "Unity va a cambiar la plataforma a Android. La primera vez tarda varios minutos. ¿Continuar?", "Sí", "Cancelar"))
                return;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };
        BuildReport report;
        try { report = BuildPipeline.BuildPlayer(options); }
        catch (System.Exception e) { WriteLog("EXCEPCION: " + e); Debug.LogException(e); return; }
        WriteReport(report);
        var s = report.summary;
        string full = Path.GetFullPath(ApkPath);
        if (s.result == BuildResult.Succeeded)
        {
            Debug.Log("[Chuwi] APK listo: " + full + " (" + (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " + s.totalTime.TotalMinutes.ToString("0.0") + " min)");
            EditorUtility.RevealInFinder(full);
        }
        else
        {
            Debug.LogError("[Chuwi] El APK falló: " + s.result + " con " + s.totalErrors + " errores. Revisa la consola.");
        }
    }

    const string LogPath = "Logs/chuwi_build.log";

    static void WriteLog(string text)
    {
        try { Directory.CreateDirectory("Logs"); File.AppendAllText(LogPath, System.DateTime.Now.ToString("HH:mm:ss") + " " + text + "\n"); } catch { }
    }

    static void WriteReport(BuildReport report)
    {
        var s = report.summary;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Resultado: " + s.result + " | errores " + s.totalErrors + " | advertencias " + s.totalWarnings + " | target " + s.platform + " | activo " + EditorUserBuildSettings.activeBuildTarget);
        foreach (var step in report.steps)
            foreach (var m in step.messages)
                if (m.type != LogType.Log)
                    sb.AppendLine("[" + step.name + "] " + m.type + ": " + m.content);
        WriteLog(sb.ToString());
    }
}
