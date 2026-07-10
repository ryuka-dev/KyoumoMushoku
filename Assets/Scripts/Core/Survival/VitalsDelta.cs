using System;

namespace KyoumoMushoku.Core.Survival
{
    /// <summary>
    /// 4つの状態への増減。飲む・食べる・寝る・傷つく、いずれも同じ型で表す。
    /// 増減の意味（何がそれを引き起こしたか）は呼び出し側が持つ。
    /// </summary>
    [Serializable]
    public struct VitalsDelta
    {
        public float Hp;
        public float Thirst;
        public float Hunger;
        public float Sanity;

        public static VitalsDelta operator +(VitalsDelta a, VitalsDelta b) => new VitalsDelta
        {
            Hp = a.Hp + b.Hp,
            Thirst = a.Thirst + b.Thirst,
            Hunger = a.Hunger + b.Hunger,
            Sanity = a.Sanity + b.Sanity,
        };

        public bool IsZero => Hp == 0f && Thirst == 0f && Hunger == 0f && Sanity == 0f;
    }
}
