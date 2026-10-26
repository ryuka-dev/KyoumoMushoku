using System;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 街に響く物音。ゴミ箱を叩くたびに、叩いた箱が鳴らす（<see cref="ProtoTrashCan.Hit"/>）。警官が耳で拾う。
    /// 大きさは 0〜1。空の箱ほど甲高く響き、詰まった箱ほど鈍く小さい（叩いた反応と同じ事実から出す）。
    ///
    /// 鳴らす側は聞く側を知らない。聞く側（<see cref="ProtoPolice"/>）は有効な間だけ購読する。
    /// </summary>
    public static class ProtoNoise
    {
        public static event Action<Vector3, float> Made;

        public static void Emit(Vector3 position, float loudness)
        {
            if (loudness > 0f)
            {
                Made?.Invoke(position, Mathf.Clamp01(loudness));
            }
        }
    }
}
