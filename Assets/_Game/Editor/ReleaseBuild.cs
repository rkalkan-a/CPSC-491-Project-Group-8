using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DungeonCrawler.EditorTools
{
    // the build step of tools/release/build_release.sh. run from the command line with
    //   -executeMethod DungeonCrawler.EditorTools.ReleaseBuild.BuildMac -releaseOutput <folder> [-releaseVersion 1.2.3]
    // it builds every enabled scene in File > Build Settings (Main Menu first) into <folder>/CPSC491-Group8.app
    // and exits 0 on success, 1 on failure, so the script can stop. it doesn't change any project setting.
    public static class ReleaseBuild
    {
        public const string AppName = "CPSC491-Group8";

        public static void BuildMac()
        {
            int exitCode = 1;
            string versionBefore = PlayerSettings.bundleVersion;
            try
            {
                string output = Arg("-releaseOutput");
                if (string.IsNullOrEmpty(output))
                    throw new ArgumentException("missing -releaseOutput <folder>");

                string version = Arg("-releaseVersion");
                if (!string.IsNullOrEmpty(version)) PlayerSettings.bundleVersion = version;

                string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) throw new InvalidOperationException("no scenes are enabled in Build Settings");

                Directory.CreateDirectory(output);
                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(output, AppName + ".app"),
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.None,
                };

                Debug.Log($"[ReleaseBuild] building {PlayerSettings.bundleVersion} for macOS, scenes: {string.Join(", ", scenes)}");
                BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
                Debug.Log($"[ReleaseBuild] result {summary.result}, {summary.totalErrors} errors, {summary.totalWarnings} warnings, " +
                          $"{summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0}s");

                exitCode = summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[ReleaseBuild] " + e.Message);
            }
            finally
            {
                // the version is only for this build, the project keeps its own
                PlayerSettings.bundleVersion = versionBefore;
            }
            EditorApplication.Exit(exitCode);
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
