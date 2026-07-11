using System;
using System.Linq;
using KyoumoMushoku.Core.DayCycle;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.DayCycle
{
    /// <summary>
    /// ゲームデイごとの時間帯の長さ。すべて叩き台であり、プレイテストで調整する。
    /// </summary>
    [CreateAssetMenu(fileName = "DaySchedule", menuName = "KyoumoMushoku/Day Schedule")]
    public sealed class DayScheduleAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Min(0.01f)] public float dayMinutes;
            [Min(0.01f)] public float duskMinutes;
        }

        [SerializeField]
        [Tooltip("1日目から順に。表にない日は最終行の値を使う。")]
        Entry[] _days =
        {
            new Entry { dayMinutes = 2.5f, duskMinutes = 1f },
            new Entry { dayMinutes = 3f, duskMinutes = 1f },
            new Entry { dayMinutes = 4.5f, duskMinutes = 1.5f },
        };

        public DaySchedule ToSchedule() => new DaySchedule(
            _days.Select(e => new DayPhaseDurations(e.dayMinutes * 60f, e.duskMinutes * 60f)));
    }
}
