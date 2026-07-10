using System;
using System.Collections.Generic;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 就寝時オートセーブで永続化される持ち物。
    /// </summary>
    [Serializable]
    public sealed class InventoryState
    {
        /// <summary>カバンの総マス数。初期装備の `ボロい肩掛けカバン` は非常に狭い。</summary>
        public int Capacity = Inventory.DefaultCapacity;

        public List<ItemInstance> Items = new List<ItemInstance>();
    }
}
