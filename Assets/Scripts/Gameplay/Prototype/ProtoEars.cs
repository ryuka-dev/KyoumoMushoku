using System.Collections;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// プレイヤーの耳。自分の身に起きたことの音（耳元で鳴る・立体音にしない）と、耳がこもる後処理を持つ。
    ///
    /// 捕まった一打は一つの抽象的な音では「何が起きたか」が伝わらなかった（作者の指摘）。三つを重ねて「掴まれて突き飛ばされた」と聞かせる：
    /// ①引っ張られる風切り（素材集の音の頭だけ、すぐ絞る）②体に当たる鈍い音（素材集の音を低く）③その後しばらく、世界の音がこもる
    /// （聞き手に低域だけを通す濾波器を掛ける＝頭を打って耳が遠くなる）。音は <see cref="SliceTuningAsset"/> の「警官の音」で差し替えられる。
    /// 時間が止まっている間（捕まって連れて行かれる演出）も鳴るよう、実時間で動く。
    /// </summary>
    public sealed class ProtoEars : MonoBehaviour
    {
        const float OpenCutoff = 22000f;
        const float MuffledCutoff = 650f;
        const float MuffleInSeconds = 0.18f;

        [SerializeField] SliceTuningAsset _slice;

        AudioSource _yank;
        AudioSource _hit;
        AudioLowPassFilter _filter;

        public void Configure(SliceTuningAsset slice) => _slice = slice;

        void Awake()
        {
            _yank = MakeSource();
            _hit = MakeSource();
        }

        AudioSource MakeSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>掴まれて突き飛ばされた音（風切り → 鈍い一打 → 耳がこもる）。こもりは <see cref="Unmuffle"/> まで続く。</summary>
        public void Grabbed()
        {
            StopAllCoroutines();
            StartCoroutine(GrabbedRoutine());
        }

        IEnumerator GrabbedRoutine()
        {
            var yankClip = _slice != null ? _slice.CaptureYank : null;
            var hitClip = _slice != null && _slice.CaptureHit != null ? _slice.CaptureHit : ProtoSounds.Thud;
            if (yankClip != null)
            {
                _yank.clip = yankClip;
                _yank.volume = _slice.CaptureYankVolume;
                _yank.pitch = 1f;
                _yank.Play();
                // 頭だけ鳴らして素早く絞る（長い尾を残すと、何の音か分からなくなる）。
                var seconds = _slice.CaptureYankSeconds;
                for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
                {
                    _yank.volume = _slice.CaptureYankVolume * (1f - Mathf.Clamp01((t - seconds * 0.5f) / (seconds * 0.5f)));
                    yield return null;
                }

                _yank.Stop();
            }

            _hit.clip = hitClip;
            _hit.volume = _slice != null ? _slice.CaptureHitVolume : 0.8f;
            _hit.pitch = _slice != null ? _slice.CaptureHitPitch : 1f;
            _hit.Play();

            // 当たった直後から、世界の音がこもる。
            var filter = Filter();
            if (filter != null)
            {
                for (var t = 0f; t < MuffleInSeconds; t += Time.unscaledDeltaTime)
                {
                    filter.cutoffFrequency = Mathf.Lerp(OpenCutoff, MuffledCutoff, t / MuffleInSeconds);
                    yield return null;
                }

                filter.cutoffFrequency = MuffledCutoff;
            }
        }

        /// <summary>こもった耳が、<paramref name="seconds"/> かけて戻る。</summary>
        public void Unmuffle(float seconds)
        {
            StopAllCoroutines();
            StartCoroutine(UnmuffleRoutine(seconds));
        }

        IEnumerator UnmuffleRoutine(float seconds)
        {
            var filter = Filter();
            if (filter == null)
            {
                yield break;
            }

            var from = filter.cutoffFrequency;
            for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                // 低域から少しずつ開く（耳が戻っていく）。
                filter.cutoffFrequency = Mathf.Lerp(from, OpenCutoff, (t / seconds) * (t / seconds));
                yield return null;
            }

            filter.cutoffFrequency = OpenCutoff;
        }

        void OnDisable()
        {
            if (_filter != null)
            {
                _filter.cutoffFrequency = OpenCutoff;
            }
        }

        /// <summary>聞き手（AudioListener）に付けた濾波器。聞き手と同じ物体に付けると、聞こえるすべての音に掛かる。</summary>
        AudioLowPassFilter Filter()
        {
            if (_filter != null)
            {
                return _filter;
            }

            var listener = FindFirstObjectByType<AudioListener>();
            if (listener == null)
            {
                return null;
            }

            _filter = listener.GetComponent<AudioLowPassFilter>();
            if (_filter == null)
            {
                _filter = listener.gameObject.AddComponent<AudioLowPassFilter>();
            }

            _filter.cutoffFrequency = OpenCutoff;
            return _filter;
        }
    }
}
