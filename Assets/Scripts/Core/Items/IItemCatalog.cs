namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// アイテム定義の参照先。定義がどこに保存されているか（ScriptableObject か、表か）を
    /// ゲームプレイ側に漏らさないための境界。
    /// </summary>
    public interface IItemCatalog
    {
        bool TryGet(ItemId id, out ItemDefinition definition);
    }
}
