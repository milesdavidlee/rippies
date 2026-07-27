using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Rippies.Reveal.Editor
{
    public static class IosLibraryExporter
    {
        private const string OutputArgument = "-rippiesOutput";

        public static void ExportSimulator()
        {
            Export(iOSSdkVersion.SimulatorSDK, "iOS-Simulator", simulatorArchitecture: 1);
        }

        public static void ExportDevice()
        {
            Export(iOSSdkVersion.DeviceSDK, "iOS-Device", simulatorArchitecture: null);
        }

        private static void Export(
            iOSSdkVersion sdkVersion,
            string defaultFolder,
            int? simulatorArchitecture)
        {
            string outputPath = GetOutputPath(defaultFolder);
            string originalIdentifier =
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            iOSSdkVersion originalSdkVersion = PlayerSettings.iOS.sdkVersion;
            string originalTargetVersion =
                PlayerSettings.iOS.targetOSVersionString;
            object originalSimulatorArchitecture =
                GetSimulatorArchitectureProperty()?.GetValue(null);
            BuildReport report;

            try
            {
                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, recursive: true);
                }
                Directory.CreateDirectory(outputPath);

                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.iOS,
                    BuildTarget.iOS);
                PlayerSettings.iOS.sdkVersion = sdkVersion;
                PlayerSettings.iOS.targetOSVersionString = "15.1";
                PlayerSettings.SetApplicationIdentifier(
                    NamedBuildTarget.iOS,
                    "com.rippies.reveal");

                if (simulatorArchitecture.HasValue)
                {
                    SetSimulatorArchitecture(simulatorArchitecture.Value);
                }

                string[] scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.iOS,
                    options = BuildOptions.None
                };

                Debug.Log(
                    $"[Rippies] Exporting {sdkVersion} Unity library to {outputPath}");
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                PlayerSettings.iOS.sdkVersion = originalSdkVersion;
                PlayerSettings.iOS.targetOSVersionString =
                    originalTargetVersion;
                PlayerSettings.SetApplicationIdentifier(
                    NamedBuildTarget.iOS,
                    originalIdentifier);
                PropertyInfo architectureProperty =
                    GetSimulatorArchitectureProperty();
                if (architectureProperty != null &&
                    originalSimulatorArchitecture != null)
                {
                    architectureProperty.SetValue(
                        null, originalSimulatorArchitecture);
                }
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Rippies iOS export failed: {report.summary.result} " +
                    $"({report.summary.totalErrors} errors)");
            }

            Debug.Log(
                $"[Rippies] iOS export complete: {report.summary.totalSize} bytes");
        }

        private static string GetOutputPath(string defaultFolder)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int argumentIndex = Array.IndexOf(arguments, OutputArgument);
            if (argumentIndex >= 0 && argumentIndex + 1 < arguments.Length)
            {
                return Path.GetFullPath(arguments[argumentIndex + 1]);
            }

            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Build", defaultFolder));
        }

        private static void SetSimulatorArchitecture(int rawValue)
        {
            PropertyInfo property = GetSimulatorArchitectureProperty();
            if (property == null)
            {
                Debug.LogWarning(
                    "[Rippies] Unity does not expose simulator architecture settings.");
                return;
            }

            object value = property.PropertyType.IsEnum
                ? Enum.ToObject(property.PropertyType, rawValue)
                : rawValue;
            property.SetValue(null, value);
        }

        private static PropertyInfo GetSimulatorArchitectureProperty()
        {
            Type iosSettings = typeof(PlayerSettings).GetNestedType(
                "iOS",
                BindingFlags.Public);
            return iosSettings?.GetProperty(
                "simulatorSdkArchitecture",
                BindingFlags.Public | BindingFlags.Static);
        }
    }
}
