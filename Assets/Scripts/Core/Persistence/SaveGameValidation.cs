using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Zones;

namespace KyoumoMushoku.Core.Persistence
{
    /// <summary>
    /// セーブデータは外部入力として扱う。適用する前に検証し、
    /// 非互換や破損は黙って直さず、明示的に失敗させる（第十一節・第十二節）。
    ///
    /// 個々のアイテムや数値の範囲は、それを所有する型（<c>Inventory</c>・<c>Vitals</c>）が
    /// 復元時に自ら守る。ここが見るのは構造とバージョンだけである。
    ///
    /// ここに届くセーブは <see cref="SaveGameMigration"/> による引き上げを済ませている。
    /// したがって版は現在の版に一致していなければならない。
    /// </summary>
    public static class SaveGameValidation
    {
        public static bool TryValidate(SaveGame save, out string error)
        {
            if (save is null)
            {
                error = "セーブデータが空である。";
                return false;
            }

            if (save.Version != SaveGame.CurrentVersion)
            {
                error = $"互換性のないセーブデータのバージョン（{save.Version}）。想定は {SaveGame.CurrentVersion}。";
                return false;
            }

            if (save.Clock is null || save.Vitals is null || save.Inventory is null ||
                save.ZoneAlerts is null || save.Knacks is null)
            {
                error = "セーブデータの構造が壊れている。";
                return false;
            }

            if (save.Clock.Day < 1)
            {
                error = $"ゲームデイが不正である（{save.Clock.Day}）。";
                return false;
            }

            if (!IsFinite(save.Clock.ElapsedInDay) || save.Clock.ElapsedInDay < 0f)
            {
                error = "その日の経過秒数が不正である。";
                return false;
            }

            if (!IsFinite(save.Vitals.Hp) || !IsFinite(save.Vitals.Thirst) ||
                !IsFinite(save.Vitals.Hunger) || !IsFinite(save.Vitals.Sanity))
            {
                error = "状態の値が不正である。";
                return false;
            }

            if (save.WalletYen < 0)
            {
                error = $"所持金が負である（{save.WalletYen}）。借金は存在しない。";
                return false;
            }

            if (!TryValidateZoneAlerts(save.ZoneAlerts, out error))
            {
                return false;
            }

            return TryValidateKnacks(save.Knacks, out error);
        }

        /// <summary>
        /// コツも外部入力である。未知のコツ・重複・負のカウンタは、黙って直さず拒む。
        /// </summary>
        static bool TryValidateKnacks(KnackState knacks, out string error)
        {
            if (knacks.Acquired is null)
            {
                error = "コツの構造が壊れている。";
                return false;
            }

            var seen = new HashSet<KnackId>();
            foreach (var id in knacks.Acquired)
            {
                if (!Enum.IsDefined(typeof(KnackId), id))
                {
                    error = $"習得済みのコツに未知のものが含まれる（{id}）。";
                    return false;
                }

                if (!seen.Add(id))
                {
                    error = $"習得済みのコツに同じものが二度現れる（{id}）。";
                    return false;
                }
            }

            if (knacks.RummageCount < 0 || knacks.ForageWarnedCount < 0 || knacks.OutdoorSleepCount < 0)
            {
                error = "コツの触発カウンタが負である。";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 警戒度も外部入力である。未知のゾーン・範囲外の値・重複は、黙って直さず拒む。
        /// 記載のないゾーンは 0 として扱えるため、全ゾーンが揃っていることまでは求めない。
        /// </summary>
        static bool TryValidateZoneAlerts(ZoneAlertState alerts, out string error)
        {
            if (alerts.Zones is null)
            {
                error = "警戒度の構造が壊れている。";
                return false;
            }

            var seen = new HashSet<AlertZoneId>();

            foreach (var entry in alerts.Zones)
            {
                if (!IsTracked(entry.Zone))
                {
                    error = $"警戒度に未知の警戒ゾーンが含まれる（{entry.Zone}）。";
                    return false;
                }

                if (!seen.Add(entry.Zone))
                {
                    error = $"警戒度に同じ警戒ゾーンが二度現れる（{entry.Zone}）。";
                    return false;
                }

                if (!IsFinite(entry.Level) ||
                    entry.Level < ZoneAlertTuning.MinLevel || entry.Level > ZoneAlertTuning.MaxLevel)
                {
                    error = $"警戒度が範囲外である（{entry.Zone}：{entry.Level}）。";
                    return false;
                }
            }

            error = null;
            return true;
        }

        static bool IsTracked(AlertZoneId zone)
        {
            foreach (var tracked in ZoneAlertLevels.Zones)
            {
                if (tracked == zone)
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
