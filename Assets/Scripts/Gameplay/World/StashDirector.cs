using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Gameplay.Police;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 保管庫イベント（第十二節）の所有者。抽選そのものは Core（<see cref="StashEventRoll"/>）に閉じ、
    /// ここは日境界での進行――予告済みイベントの発生と、翌日ぶんの新規抽選――を束ねる。
    ///
    /// 抽選は就寝＝日境界で行い、ちょうど1日前に予告する。予告済みで未発生のイベントは永続し
    /// （<c>GameSession</c> がセーブへ束ねる）、当日の抽選結果そのものは一過性なので永続しない。
    /// 予告と事後説明は世界の中のラベル（<see cref="StashSpot"/> の貼り紙）で示す（第十四節）。
    /// </summary>
    public sealed class StashDirector : MonoBehaviour
    {
        readonly List<PendingStashEvent> _pending = new();

        ZoneAlertDirector _alerts;
        IRng _rng;

        void Awake()
        {
            _alerts ??= FindFirstObjectByType<ZoneAlertDirector>();
            _rng ??= new SystemRng();
        }

        /// <summary>診断・テスト用に乱数源を差し替える。実機では既定の <see cref="SystemRng"/> を使う。</summary>
        public void ConfigureRng(IRng rng) => _rng = rng;

        /// <summary>
        /// 指定した設置場所の予告を取り下げる（保管庫が回収・撤去されて世界から消えたとき・第十二節）。
        /// 予告の対象がもう無いので、翌日その予告が別の（新しい）箱に降りかからないようにする。
        /// </summary>
        public void ClearPendingFor(string spotId) =>
            _pending.RemoveAll(p => p != null && p.SpotId == spotId);

        /// <summary>
        /// 就寝して日付が変わった。<c>GameSession</c> だけが、警戒度の日次減衰と無料就寝の上昇を済ませたあとに呼ぶ。
        /// まず今日ぶんの予告済みイベントを発生させ、次に翌日ぶんを抽選して予告する。
        /// </summary>
        public void BeginNextDay(int currentDay)
        {
            var spots = FindSpotsById();
            var fired = FireDue(currentDay, spots);
            RollForTomorrow(currentDay, spots);
            TickRent(spots);
            RefreshNotices(spots, fired);
        }

        /// <summary>今日発生する予告を実行し、_pending から取り除く。世界に無い設置場所ぶんは捨てる。</summary>
        Dictionary<string, (StashEventKind Kind, int Lost)> FireDue(
            int currentDay, Dictionary<string, StashSpot> spots)
        {
            var fired = new Dictionary<string, (StashEventKind, int)>();
            var kept = new List<PendingStashEvent>();

            foreach (var pending in _pending)
            {
                if (pending is null || !spots.TryGetValue(pending.SpotId, out var spot) || !spot.HasStash)
                {
                    continue; // 設置場所が無い／空 → 捨てる
                }

                if (pending.TriggerDay > currentDay)
                {
                    kept.Add(pending); // まだ先の予告 → 残す
                    continue;
                }

                var lost = spot.ApplyEventLoss(pending.Kind, _rng);
                if (pending.Kind == StashEventKind.PoliceRemoval)
                {
                    _alerts?.Raise(spot.Zone, StashEventTuning.PoliceRemovalAlertRaise);
                }

                // 先輩ホームレスが次に箱を開けたとき語れるよう、起きたことを覚えさせる（第十四節）。
                spot.QueueAftermath(pending.Kind, lost);
                fired[pending.SpotId] = (pending.Kind, lost);
            }

            _pending.Clear();
            _pending.AddRange(kept);
            return fired;
        }

        /// <summary>まだ予告を持たない各保管庫について、翌日ぶんを抽選する。</summary>
        void RollForTomorrow(int currentDay, Dictionary<string, StashSpot> spots)
        {
            foreach (var spot in spots.Values)
            {
                if (!spot.HasStash || HasPending(spot.StashSpotId))
                {
                    continue;
                }

                var alert = _alerts != null ? _alerts.Level(spot.Zone) : 0f;
                // 場所代を払った箱は安全性が上がり、発生確率が下がる（第十二節・StashSafety）。
                var kind = StashEventRoll.Roll(_rng, alert, spot.StashUsedSlots, spot.EventChanceMultiplier);
                if (kind != StashEventKind.None)
                {
                    _pending.Add(new PendingStashEvent
                    {
                        SpotId = spot.StashSpotId,
                        Kind = kind,
                        TriggerDay = currentDay + 1,
                    });
                }
            }
        }

        /// <summary>
        /// 翌日ぶんの抽選のあと、各保管庫の場所代の効果を1日ぶん減らす。抽選は払い済みの安全性を読んだ
        /// あとにこれで切れるので、払った効果はちょうどその夜の抽選に効き、翌日には切れる（＝1日ぶん）。
        /// </summary>
        void TickRent(Dictionary<string, StashSpot> spots)
        {
            foreach (var spot in spots.Values)
            {
                if (spot.HasStash)
                {
                    spot.TickRentDay();
                }
            }
        }

        /// <summary>各設置場所のラベルを、予告（あれば）→事後説明（今日発生したら）→無し の順で決める。</summary>
        void RefreshNotices(
            Dictionary<string, StashSpot> spots, Dictionary<string, (StashEventKind Kind, int Lost)> fired)
        {
            foreach (var spot in spots.Values)
            {
                if (TryGetPending(spot.StashSpotId, out var pending))
                {
                    spot.ShowForecast(pending.Kind);
                }
                else if (fired != null && fired.TryGetValue(spot.StashSpotId, out var f))
                {
                    spot.ShowAftermath(f.Kind, f.Lost);
                }
                else
                {
                    spot.ClearNotice();
                }
            }
        }

        Dictionary<string, StashSpot> FindSpotsById()
        {
            var map = new Dictionary<string, StashSpot>();
            foreach (var spot in FindObjectsByType<StashSpot>(FindObjectsSortMode.None))
            {
                map[spot.StashSpotId] = spot;
            }

            return map;
        }

        bool HasPending(string spotId) => TryGetPending(spotId, out _);

        bool TryGetPending(string spotId, out PendingStashEvent pending)
        {
            foreach (var candidate in _pending)
            {
                if (candidate != null && candidate.SpotId == spotId)
                {
                    pending = candidate;
                    return true;
                }
            }

            pending = null;
            return false;
        }

        public List<PendingStashEvent> CaptureState()
        {
            var list = new List<PendingStashEvent>(_pending.Count);
            foreach (var pending in _pending)
            {
                if (pending != null)
                {
                    list.Add(new PendingStashEvent
                    {
                        SpotId = pending.SpotId,
                        Kind = pending.Kind,
                        TriggerDay = pending.TriggerDay,
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// セーブデータから予告を復元する。ロードの単一の所有者（<c>GameSession</c>）だけが、保管庫の復元後に呼ぶ。
        /// 設置場所が世界に無い予告は捨て、残った予告のラベルを世界の中に出し直す。
        /// </summary>
        public void RestoreState(List<PendingStashEvent> pending)
        {
            _pending.Clear();

            var spots = FindSpotsById();
            if (pending != null)
            {
                foreach (var entry in pending)
                {
                    if (entry != null && spots.ContainsKey(entry.SpotId))
                    {
                        _pending.Add(new PendingStashEvent
                        {
                            SpotId = entry.SpotId,
                            Kind = entry.Kind,
                            TriggerDay = entry.TriggerDay,
                        });
                    }
                }
            }

            RefreshNotices(spots, null);
        }
    }
}
