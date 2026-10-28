using UnityEngine;

namespace KyoumoMushoku.Gameplay.App
{
    /// <summary>
    /// 音量とマウスの感度。<see cref="FrameRateSetting"/> と同じく、プレイヤーの好み（アプリ設定）であってセーブデータではないので、
    /// プラットフォームの設定領域（<see cref="PlayerPrefs"/>）に置く。ここがその唯一の所有者で、UI はここを通してのみ読み書きする。
    ///
    /// - 全体の音量：<see cref="AudioListener.volume"/> に掛ける。起動時に <see cref="ApplyOnBoot"/> が適用する。
    /// - 音楽の音量：BGM の大きさ（SliceTuning の値）に掛ける倍率。読むのは BGM の側。
    /// - マウスの感度：視点の回転（コンポーネントの既定値）に掛ける倍率。読むのは視点の側。
    /// 保存値は外から書き換えられうるので、読むたびに範囲へ丸める（CLAUDE.md 第十二条）。
    /// </summary>
    public static class PlayerOptions
    {
        const string MasterKey = "audio.master";
        const string MusicKey = "audio.music";
        const string LookKey = "input.lookSensitivity";

        public const float VolumeMin = 0f;
        public const float VolumeMax = 1f;
        public const float LookMin = 0.25f;
        public const float LookMax = 3f;

        public const float DefaultMaster = 0.8f;
        public const float DefaultMusic = 1f;
        public const float DefaultLook = 1f;

        public static float MasterVolume => Read(MasterKey, DefaultMaster, VolumeMin, VolumeMax);
        public static float MusicVolume => Read(MusicKey, DefaultMusic, VolumeMin, VolumeMax);
        public static float LookSensitivity => Read(LookKey, DefaultLook, LookMin, LookMax);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnBoot() => AudioListener.volume = MasterVolume;

        public static void SetMasterVolume(float value)
        {
            Write(MasterKey, Mathf.Clamp(value, VolumeMin, VolumeMax));
            AudioListener.volume = MasterVolume;
        }

        public static void SetMusicVolume(float value) => Write(MusicKey, Mathf.Clamp(value, VolumeMin, VolumeMax));

        public static void SetLookSensitivity(float value) => Write(LookKey, Mathf.Clamp(value, LookMin, LookMax));

        /// <summary>ポーズ画面を閉じるときに一度だけ書き出す（つまみを動かすたびに書かない）。</summary>
        public static void Save() => PlayerPrefs.Save();

        static float Read(string key, float fallback, float min, float max)
        {
            var value = PlayerPrefs.GetFloat(key, fallback);
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        }

        static void Write(string key, float value) => PlayerPrefs.SetFloat(key, value);
    }
}
