using System;
using System.IO;
using KyoumoMushoku.Core.Persistence;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Persistence
{
    /// <summary>
    /// <see cref="ISaveStore"/> のファイルシステム実装。JsonUtility とファイル入出力という
    /// インフラの都合を、この境界の内側に閉じ込める。
    ///
    /// 読み込みに失敗しても既存のセーブを壊さない。書き込みは一時ファイル経由の差し替えにより、
    /// 途中で失敗しても半端なファイルを残さない（第十一節）。
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        readonly string _path;
        readonly string _tempPath;

        public FileSaveStore(string fileName = "save.json")
        {
            var directory = Application.persistentDataPath;
            _path = System.IO.Path.Combine(directory, fileName);
            _tempPath = _path + ".tmp";
        }

        public string FilePath => _path;

        public bool Exists => File.Exists(_path);

        public bool TryLoad(out SaveGame save, out string error)
        {
            save = null;

            if (!File.Exists(_path))
            {
                error = "セーブデータが存在しない。";
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(_path);
            }
            catch (Exception e)
            {
                error = $"セーブデータを読めなかった：{e.Message}";
                return false;
            }

            SaveGame parsed;
            try
            {
                parsed = JsonUtility.FromJson<SaveGame>(json);
            }
            catch (Exception e)
            {
                error = $"セーブデータを解釈できなかった：{e.Message}";
                return false;
            }

            // 古い版は検証の前に引き上げる。検証が見るのは常に現在の版である。
            if (!SaveGameMigration.TryUpgrade(parsed, out error))
            {
                return false;
            }

            if (!SaveGameValidation.TryValidate(parsed, out error))
            {
                return false;
            }

            save = parsed;
            return true;
        }

        public void Save(SaveGame save)
        {
            if (save is null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            var json = JsonUtility.ToJson(save, prettyPrint: true);

            // 一時ファイルへ書いてから差し替える。既存のセーブは、新しい書き込みが完了するまで無傷。
            File.WriteAllText(_tempPath, json);
            if (File.Exists(_path))
            {
                File.Replace(_tempPath, _path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(_tempPath, _path);
            }
        }

        public void Delete()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            if (File.Exists(_tempPath))
            {
                File.Delete(_tempPath);
            }
        }
    }
}
