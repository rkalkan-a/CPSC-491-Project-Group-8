
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;

public class SecurityScanner : EditorWindow
{
    private int filesScanned;
    private int findingsCount;
    private string scanResults = "No scan performed yet.";

    [MenuItem("Tools/Security Scanner")]
    public static void ShowWindow()
    {
        GetWindow<SecurityScanner>("Security Scanner");
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "SOC 2 CC7.1 - Security Scanner",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Scan Unity C# scripts for potentially " +
            "hardcoded passwords, API keys, and tokens.",
            MessageType.Info
        );

        if (GUILayout.Button("Scan Project", GUILayout.Height(35)))
        {
            ScanProject();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Files Scanned: " + filesScanned
        );

        EditorGUILayout.LabelField(
            "Potential Findings: " + findingsCount
        );

        EditorGUILayout.Space();

        EditorGUILayout.TextArea(
            scanResults,
            GUILayout.MinHeight(150)
        );
    }

    private void ScanProject()
    {
        filesScanned = 0;
        findingsCount = 0;
        scanResults = "";

        string assetsPath = Application.dataPath;

        string[] files = Directory.GetFiles(
            assetsPath,
            "*.cs",
            SearchOption.AllDirectories
        );

        string[] patterns =
        {
            @"\bpassword\s*=\s*""[^""]+""",
            @"\bapi[_-]?key\s*=\s*""[^""]+""",
            @"\b(?:auth[_-]?)?token\s*=\s*""[^""]+"""
        };

        foreach (string file in files)
        {
            filesScanned++;

            try
            {
                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (string pattern in patterns)
                    {
                        if (Regex.IsMatch(
                            lines[i],
                            pattern,
                            RegexOptions.IgnoreCase))
                        {
                            findingsCount++;

                            string relativePath =
                                "Assets" + file.Substring(
                                    assetsPath.Length
                                );

                            scanResults +=
                                relativePath +
                                " | Line " + (i + 1) +
                                " | Possible hardcoded credential\n";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "Could not scan " +
                    Path.GetFileName(file) +
                    ": " + ex.GetType().Name
                );
            }
        }

        if (findingsCount == 0)
        {
            scanResults =
                "No potential hardcoded credentials detected " +
                "by the configured patterns.";
        }

        Debug.Log(
            "CC7.1 Security Scan Complete. " +
            "Files: " + filesScanned +
            ", Findings: " + findingsCount
        );
    }
}
