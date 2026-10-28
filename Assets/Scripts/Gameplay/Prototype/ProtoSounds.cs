using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 試作用の効果音をその場で合成する（音素材を持たない灰箱のため）。
    /// 本採用になれば、本物の音素材に差し替える。
    /// </summary>
    public static class ProtoSounds
    {
        const int SampleRate = 44100;

        static AudioClip _thud;
        static AudioClip _clang;
        static AudioClip _pop;
        static AudioClip _grab;
        static AudioClip _chime;
        static AudioClip _beep;
        static AudioClip _jackpot;

        /// <summary>中身の詰まった鈍い音。</summary>
        public static AudioClip Thud => _thud ??= Noise("ProtoThud", 0.22f, decay: 18f, lowPass: 0.06f, tone: 70f, toneMix: 0.6f);

        /// <summary>空洞の金属音。空振りの箱はこう鳴る。</summary>
        public static AudioClip Clang => _clang ??= Noise("ProtoClang", 0.45f, decay: 7f, lowPass: 0.5f, tone: 520f, toneMix: 0.5f);

        /// <summary>ふたが弾けて中身が飛ぶ音。</summary>
        public static AudioClip Pop => _pop ??= Noise("ProtoPop", 0.18f, decay: 25f, lowPass: 0.25f, tone: 180f, toneMix: 0.3f);

        /// <summary>掴んだ音。</summary>
        public static AudioClip Grab => _grab ??= Noise("ProtoGrab", 0.12f, decay: 30f, lowPass: 0.35f, tone: 900f, toneMix: 0.4f);

        /// <summary>珍しい物のきらめき。</summary>
        public static AudioClip Chime => _chime ??= Noise("ProtoChime", 0.6f, decay: 5f, lowPass: 1f, tone: 1320f, toneMix: 0.95f);

        /// <summary>大当たりの「キュイン」（パチンコの当たりの上がる音）。短く一度だけ。</summary>
        public static AudioClip Jackpot => _jackpot ??= Sweep("ProtoJackpot", 0.45f, 700f, 2600f);

        /// <summary>レジのバーコードを読んだ音。</summary>
        public static AudioClip Beep => _beep ??= Noise("ProtoBeep", 0.1f, decay: 12f, lowPass: 1f, tone: 2000f, toneMix: 0.98f);

        /// <summary>音の高さが下から上へ滑る音。細かく揺らして電子音らしくする。</summary>
        static AudioClip Sweep(string name, float seconds, float fromHz, float toHz)
        {
            var length = Mathf.CeilToInt(SampleRate * seconds);
            var data = new float[length];
            var phase = 0f;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var k = t / seconds;
                // 前半で一気に上がり、後半は高いところで鳴り続ける。
                var hz = Mathf.Lerp(fromHz, toHz, 1f - (1f - k) * (1f - k)) * (1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 30f * t));
                phase += 2f * Mathf.PI * hz / SampleRate;
                var envelope = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((seconds - t) / 0.12f);
                data[i] = (Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.2f) * envelope * 0.6f;
            }

            var clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(string name, float seconds, float decay, float lowPass, float tone, float toneMix)
        {
            var length = Mathf.CeilToInt(SampleRate * seconds);
            var data = new float[length];
            var random = new System.Random(name.GetHashCode());
            var filtered = 0f;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                filtered += (noise - filtered) * lowPass;
                var sine = Mathf.Sin(2f * Mathf.PI * tone * t);
                var envelope = Mathf.Exp(-decay * t);
                data[i] = Mathf.Lerp(filtered * 2f, sine, toneMix) * envelope * 0.8f;
            }

            var clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
