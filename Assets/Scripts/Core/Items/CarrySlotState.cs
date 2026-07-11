using System;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 就寝時オートセーブで永続化される背負いスロット（第十一節）。段ボールを担いだまま寝ることもある。
    /// </summary>
    [Serializable]
    public sealed class CarrySlotState
    {
        public bool Occupied;
        public ItemInstance Item;
    }
}
