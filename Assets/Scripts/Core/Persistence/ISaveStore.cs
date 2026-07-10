namespace KyoumoMushoku.Core.Persistence
{
    /// <summary>
    /// セーブデータの読み書き口。ファイルシステムやシリアライズ形式は、この境界の内側に留める。
    /// </summary>
    public interface ISaveStore
    {
        bool Exists { get; }

        /// <summary>
        /// 読み込みに失敗しても既存のセーブデータを壊してはならない。
        /// 失敗の理由は <paramref name="error"/> に入れて呼び出し側に返す。
        /// </summary>
        bool TryLoad(out SaveGame save, out string error);

        void Save(SaveGame save);

        void Delete();
    }
}
