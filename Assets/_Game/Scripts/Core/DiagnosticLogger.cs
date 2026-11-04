using System;
using UnityEngine;

namespace DungeonCrawler.Core
{
    public static class DiagnosticLogger
    {
        public enum Severity { Info, Warning, Error }
        public static event Action<Severity, string, string> MessageLogged;

        public static void Info(string system, string message) => Write(Severity.Info, system, message);
        public static void Warning(string system, string message) => Write(Severity.Warning, system, message);
        public static void Error(string system, string message) => Write(Severity.Error, system, message);

        private static void Write(Severity severity, string system, string message)
        {
            string category = string.IsNullOrWhiteSpace(system) ? "General" : system;
            string detail = string.IsNullOrWhiteSpace(message) ? "No details supplied" : message;
            string formatted = $"[{DateTime.UtcNow:O}] [{severity.ToString().ToUpperInvariant()}] [{category}] {detail}";
            switch (severity)
            {
                case Severity.Warning: Debug.LogWarning(formatted); break;
                case Severity.Error: Debug.LogError(formatted); break;
                default: Debug.Log(formatted); break;
            }
            MessageLogged?.Invoke(severity, category, detail);
        }
    }
}
