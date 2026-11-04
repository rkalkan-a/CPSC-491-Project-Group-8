using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Core;

namespace DungeonCrawler.Tests
{
    public class DiagnosticLoggingTests
    {
        [Test]
        public void Info_EmitsCategorizedMessageAndEvent()
        {
            DiagnosticLogger.Severity receivedLevel = DiagnosticLogger.Severity.Error;
            string receivedSystem = null;
            string receivedMessage = null;
            Action<DiagnosticLogger.Severity, string, string> handler = (level, system, message) =>
            {
                receivedLevel = level;
                receivedSystem = system;
                receivedMessage = message;
            };
            DiagnosticLogger.MessageLogged += handler;
            try
            {
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[INFO\] \[Tests\] Test event$"));
                DiagnosticLogger.Info("Tests", "Test event");
                Assert.AreEqual(DiagnosticLogger.Severity.Info, receivedLevel);
                Assert.AreEqual("Tests", receivedSystem);
                Assert.AreEqual("Test event", receivedMessage);
            }
            finally { DiagnosticLogger.MessageLogged -= handler; }
        }

        [Test]
        public void Warning_UsesUnityWarningLevel()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[WARNING\] \[Tests\] Warning event$"));
            DiagnosticLogger.Warning("Tests", "Warning event");
        }

        [Test]
        public void Error_UsesUnityErrorLevel()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[ERROR\] \[Tests\] Error event$"));
            DiagnosticLogger.Error("Tests", "Error event");
        }

        [Test]
        public void EmptyCategoryAndMessage_UseSafeDefaults()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[INFO\] \[General\] No details supplied$"));
            DiagnosticLogger.Info(null, null);
        }
    }
}
