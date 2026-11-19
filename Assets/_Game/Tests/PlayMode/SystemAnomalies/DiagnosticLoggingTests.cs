
using System;
using System.Text.RegularExpressions;
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
            DiagnosticLogger.Severity receivedLevel =
                DiagnosticLogger.Severity.Error;
            string receivedSystem = null;
            string receivedMessage = null;

            Action<DiagnosticLogger.Severity, string, string> handler =
                (level, system, message) =>
                {
                    receivedLevel = level;
                    receivedSystem = system;
                    receivedMessage = message;
                };

            DiagnosticLogger.MessageLogged += handler;

            try
            {
                LogAssert.Expect(LogType.Log,
                    new Regex(@"\[INFO\] \[Tests\] Test event$"));

                DiagnosticLogger.Info("Tests", "Test event");

                Assert.AreEqual(DiagnosticLogger.Severity.Info, receivedLevel);
                Assert.AreEqual("Tests", receivedSystem);
                Assert.AreEqual("Test event", receivedMessage);
            }
            finally
            {
                DiagnosticLogger.MessageLogged -= handler;
            }
        }

        [Test]
        public void Warning_UsesUnityWarningLevel()
        {
            LogAssert.Expect(LogType.Warning,
                new Regex(@"\[WARNING\] \[Tests\] Warning event$"));

            DiagnosticLogger.Warning("Tests", "Warning event");
        }

        [Test]
        public void Error_UsesUnityErrorLevel()
        {
            LogAssert.Expect(LogType.Error,
                new Regex(@"\[ERROR\] \[Tests\] Error event$"));

            DiagnosticLogger.Error("Tests", "Error event");
        }

        [Test]
        public void EmptyCategoryAndMessage_UseSafeDefaults()
        {
            LogAssert.Expect(LogType.Log,
                new Regex(@"\[INFO\] \[General\] No details supplied$"));

            DiagnosticLogger.Info(null, null);
        }

        // Verify that starting a run produces the correct diagnostic log.
        [Test]
        public void RunStarted_LogsCorrectMessage()
        {
            LogAssert.Expect(LogType.Log,
                new Regex(@"\[INFO\] \[Game\] Run started$"));

            GameEvents.RaiseRunStarted();
        }

        // Verify that resetting a run produces the correct diagnostic log.
        [Test]
        public void RunReset_LogsCorrectMessage()
        {
            LogAssert.Expect(LogType.Log,
                new Regex(@"\[INFO\] \[Game\] Run reset requested$"));

            GameEvents.RaiseRunReset();
        }

        // Verify that player death is recorded as a warning.
        [Test]
        public void PlayerDied_LogsWarning()
        {
            LogAssert.Expect(LogType.Warning,
                new Regex(@"\[WARNING\] \[Player\] Player died$"));

            GameEvents.RaisePlayerDied();
        }

        // Verify that clearing a room produces the correct diagnostic log.
        [Test]
        public void RoomCleared_LogsCorrectMessage()
        {
            LogAssert.Expect(LogType.Log,
                new Regex(@"\[INFO\] \[Rooms\] Room cleared$"));

            GameEvents.RaiseRoomCleared(null);
        }

        // Verify that one logging call raises exactly one event.
        [Test]
        public void DiagnosticEvent_FiresExactlyOnce()
        {
            int eventCount = 0;

            Action<DiagnosticLogger.Severity, string, string> handler =
                (level, system, message) =>
                {
                    if (system == "Tests" &&
                        message == "Single event")
                    {
                        eventCount++;
                    }
                };

            DiagnosticLogger.MessageLogged += handler;

            try
            {
                LogAssert.Expect(LogType.Log,
                    new Regex(@"\[INFO\] \[Tests\] Single event$"));

                DiagnosticLogger.Info("Tests", "Single event");

                Assert.AreEqual(1, eventCount,
                    "A single diagnostic logging call should raise exactly one event.");
            }
            finally
            {
                DiagnosticLogger.MessageLogged -= handler;
            }
        }

        // Verify that diagnostic logs contain a valid UTC timestamp.
        [Test]
        public void DiagnosticLog_ContainsValidTimestamp()
        {
            string capturedLog = null;

            Application.LogCallback handler =
                (condition, stackTrace, type) =>
                {
                    if (type == LogType.Log &&
                        condition.Contains("[Tests] Timestamp validation"))
                    {
                        capturedLog = condition;
                    }
                };

            Application.logMessageReceived += handler;

            try
            {
                LogAssert.Expect(LogType.Log,
                    new Regex(@"\[INFO\] \[Tests\] Timestamp validation$"));

                DiagnosticLogger.Info("Tests", "Timestamp validation");

                Assert.IsNotNull(capturedLog,
                    "The diagnostic log should be captured.");

                Match match = Regex.Match(capturedLog,
                    @"^\[([^\]]+)\] \[INFO\] \[Tests\] Timestamp validation$");

                Assert.IsTrue(match.Success,
                    "The log should contain a timestamp.");

                DateTimeOffset timestamp;

                Assert.IsTrue(
                    DateTimeOffset.TryParse(match.Groups[1].Value,
                        out timestamp),
                    "The timestamp should be a valid date and time.");

                Assert.AreEqual(TimeSpan.Zero, timestamp.Offset,
                    "The timestamp should use UTC.");
            }
            finally
            {
                Application.logMessageReceived -= handler;
            }
        }
    }
}
