using KyoumoMushoku.Gameplay.DayCycle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Scaffolding
{
    /// <summary>
    /// Phase 0 の足場。就寝場所がまだ存在しないため、N キーで日付を進める。
    /// 実際の就寝場所と就寝時オートセーブが入る Phase 1 で、このファイルごと削除する。
    /// </summary>
    public sealed class PhaseZeroSleepKey : MonoBehaviour
    {
        [SerializeField] GameClockDriver _clock;

        public void Configure(GameClockDriver clock) => _clock = clock;

        void Update()
        {
            if (_clock != null && Keyboard.current is { nKey: { wasPressedThisFrame: true } })
            {
                _clock.Sleep();
            }
        }
    }
}
