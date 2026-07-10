using System;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// アイテムの恒久的な識別子。表示名とは別の概念であり、混同してはならない。
    /// 表示名は言語や演出の都合で変わりうるが、識別子はセーブデータの互換性を担う。
    /// </summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        readonly string _value;

        public ItemId(string value)
        {
            _value = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public string Value => _value ?? string.Empty;

        public bool IsEmpty => _value is null;

        public bool Equals(ItemId other) => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ItemId other && Equals(other);

        public override int GetHashCode() => _value?.GetHashCode() ?? 0;

        public override string ToString() => Value;

        public static bool operator ==(ItemId a, ItemId b) => a.Equals(b);

        public static bool operator !=(ItemId a, ItemId b) => !a.Equals(b);
    }
}
