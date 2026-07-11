using System;
using System.Collections.Generic;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 就寝時オートセーブで永続化される保管庫1つ（第十二節）。貯め込んだ財産は日をまたいで残る。
    ///
    /// どの拠点に置かれた保管庫かは <see cref="SpotId"/> で持つ。座標ではなく識別子で持つのは、
    /// 就寝場所（<c>SleepSpotId</c>）と同じ理由で、地形を動かしてもセーブが壊れないようにするためである。
    /// </summary>
    [Serializable]
    public sealed class StashState
    {
        /// <summary>この保管庫が置かれた拠点の識別子。ロード時にこの識別子で世界の中の設置場所を探す。</summary>
        public string SpotId = string.Empty;

        public StashKind Kind = StashKind.CardboardBox;

        /// <summary>保管庫の総マス数。</summary>
        public int Capacity = StashTuning.CardboardBoxCapacity;

        public List<ItemInstance> Items = new List<ItemInstance>();
    }
}
