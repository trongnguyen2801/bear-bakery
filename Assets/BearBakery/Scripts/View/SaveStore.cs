// Local JSON save with temp-write + backup, version check and corruption handling.
using System;
using System.IO;
using BB.Model;
using UnityEngine;

namespace BB.View
{
    public static class SaveStore
    {
        public enum Status { NewGame, Loaded, LoadedFromBackup, CorruptStartedNew, NewerVersionReadOnly, Migrated }
        public static string Slot = "save";
        public static bool ReadOnly;                 // set when the file is from a newer build: never overwrite it
        public static string LastError = "";
        public static string Dir => Application.persistentDataPath;
        public static string MainPath => Path.Combine(Dir, Slot + ".json");
        public static string BackupPath => MainPath + ".bak";

        [Serializable] class VersionProbe { public int saveVersion; }

        public static double UnixNow() => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        public static SaveData Load(out Status status, out string message)
        {
            message = ""; status = Status.NewGame; ReadOnly = false;
            if (!File.Exists(MainPath) && !File.Exists(BackupPath)) return null;
            var d = TryRead(MainPath, out int ver, out string err);
            if (ver > SaveData.CurrentVersion)
            {
                ReadOnly = true;
                status = Status.NewerVersionReadOnly;
                message = "Bản lưu được tạo bởi phiên bản mới hơn. Game chạy chế độ không ghi để không làm hỏng tiến trình.";
                return null;
            }
            if (d != null)
            {
                status = ver < SaveData.CurrentVersion ? Status.Migrated : Status.Loaded;
                Migrate(d, ver);
                return d;
            }
            var b = TryRead(BackupPath, out int bver, out string berr);
            if (b != null && bver <= SaveData.CurrentVersion)
            {
                Migrate(b, bver);
                status = Status.LoadedFromBackup;
                message = "Bản lưu chính bị lỗi, đã khôi phục từ bản sao lưu.";
                return b;
            }
            // both unreadable: keep the broken file aside, never silently delete it
            try
            {
                if (File.Exists(MainPath)) File.Copy(MainPath, Path.Combine(Dir, $"{Slot}_corrupt_{DateTime.Now:yyyyMMdd_HHmmss}.json"), true);
            }
            catch (Exception e) { LastError = e.Message; }
            status = Status.CorruptStartedNew;
            message = "Không đọc được bản lưu (" + err + "). Đã giữ lại tệp lỗi và bắt đầu tiệm mới.";
            return null;
        }

        static SaveData TryRead(string path, out int version, out string error)
        {
            version = 0; error = "";
            try
            {
                if (!File.Exists(path)) { error = "missing"; return null; }
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) { error = "empty/invalid"; return null; }
                var probe = JsonUtility.FromJson<VersionProbe>(json);
                version = probe.saveVersion;
                if (version > SaveData.CurrentVersion) return null;
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null || d.boardSlots == null) { error = "parse"; return null; }
                return d;
            }
            catch (Exception e) { error = e.GetType().Name; return null; }
        }

        static void Migrate(SaveData d, int from)
        {
            // v1 -> v2: lastSimAt introduced; older saves only had lastSavedAt.
            if (from < 2 && d.lastSimAt <= 0) d.lastSimAt = d.lastSavedAt;
            d.saveVersion = SaveData.CurrentVersion;
        }

        public static bool Save(SaveData d)
        {
            if (ReadOnly || d == null) return false;
            try
            {
                Directory.CreateDirectory(Dir);
                string json = JsonUtility.ToJson(d);
                string tmp = MainPath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(MainPath)) File.Copy(MainPath, BackupPath, true);
                if (File.Exists(MainPath)) File.Delete(MainPath);
                File.Move(tmp, MainPath);
                LastError = "";
                return true;
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("Save failed: " + e); return false; }
        }

        public static void DeleteAll()
        {
            try { if (File.Exists(MainPath)) File.Delete(MainPath); if (File.Exists(BackupPath)) File.Delete(BackupPath); }
            catch (Exception e) { LastError = e.Message; }
        }
    }
}
