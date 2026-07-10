using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 目覚める場所。就寝場所（第二節）と病院（第三節）がこれを実装する。
    /// セーブは就寝の瞬間にしか起こらないため、再開時にプレイヤーが立つのは常に就寝場所であり、
    /// 座標ではなく識別子で復元できる。地形を動かしてもセーブデータは壊れない。
    /// </summary>
    public interface IRespawnPoint
    {
        /// <summary>恒久的な識別子。セーブデータに載る。</summary>
        string RespawnId { get; }

        /// <summary>ここで目覚めるときにプレイヤーが立つ位置。</summary>
        Vector3 SpawnPosition { get; }
    }
}
