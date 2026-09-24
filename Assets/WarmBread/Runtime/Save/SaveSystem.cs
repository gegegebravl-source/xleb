using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace WarmBread
{
    public sealed class SaveSystem
    {
        public const int CurrentVersion = 1;
        private readonly string directory;

        public SaveSystem(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory is required.", nameof(directory));
            this.directory = directory;
        }

        public void Save<T>(string slot, T payload)
        {
            ValidateSlot(slot);
            Directory.CreateDirectory(directory);
            var payloadJson = JsonConvert.SerializeObject(payload);
            var envelope = new SaveEnvelope { Version = CurrentVersion, WrittenUtc = DateTime.UtcNow, Checksum = Hash(payloadJson), Payload = payloadJson };
            var json = JsonConvert.SerializeObject(envelope, Formatting.Indented);
            var path = Path.Combine(directory, "warm-bread-v" + CurrentVersion + "-" + slot + ".json");
            var temporary = path + ".tmp";
            var previous = path + ".previous";
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            if (File.Exists(path)) File.Copy(path, previous, true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        public bool TryLoad<T>(string slot, out T payload, out string error)
        {
            payload = default;
            error = null;
            try
            {
                ValidateSlot(slot);
                var path = Path.Combine(directory, "warm-bread-v" + CurrentVersion + "-" + slot + ".json");
                if (!File.Exists(path)) { error = "save_not_found"; return false; }
                var envelope = JsonConvert.DeserializeObject<SaveEnvelope>(File.ReadAllText(path));
                if (envelope == null || envelope.Version != CurrentVersion) { error = "save_version_unsupported"; return false; }
                if (!FixedEquals(envelope.Checksum, Hash(envelope.Payload))) { error = "save_checksum_invalid"; return false; }
                payload = JsonConvert.DeserializeObject<T>(envelope.Payload);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException || exception is ArgumentException)
            {
                error = "save_corrupted";
                return false;
            }
        }

        private static void ValidateSlot(string slot)
        {
            if (slot != "autosave" && slot != "manual") throw new ArgumentException("Only autosave and manual slots are allowed.", nameof(slot));
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        private static bool FixedEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            var different = 0;
            for (var i = 0; i < left.Length; i++) different |= left[i] ^ right[i];
            return different == 0;
        }

        [Serializable]
        private sealed class SaveEnvelope
        {
            public int Version;
            public DateTime WrittenUtc;
            public string Checksum;
            public string Payload;
        }
    }
}
