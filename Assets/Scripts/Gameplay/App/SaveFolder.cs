using UnityEngine;

namespace KyoumoMushoku.Gameplay.App
{
    /// <summary>
    /// セーブデータの置き場所を OS のファイルブラウザで開く。<see cref="Application.persistentDataPath"/> と
    /// <see cref="Application.OpenURL"/> というプラットフォームの都合をこの境界の内側に閉じ込める。
    ///
    /// セーブ本体の読み書きは <see cref="Persistence.FileSaveStore"/> が持つ。ここは「場所を開く」だけで、
    /// セーブデータには触れない。
    /// </summary>
    public static class SaveFolder
    {
        /// <summary>セーブデータが置かれるフォルダ。Unity が起動時に必ず作るので常に存在する。</summary>
        public static string Path => Application.persistentDataPath;

        /// <summary>
        /// フォルダを OS のファイルブラウザで開く。Windows / macOS ではエクスプローラ／Finder が開く。
        /// 環境によっては開けないことがあるため、成否を返す。
        /// </summary>
        public static bool Reveal()
        {
            var path = Path;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            Application.OpenURL("file://" + path);
            return true;
        }
    }
}
