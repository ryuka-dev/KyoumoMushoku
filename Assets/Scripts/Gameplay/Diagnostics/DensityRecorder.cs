using System;
using System.IO;
using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Diagnostics;
using KyoumoMushoku.Gameplay.Foraging;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Shop;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Diagnostics
{
    /// <summary>
    /// 密度の計測（`Docs/作業計画.md` 第四節 M1）を、遊んでいるあいだに自動でとる。
    /// 純粋な積み上げは <see cref="DensityLog"/> が持ち、ここは Unity の都合——時計の観測、
    /// <see cref="PlayerInteractor"/> の購読、ファイル書き出し——だけを引き受ける。
    ///
    /// MonoBehaviour ではない。美術を置いた時点で本番シーンは手の所有物になり、ビルダーが
    /// 部品を足すことはできなくなった（CLAUDE.md「シーンの所有権」）。したがって既にシーンに
    /// 居る <see cref="PhaseZeroHud"/> が生成し、駆動し、捨てる。隠れたループもグローバルな
    /// ライフサイクルも持たない（CLAUDE.md 第13条）。
    ///
    /// これは診断である。読まれなくても遊びは同じように動く（CLAUDE.md 第7条）。
    /// </summary>
    public sealed class DensityRecorder : IDisposable
    {
        public const string FileName = "density-report.txt";

        readonly DensityLog _log = new DensityLog();

        PlayerInteractor _interactor;
        GameClock _clock;

        public DensityLog Log => _log;

        /// <summary>報告の置き場所。セーブと同じ場所なので、ポーズ画面の「セーブ場所を開く」で辿り着ける。</summary>
        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>行動の出所を購読する。二重購読しない。</summary>
        public void Bind(PlayerInteractor interactor)
        {
            if (ReferenceEquals(_interactor, interactor))
            {
                return;
            }

            if (_interactor != null)
            {
                _interactor.Acted -= OnActed;
            }

            _interactor = interactor;

            if (_interactor != null)
            {
                _interactor.Acted += OnActed;
            }
        }

        /// <summary>
        /// 毎フレーム呼ぶ。時計インスタンスはロードで差し替わるため、掴んだまま持たず毎回渡す。
        /// </summary>
        public void Tick(GameClock clock)
        {
            _clock = clock;
            if (clock == null)
            {
                return;
            }

            // Time.time は timeScale に従うので、ポーズ中は進まない。ポーズは実時間に数えない。
            _log.Observe(clock.Day, clock.ElapsedInDay, Time.time);
        }

        /// <summary>報告を書き出す。書けたらそのパスを返し、書けなければ null。</summary>
        public string Dump()
        {
            var path = Path;

            try
            {
                File.WriteAllText(path, _log.Format(DateTime.Now));
            }
            catch (Exception error)
            {
                Debug.LogWarning($"{nameof(DensityRecorder)}: {path} / {error.Message}");
                return null;
            }

            return path;
        }

        public void Dispose() => Bind(null);

        void OnActed(IInteractable target)
        {
            if (_clock == null)
            {
                return;
            }

            _log.Record(Classify(target), _clock.Phase == DayPhase.Night, _clock.ElapsedInDay);
        }

        /// <summary>
        /// 相手の型で分類する。型そのものが「何をしたか」の canonical な出所であり、
        /// 名前やシーンの当て推量には頼らない（CLAUDE.md 第6条）。
        /// </summary>
        static ActKind Classify(IInteractable target) => target switch
        {
            TrashCan => ActKind.Forage,
            Storefront => ActKind.Shop,
            SleepSpot => ActKind.Sleep,
            WaterSource => ActKind.Water,
            StashSpot => ActKind.Stash,
            ItemPickup => ActKind.Pickup,
            Hospital => ActKind.Hospital,
            Stairwell => ActKind.Transit,
            _ => ActKind.Other,
        };
    }
}
