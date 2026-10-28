using System;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 診断：フレームの速さをログ（Player.log）へ書く。起動引数に <c>-frameStats</c> があるときだけ動く。
    /// 遊びには一切触れない（時計も状態も変えない）。打包版で夜の灯り（点光源が多い）の重さを測るために置く。
    ///
    /// 10 秒ごとに「平均 fps・最も遅い一フレームの ms・遅い方から 1% の fps」と、そのときの昼夜を一行で書く。
    ///
    /// <c>-frameStatsTour</c> も付けると、カメラだけを決まった道筋（大通り→アーケード→裏通り）で動かし続ける。
    /// 歩いて見回したときの重さを測るため。プレイヤーの体・時計・状態には触れない（描画の視点を上書きするだけ）。
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class ProtoFrameStats : MonoBehaviour
    {
        const string Flag = "-frameStats";
        const string TourFlag = "-frameStatsTour";
        const float Window = 10f;
        const float TourSpeed = 4f;

        // 目の高さの道筋（切片の街の座標）。最後の点から最初の点へ戻って繰り返す。
        static readonly Vector3[] TourPath =
        {
            new Vector3(-80f, 1.6f, 0f), new Vector3(40f, 1.6f, 0f), new Vector3(98f, 1.6f, 0f), new Vector3(103f, 1.6f, 2f),
            new Vector3(103f, 1.6f, 19.5f), new Vector3(30f, 1.6f, 19.5f), new Vector3(27f, 1.6f, 17.4f),
            new Vector3(-6.8f, 1.6f, 17.4f), new Vector3(-6.8f, 1.6f, 2f),
        };

        readonly float[] _frames = new float[4096];
        int _count;
        float _elapsed;
        ProtoDayNight _dayNight;
        bool _tour;
        float _tourDistance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) < 0)
            {
                return;
            }

            var go = new GameObject("FrameStats");
            DontDestroyOnLoad(go);
            go.AddComponent<ProtoFrameStats>()._tour = Array.IndexOf(Environment.GetCommandLineArgs(), TourFlag) >= 0;
        }

        void LateUpdate()
        {
            var cam = _tour ? Camera.main : null;
            if (cam == null)
            {
                return;
            }

            _tourDistance += TourSpeed * Time.unscaledDeltaTime;
            var remaining = _tourDistance;
            for (var lap = 0; lap < 2; lap++)
            {
                for (var i = 0; i < TourPath.Length; i++)
                {
                    var a = TourPath[i];
                    var b = TourPath[(i + 1) % TourPath.Length];
                    var length = Vector3.Distance(a, b);
                    if (remaining <= length)
                    {
                        cam.transform.SetPositionAndRotation(Vector3.Lerp(a, b, remaining / length), Quaternion.LookRotation(b - a));
                        return;
                    }

                    remaining -= length;
                }

                _tourDistance = remaining;
            }
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
