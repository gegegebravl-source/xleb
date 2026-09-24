using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace WarmBread
{
    public sealed class ReleaseDiagnostics : MonoBehaviour
    {
        private const long MaximumBytes = 2 * 1024 * 1024;
        private readonly object writeLock = new object();
        private string logPath;
        private string persistentDataPathCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Procedural diagnostics object is disabled to keep the project fully manual/editable.
        }

        private void Awake()
        {
            persistentDataPathCache = Application.persistentDataPath;
            var directory = Path.Combine(persistentDataPathCache, "Diagnostics");
            Directory.CreateDirectory(directory);
            logPath = Path.Combine(directory, "warm-bread.log");
            RotateIfNeeded();
            Application.logMessageReceivedThreaded += OnLog;
            Application.lowMemory += OnLowMemory;
            Append("START " + Application.productName + " " + Application.version + " | Unity " + Application.unityVersion + " | " + SystemInfo.operatingSystem + " | " + SystemInfo.graphicsDeviceName);
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            Application.lowMemory -= OnLowMemory;
        }

        private void OnLowMemory() { Append("WARNING low_memory"); }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && type != LogType.Warning) return;
            Append(type.ToString().ToUpperInvariant() + " " + Sanitize(condition) + (string.IsNullOrEmpty(stackTrace) ? string.Empty : "\n" + Sanitize(stackTrace)));
        }

        private void Append(string message)
        {
            try
            {
                lock (writeLock)
                {
                    File.AppendAllText(logPath, DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine, new UTF8Encoding(false));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace(persistentDataPathCache, "<save>").Replace(Application.dataPath, "<game>");
        }

        private void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(logPath) || new FileInfo(logPath).Length <= MaximumBytes) return;
                var previous = logPath + ".previous";
                if (File.Exists(previous)) File.Delete(previous);
                File.Move(logPath, previous);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
