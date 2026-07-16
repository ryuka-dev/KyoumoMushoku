using UnityEngine;

namespace KyoumoMushoku.Gameplay.App
{
    /// <summary>
    /// フレーム上限の設定。プレイヤーの好み（アプリ設定）であり、ゲームのセーブデータとは別物なので、
    /// プラットフォームの設定領域（<see cref="PlayerPrefs"/>）に置く。ここがその唯一の所有者であり、
    /// PlayerPrefs・<see cref="Application.targetFrameRate"/>・<see cref="QualitySettings.vSyncCount"/> という
    /// プラットフォームの都合をこの境界の内側に閉じ込める。UI はここを通してのみ上限を読み書きする。
    ///
    /// 既定は 60 ロック。起動時に <see cref="ApplyOnBoot"/> が適用するので、ポーズ画面を開かなくても効く。
    /// 上限を効かせるには vSync を切る必要がある（vSync が有効だと targetFrameRate は無視される）ため、
    /// 適用のたびに vSyncCount を 0 にする。
    /// </summary>
    public static class FrameRateSetting
    {
        const string PrefKey = "display.frameRateLimit";

        /// <summary>無制限を表す上限値（<see cref="Application.targetFrameRate"/> の規約）。</summary>
        public const int Unlimited = -1;

        /// <summary>既定の上限。60 ロック。</summary>
        public const int Default = 60;

        /// <summary>選べる上限の一覧。UI はこの順で循環させる。無制限は末尾。</summary>
        public static readonly int[] Options = { 30, 60, 120, 144, Unlimited };

        /// <summary>いまの上限（PlayerPrefs に無ければ既定）。値は必ず <see cref="Options"/> のいずれか。</summary>
        public static int Current
        {
            get
            {
                var value = PlayerPrefs.GetInt(PrefKey, Default);
                return IsKnown(value) ? value : Default;
            }
        }

        /// <summary>
        /// 起動時に一度だけ、保存済み（無ければ既定 60）の上限を適用する。シーンの配線に依存せず、
        /// 最初のフレームより前に効かせるために使う。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnBoot() => Apply(Current);

        /// <summary>次の上限へ循環して切り替え、保存し、適用する。切り替え後の上限を返す。</summary>
        public static int Cycle()
        {
            var index = System.Array.IndexOf(Options, Current);
            var next = Options[(index < 0 ? 0 : index + 1) % Options.Length];
            Set(next);
            return next;
        }

        /// <summary>上限を設定し、保存し、適用する。未知の値は既定へ丸める。</summary>
        public static void Set(int limit)
        {
            var value = IsKnown(limit) ? limit : Default;
            PlayerPrefs.SetInt(PrefKey, value);
            PlayerPrefs.Save();
            Apply(value);
        }

        static void Apply(int limit)
        {
            // vSync が有効だと targetFrameRate は無視される。上限を確実に効かせるため切る。
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = limit;
        }

        static bool IsKnown(int value) => System.Array.IndexOf(Options, value) >= 0;
    }
}
