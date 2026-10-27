using System;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 診断：フレームの速さをログ（Player.log）へ書く。起動引数に <c>-frameStats</c> があるときだけ動く。
    /// 遊びには一切触れない（時計も状態も変えない）。打包版で夜の灯り（点光源が多い）の重さを測るために置く。
    ///
    /// 10 秒ごとに「平均 fps・最も遅い一フレームの ms・遅い方から 1% の fps」と、そのときの昼夜を一行で書く。
    /// </summary>
    public sealed class ProtoFrameStats : MonoBehaviour
    {
        const string Flag = "-frameStats";
        const float Window = 10f;

        readonly float[] _frames = new float[4096];
        int _count;
        float _elapsed;
        ProtoDayNight _dayNight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) < 0)
            {
                return;
            }

            var go = new GameObject("FrameStats");
            DontDestroyOnLoad(go);
            go.AddComponent<ProtoFrameStats>();
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (_count < _frames.Length)
            {
                _frames[_count++] = dt;
            }

            _elapsed += dt;
            if (_elapsed < Window || _count == 0)
            {
                return;
            }

            Array.Sort(_frames, 0, _count);
            var slowest = _frames[_count - 1];
            var lowIndex = Mathf.Clamp(_count - 1 - _count / 100, 0, _count - 1);
            var avg = _count / _elapsed;
            if (_dayNight == null)
            {
                _dayNight = FindFirstObjectByType<ProtoDayNight>();
            }

            var night = _dayNight != null && _dayNight.Clock != null ? _dayNight.Clock.NightBlend01 : -1f;
            Debug.Log($"[FrameStats] avg {avg:F1} fps / worst {slowest * 1000f:F1} ms / 1% low {1f / _frames[lowIndex]:F1} fps / night {night:F2} / frames {_count}");
            _count = 0;
            _elapsed = 0f;
        }
    }
}
