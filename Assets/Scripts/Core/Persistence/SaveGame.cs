using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Persistence
{
    /// <summary>
    /// 就寝時オートセーブで永続化される全状態。就寝場所を問わず、就寝すると必ずここに書き出す。
    /// セーブ専用の場所や操作は設けない（第二節）。
    ///
    /// 再開地点は座標ではなく就寝場所の識別子で持つ。セーブは必ず就寝の瞬間に起こるため、
    /// 目覚める場所は常に就寝場所であり、地形を動かしてもセーブデータは壊れない。
    /// </summary>
    [Serializable]
    public sealed class SaveGame
    {
        /// <summary>
        /// 版 1：Phase 1〜2。時計・状態・カバン・所持金・就寝場所。
        /// 版 2：Phase 3。警戒ゾーンごとの警戒度を加えた。
        /// 版 3：Phase 5a。習得したコツと触発カウンタを加えた。
        /// 版 4：Phase 5b-1。背負いスロット（段ボール）を加えた。
        /// 版 5：Phase 5b-2。拠点に置いた保管庫（段ボール箱）を加えた。
        /// 版 6：Phase 5b-3。予告済みの保管庫イベントを加えた。古い版は <see cref="SaveGameMigration"/> が引き上げる。
        /// </summary>
        public const int CurrentVersion = 6;

        public const int OldestSupportedVersion = 1;

        public int Version = CurrentVersion;

        public GameClockState Clock = new GameClockState();
        public VitalsState Vitals = new VitalsState();
        public InventoryState Inventory = new InventoryState();

        /// <summary>
        /// 警戒度は日をまたいで残る（第五節）。同じベンチで寝続けると顔を覚えられる、という
        /// 仕様はここが永続しなければ成立しない。
        /// </summary>
        public ZoneAlertState ZoneAlerts = new ZoneAlertState();

        /// <summary>
        /// 習得したコツと触発カウンタ（第六節）。コツは失われないので日をまたいで残る。
        /// 版 2 以前は誰もコツを知らなかった状態として引き上げる。
        /// </summary>
        public KnackState Knacks = new KnackState();

        /// <summary>背負っている段ボール（第十一節）。担いだまま寝ることもあるため永続する。</summary>
        public CarrySlotState CarrySlot = new CarrySlotState();

        /// <summary>
        /// 拠点に置いた保管庫（第十二節）。貯め込んだ財産は日をまたいで残る。
        /// 未設置なら空。版 4 以前は保管庫という概念がまだ無かった状態として引き上げる。
        /// </summary>
        public List<StashState> Stashes = new List<StashState>();

        /// <summary>
        /// 予告済みで未発生の保管庫イベント（第十二節）。猶予中に終了・再開しても予告が消えないよう永続する。
        /// 当日の抽選結果そのものは一過性なので載せない。版 5 以前はイベントという概念がまだ無かった状態として引き上げる。
        /// </summary>
        public List<PendingStashEvent> PendingStashEvents = new List<PendingStashEvent>();

        public int WalletYen;

        /// <summary>目覚める就寝場所の識別子。</summary>
        public string SleepSpotId = string.Empty;
    }
}
