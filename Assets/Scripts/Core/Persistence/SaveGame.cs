using System;
using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Items;
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
        /// 版 2：Phase 3。警戒ゾーンごとの警戒度を加えた。版 1 は <see cref="SaveGameMigration"/> が引き上げる。
        /// </summary>
        public const int CurrentVersion = 2;

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

        public int WalletYen;

        /// <summary>目覚める就寝場所の識別子。</summary>
        public string SleepSpotId = string.Empty;
    }
}
