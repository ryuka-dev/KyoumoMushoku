using KyoumoMushoku.Core.Zones;
using UnityEngine;

namespace KyoumoMushoku.Editor.Greybox
{
    /// <summary>
    /// 第一街区の灰箱レイアウト。第十三節の「安全と豊かさの勾配」をそのまま X 軸に写す。
    /// 左端が最も安全で最も貧しく、右端が最も危険で最も実り多い。
    /// </summary>
    public static class FirstDistrictLayout
    {
        public readonly struct Area
        {
            public readonly string Name;
            public readonly float XStart;
            public readonly float Width;
            public readonly float GroundY;
            public readonly AlertZoneId Zone;
            public readonly Color Tint;

            public Area(string name, float xStart, float width, float groundY, AlertZoneId zone, Color tint)
            {
                Name = name;
                XStart = xStart;
                Width = width;
                GroundY = groundY;
                Zone = zone;
                Tint = tint;
            }

            public float CenterX => XStart + Width * 0.5f;
        }

        public const float SurfaceY = 0f;
        public const float UnderpassY = -20f;

        /// <summary>プレイヤーの開始地点。公園の中央。</summary>
        public static readonly Vector2 PlayerStart = new(22f, SurfaceY + 1.5f);

        // 地上を 100 → 170 と歩けば 70 units、地下通路なら 50 units。
        // 地下通路は警察を迂回するだけでなく、実際に短い。
        public const float StairSurfaceAlleyX = 100f;
        public const float StairUnderpassWestX = 100f;
        public const float StairUnderpassEastX = 150f;
        public const float StairSurfaceStreetX = 170f;

        public static readonly Area[] Areas =
        {
            new("Hospital", -35f, 35f, SurfaceY, AlertZoneId.Quiet, new Color(0.74f, 0.84f, 0.80f)),
            new("Park", 0f, 45f, SurfaceY, AlertZoneId.Quiet, new Color(0.55f, 0.74f, 0.50f)),
            new("PublicToilet", 45f, 25f, SurfaceY, AlertZoneId.Quiet, new Color(0.64f, 0.78f, 0.62f)),
            new("BackAlley", 70f, 40f, SurfaceY, AlertZoneId.Residential, new Color(0.45f, 0.38f, 0.32f)),
            new("ConvenienceStore", 110f, 40f, SurfaceY, AlertZoneId.Commercial, new Color(0.42f, 0.56f, 0.76f)),
            new("MainStreet", 150f, 45f, SurfaceY, AlertZoneId.Commercial, new Color(0.32f, 0.45f, 0.68f)),
            new("ShoppingStreetEntrance", 195f, 35f, SurfaceY, AlertZoneId.Commercial, new Color(0.24f, 0.34f, 0.60f)),
            new("Underpass", 95f, 60f, UnderpassY, AlertZoneId.Residential, new Color(0.30f, 0.28f, 0.31f)),
        };

        public const float WorldLeftEdge = -35f;
        public const float WorldRightEdge = 230f;
    }
}
