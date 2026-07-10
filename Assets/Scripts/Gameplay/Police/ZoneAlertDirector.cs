using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Zones;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Police
{
    /// <summary>
    /// <see cref="ZoneAlertLevels"/> をシーンに接続する唯一の場所であり、警戒度の所有者。
    /// 警官・就寝場所・保管庫（Phase 5）は、ここを読むか、ここに上昇を申告するだけである。
    ///
    /// 時間経過による減衰だけをここが駆動する。就寝（日付の切り替わり）による減衰は
    /// <c>GameSession</c> が就寝の瞬間に <see cref="BeginNextDay"/> を呼んで行う。
    /// 日付をポーリングしないのは、ロード時にも日付が変わって見え、二重に減衰してしまうためである。
    /// </summary>
    public sealed class ZoneAlertDirector : MonoBehaviour
    {
        public ZoneAlertLevels Levels { get; private set; }

        void Awake()
        {
            Levels ??= new ZoneAlertLevels();
        }

        void Update()
        {
            Levels.Decay(Time.deltaTime);
        }

        public float Level(AlertZoneId zone) => Levels.Level(zone);

        /// <summary>目立った。ゾーンの外（<see cref="AlertZoneId.None"/>）では何も起きない。</summary>
        public void Raise(AlertZoneId zone, float amount) => Levels.Raise(zone, amount);

        /// <summary>就寝して日付が変わった。<c>GameSession</c> だけが呼ぶ。</summary>
        public void BeginNextDay() => Levels.BeginNextDay();

        public ZoneAlertState CaptureState() => Levels.CaptureState();

        /// <summary>セーブデータから復元する。ロードの単一の所有者（<c>GameSession</c>）だけが呼ぶ。</summary>
        public void RestoreState(ZoneAlertState state) => Levels.Restore(state);
    }
}
