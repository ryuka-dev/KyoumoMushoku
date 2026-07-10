using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Zones;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// プレイヤーが今どの警戒ゾーンにいるかを報告する。
    /// 警戒度そのものは保持しない（Phase 3 で導入する）。
    /// </summary>
    public sealed class ZoneTracker : MonoBehaviour
    {
        readonly List<AlertZoneVolume> _overlapping = new();

        public AlertZoneId CurrentZone { get; private set; } = AlertZoneId.None;

        public event Action<AlertZoneId> ZoneChanged;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out AlertZoneVolume volume))
            {
                return;
            }

            _overlapping.Add(volume);
            Resolve();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out AlertZoneVolume volume))
            {
                return;
            }

            _overlapping.Remove(volume);
            Resolve();
        }

        /// <summary>境界で複数のボリュームに重なりうるため、最後に入ったものを現在地とする。</summary>
        void Resolve()
        {
            var next = _overlapping.Count > 0
                ? _overlapping[^1].Zone
                : AlertZoneId.None;

            if (next == CurrentZone)
            {
                return;
            }

            CurrentZone = next;
            ZoneChanged?.Invoke(next);
        }
    }
}
