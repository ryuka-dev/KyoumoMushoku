using KyoumoMushoku.Core.DayCycle;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.DayCycle
{
    /// <summary>
    /// <see cref="GameClock"/> を Unity のフレーム更新に接続する唯一の場所。
    /// 隠れたコールバックポンプやグローバルなライフサイクルは持たない。
    /// </summary>
    public sealed class GameClockDriver : MonoBehaviour
    {
        [SerializeField] DayScheduleAsset _schedule;

        public GameClock Clock { get; private set; }

        void Awake()
        {
            if (_schedule == null)
            {
                Debug.LogError($"{nameof(GameClockDriver)}: {nameof(DayScheduleAsset)} が割り当てられていない。", this);
                enabled = false;
                return;
            }

            Clock = new GameClock(_schedule.ToSchedule());
        }

        void Update()
        {
            Clock?.Advance(Time.deltaTime);
        }

        /// <summary>就寝して日付を進める。就寝場所を問わず、ここでオートセーブする（Phase 1）。</summary>
        public void Sleep()
        {
            Clock?.BeginNextDay();
        }
    }
}
