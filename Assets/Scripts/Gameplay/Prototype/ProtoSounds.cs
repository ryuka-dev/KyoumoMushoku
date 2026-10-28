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
        public static AudioClip Jackpot => _jackpot ??= Kyuin("ProtoJackpot");

        /// <summary>レジのバーコードを読んだ音。</summary>
        public static AudioClip Beep => _beep ??= Noise("ProtoBeep", 0.1f, decay: 12f, lowPass: 1f, tone: 2000f, toneMix: 0.98f);

        static AudioClip _step;
        static AudioClip _voice;

        /// <summary>靴音（追ってくる足音）。素材集に足音が無いための代用。柔らかく鈍い、短い音。</summary>
        public static AudioClip Step => _step ??= Noise("ProtoStep", 0.08f, decay: 45f, lowPass: 0.08f, tone: 90f, toneMix: 0.3f);

        /// <summary>警官が口を開いた合図の短い声（言葉ではない、低いうなり）。どちらから声がしたかを耳で知らせる。</summary>
        public static AudioClip Voice => _voice ??= Noise("ProtoVoice", 0.16f, decay: 14f, lowPass: 0.12f, tone: 190f, toneMix: 0.7f);

        /// <summary>
        /// 合成の「キュイン」（素材が入るまでの代用）。0.12 秒で一気に駆け上がり、上で細かく揺れながら伸び、こだまが残る。
        /// わずかにずらした二つの声を重ねて、うなり（電子音の厚み）を作る。
        /// </summary>
        static AudioClip Kyuin(string name)
        {
            const float seconds = 1.3f;
            var length = Mathf.CeilToInt(SampleRate * seconds);
            var dry = new float[length];
            var phaseA = 0f;
            var phaseB = 0f;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var rise = Mathf.Clamp01(t / 0.12f);
                var hz = Mathf.Lerp(380f, 1560f, 1f - (1f - rise) * (1f - rise) * (1f - rise))
                         * (1f + 0.035f * Mathf.Sin(2f * Mathf.PI * 11f * t) * Mathf.Clamp01((t - 0.1f) * 8f))
                         * (1f + 0.04f * Mathf.Clamp01((t - 0.6f) / 0.6f));
                phaseA += 2f * Mathf.PI * hz / SampleRate;
                phaseB += 2f * Mathf.PI * hz * 1.012f / SampleRate;
                // 奇数倍音を足した角の立った波（矩形寄り）。
                var a = Mathf.Sin(phaseA) + Mathf.Sin(phaseA * 3f) / 3f + Mathf.Sin(phaseA * 5f) / 5f;
                var b = Mathf.Sin(phaseB) + Mathf.Sin(phaseB * 3f) / 3f;
                var envelope = Mathf.Clamp01(t / 0.01f) * (t < 0.75f ? 1f : Mathf.Clamp01((seconds - 0.25f - t) / 0.3f));
                dry[i] = (a * 0.6f + b * 0.4f) * envelope * 0.35f;
            }

            // こだま（90ms おき・減衰 0.4）。
            var data = new float[length];
            var delay = Mathf.RoundToInt(SampleRate * 0.09f);
            for (var i = 0; i < length; i++)
            {
                data[i] = dry[i] + (i >= delay ? data[i - delay] * 0.4f : 0f);
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
