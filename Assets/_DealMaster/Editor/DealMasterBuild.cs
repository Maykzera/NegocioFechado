using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Configuração do projeto e builds para Windows e Android.
/// Menus: DealMaster > Configurar projeto / Build Windows / Build Android.
/// Também pode ser chamado em linha de comando:
///   Unity -batchmode -quit -projectPath . -executeMethod DealMasterBuild.BuildWindows
/// </summary>
public static class DealMasterBuild
{
    private const string BuildFolder = "Builds";

    [MenuItem("DealMaster/Configurar projeto", priority = 20)]
    public static void ConfigureProject()
    {
        PlayerSettings.companyName = "Negocio Fechado";
        PlayerSettings.productName = "DealMaster";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.negociofechado.dealmaster");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.negociofechado.dealmaster");

        // Android: ARM64 com IL2CPP, tela deitada.
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        SetActiveInputHandlingBoth();

        // A IA de verdade usa a internet; no Android a permissão precisa ser forçada.
        PlayerSettings.Android.forceInternetPermission = true;
        ConfigureAnthropicDefine();

        if (!File.Exists(DealMasterSceneBuilder.ScenePath)) DealMasterSceneBuilder.Build();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(DealMasterSceneBuilder.ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[DealMaster] Projeto configurado.");
    }

    [MenuItem("DealMaster/Build Windows", priority = 40)]
    public static void BuildWindows() =>
        Build(BuildTarget.StandaloneWindows64, Path.Combine(BuildFolder, "Windows", "DealMaster.exe"));

    [MenuItem("DealMaster/Build Android (APK)", priority = 41)]
    public static void BuildAndroid()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        Build(BuildTarget.Android, Path.Combine(BuildFolder, "Android", "DealMaster.apk"));
    }

    private static void Build(BuildTarget target, string output)
    {
        ConfigureProject();
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { DealMasterSceneBuilder.ScenePath },
            locationPathName = output,
            target = target,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[DealMaster] Build concluído: {output} ({report.summary.totalSize / (1024 * 1024)} MB)");
        else
            Debug.LogError($"[DealMaster] Build falhou: {report.summary.result} ({report.summary.totalErrors} erros)");

        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    public const string AnthropicDefine = "DEALMASTER_ANTHROPIC_SDK";

    /// <summary>
    /// Liga a IA de verdade quando o SDK oficial da Anthropic está no projeto (instalado pelo NuGetForUnity),
    /// e desliga quando não está, para o projeto sempre compilar.
    /// </summary>
    [MenuItem("DealMaster/Detectar SDK da IA (Anthropic)", priority = 21)]
    public static void ConfigureAnthropicDefine()
    {
        bool installed = false;
        foreach (var guid in AssetDatabase.FindAssets("Anthropic"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/Anthropic.dll", System.StringComparison.OrdinalIgnoreCase)) { installed = true; break; }
        }
        foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            var defines = new System.Collections.Generic.List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
            defines.RemoveAll(d => string.IsNullOrWhiteSpace(d) || d == AnthropicDefine);
            if (installed) defines.Add(AnthropicDefine);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        }
        Debug.Log(installed
            ? "[DealMaster] SDK da Anthropic encontrado: IA de verdade ativada. Coloque sua chave no computador do jogo (Configurações)."
            : "[DealMaster] SDK da Anthropic não encontrado: o jogo usa o cliente offline. Instale o pacote 'Anthropic' pelo NuGetForUnity para ativar a IA.");
    }

    /// <summary>
    /// O jogo usa o Input Manager clássico; "Both" mantém compatibilidade com o Input System.
    /// Não existe API pública para isso, então o valor é gravado direto no ProjectSettings.
    /// </summary>
    private static void SetActiveInputHandlingBoth()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        var prop = so.FindProperty("activeInputHandler");
        if (prop == null) return;
        if (prop.intValue != 2)
        {
            prop.intValue = 2;
            so.ApplyModifiedProperties();
            Debug.LogWarning("[DealMaster] 'Active Input Handling' alterado para 'Both'. Reinicie o Unity para aplicar.");
        }
    }
}
