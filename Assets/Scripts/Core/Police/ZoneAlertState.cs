using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Zones;

namespace KyoumoMushoku.Core.Police
{
    /// <summary>就寝時オートセーブで永続化される、1ゾーン分の警戒度。</summary>
    [Serializable]
    public struct ZoneAlertEntry
    {
        public AlertZoneId Zone;
        public float Level;

        public ZoneAlertEntry(AlertZoneId zone, float level)
        {
            Zone = zone;
            Level = level;
        }
    }

    /// <summary>
    /// 警戒度は日をまたいで残る（「同じベンチで寝続けると顔を覚えられる」）。
    /// したがって就寝時オートセーブに載る純粋データとして保つ。
    /// </summary>
    [Serializable]
    public sealed class ZoneAlertState
    {
        public List<ZoneAlertEntry> Zones = new List<ZoneAlertEntry>();
    }
}
