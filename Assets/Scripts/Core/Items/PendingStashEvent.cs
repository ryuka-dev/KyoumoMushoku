using System;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 予告済みで、まだ発生していない保管庫イベント（第十二節）。抽選は日境界で行い、ちょうど1日前に予告する。
    /// 予告済みのイベントはセーブに載る（猶予中に終了・再開しても予告が消えない）。当日の抽選結果そのものは
    /// 一過性なので載せない――載せるのは「予告済みで未発生」のものだけである。
    /// </summary>
    [Serializable]
    public sealed class PendingStashEvent
    {
        /// <summary>どの保管庫を脅かすか（設置場所の識別子）。</summary>
        public string SpotId = string.Empty;

        public StashEventKind Kind = StashEventKind.None;

        /// <summary>このゲームデイの朝（就寝の瞬間）に発生する。予告はこの前日いっぱい世界に出る。</summary>
        public int TriggerDay;
    }
}
