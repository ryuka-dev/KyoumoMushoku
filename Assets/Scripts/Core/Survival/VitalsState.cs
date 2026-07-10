using System;

namespace KyoumoMushoku.Core.Survival
{
    /// <summary>
    /// 就寝時オートセーブで永続化される4つの状態。
    /// MonoBehaviour から分離した純粋データとして保つ。
    /// </summary>
    [Serializable]
    public sealed class VitalsState
    {
        public float Hp = 100f;
        public float Thirst = 100f;
        public float Hunger = 100f;
        public float Sanity = SanityScale.Max;
    }
}
