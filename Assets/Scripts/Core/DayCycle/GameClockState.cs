using System;

namespace KyoumoMushoku.Core.DayCycle
{
    /// <summary>
    /// 就寝時オートセーブで永続化される時計の状態。
    /// MonoBehaviour から分離した純粋データとして保つ。
    /// </summary>
    [Serializable]
    public sealed class GameClockState
    {
        /// <summary>ゲームデイ（1始まり）。</summary>
        public int Day = 1;

        /// <summary>その日、休憩地点を出てから経過した秒数。</summary>
        public float ElapsedInDay;
    }
}
