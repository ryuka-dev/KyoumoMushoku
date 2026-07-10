using System.IO;
using System.Linq;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Diagnostics;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Scaffolding;
using KyoumoMushoku.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KyoumoMushoku.Editor.Greybox
{
    /// <summary>
    /// Phase 0 の灰箱シーンを生成する。美術アセットは一切必要としない。
    /// 8つの区域はすべて同一シーンに置く。移動時間そのものがソフトクロックを進めるため、
    /// 区域の切り替えにロードや瞬間移動を挟むと、設計上の圧力が失われる。
    /// </summary>
    public static class GreyboxBuilder
    {
        const string ScenePath = "Assets/Scenes/FirstDistrict.unity";
        const string SchedulePath = "Assets/Config/DaySchedule.asset";
        const string SpritePath = "Assets/Art/Greybox/White.png";
        const string AreaPrefabFolder = "Assets/Prefabs/Areas";

        const float BackdropHeight = 14f;
        const float GroundThickness = 2f;
        const float ZoneVolumeHeight = 12f;

        [MenuItem("KyoumoMushoku/Phase 0/Build Greybox Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var white = EnsureWhiteSprite();
            var spriteMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            var schedule = EnsureDaySchedule();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var areasRoot = new GameObject("Areas").transform;
            EnsureAssetFolder(AreaPrefabFolder);
            foreach (var area in FirstDistrictLayout.Areas)
            {
                BuildArea(area, white, spriteMaterial, areasRoot);
            }

            BuildWorldEdges(white, spriteMaterial);

            var player = BuildPlayer(white, spriteMaterial);
            BuildCamera(player.transform);
            BuildStairwells();

            var clock = BuildSystems(schedule);

            var hud = clock.gameObject.AddComponent<PhaseZeroHud>();
            hud.Configure(clock, player.GetComponent<ZoneTracker>(), player.GetComponent<PlayerMotor>(), player.transform);

            clock.gameObject.AddComponent<PhaseZeroSleepKey>().Configure(clock);

            EnsureAssetFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log($"Phase 0 greybox built at {ScenePath}. Play, then A/D to walk, Shift to run, N to sleep.");
        }

        static void BuildArea(FirstDistrictLayout.Area area, Sprite white, Material material, Transform parent)
        {
            var root = new GameObject(area.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(area.CenterX, area.GroundY, 0f);

            var backdrop = MakeQuad("Backdrop", white, material, area.Tint, sortingOrder: -10);
            backdrop.transform.SetParent(root.transform, false);
            backdrop.transform.localPosition = new Vector3(0f, BackdropHeight * 0.5f, 0f);
            backdrop.transform.localScale = new Vector3(area.Width, BackdropHeight, 1f);

            var groundTint = area.Tint * 0.45f;
            groundTint.a = 1f;
            var ground = MakeQuad("Ground", white, material, groundTint, sortingOrder: 0);
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0f, -GroundThickness * 0.5f, 0f);
            ground.transform.localScale = new Vector3(area.Width, GroundThickness, 1f);
            ground.AddComponent<BoxCollider2D>();

            var zone = new GameObject("AlertZone");
            zone.transform.SetParent(root.transform, false);
            zone.transform.localPosition = new Vector3(0f, ZoneVolumeHeight * 0.5f, 0f);
            var zoneCollider = zone.AddComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector2(area.Width, ZoneVolumeHeight);
            zone.AddComponent<AlertZoneVolume>().Configure(area.Zone);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                root, $"{AreaPrefabFolder}/{area.Name}.prefab", InteractionMode.AutomatedAction);
        }

        static void BuildWorldEdges(Sprite white, Material material)
        {
            var root = new GameObject("WorldEdges").transform;

            foreach (var (name, x) in new[]
                     {
                         ("LeftWall", FirstDistrictLayout.WorldLeftEdge - 0.5f),
                         ("RightWall", FirstDistrictLayout.WorldRightEdge + 0.5f),
                     })
            {
                var wall = MakeQuad(name, white, material, new Color(0.15f, 0.15f, 0.15f), sortingOrder: 1);
                wall.transform.SetParent(root, false);
                wall.transform.position = new Vector3(x, FirstDistrictLayout.SurfaceY + 10f, 0f);
                wall.transform.localScale = new Vector3(1f, 24f, 1f);
                wall.AddComponent<BoxCollider2D>();
            }
        }

        static GameObject BuildPlayer(Sprite white, Material material)
        {
            var player = MakeQuad("Player", white, material, new Color(0.95f, 0.85f, 0.30f), sortingOrder: 10);
            player.transform.position = FirstDistrictLayout.PlayerStart;
            player.transform.localScale = new Vector3(0.9f, 1.8f, 1f);

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            player.AddComponent<BoxCollider2D>();
            player.AddComponent<InputSystemPlayerInput>();
            player.AddComponent<PlayerMotor>();
            player.AddComponent<ZoneTracker>();

            return player;
        }

        static void BuildCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
            go.transform.position = new Vector3(target.position.x, target.position.y + 2f, -10f);
            go.AddComponent<CameraFollow>().Configure(target);
        }

        static GameClockDriver BuildSystems(DayScheduleAsset schedule)
        {
            var clock = new GameObject("Systems").AddComponent<GameClockDriver>();

            var so = new SerializedObject(clock);
            so.FindProperty("_schedule").objectReferenceValue = schedule;
            so.ApplyModifiedPropertiesWithoutUndo();

            return clock;
        }

        static void BuildStairwells()
        {
            var root = new GameObject("Stairwells").transform;

            var alley = MakeStairwell("Stair_Alley", root,
                new Vector2(FirstDistrictLayout.StairSurfaceAlleyX, FirstDistrictLayout.SurfaceY + 1.5f));
            var underWest = MakeStairwell("Stair_Underpass_West", root,
                new Vector2(FirstDistrictLayout.StairUnderpassWestX, FirstDistrictLayout.UnderpassY + 1.5f));
            var underEast = MakeStairwell("Stair_Underpass_East", root,
                new Vector2(FirstDistrictLayout.StairUnderpassEastX, FirstDistrictLayout.UnderpassY + 1.5f));
            var street = MakeStairwell("Stair_MainStreet", root,
                new Vector2(FirstDistrictLayout.StairSurfaceStreetX, FirstDistrictLayout.SurfaceY + 1.5f));

            Link(alley, underWest);
            Link(underEast, street);
        }

        static (Stairwell Stairwell, Transform Arrival) MakeStairwell(string name, Transform parent, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2f, 3f);

            var arrival = new GameObject("Arrival").transform;
            arrival.SetParent(go.transform, false);

            // 到着点を階段の口からずらさないと、着いた瞬間に押し戻される。
            arrival.localPosition = new Vector3(3f, 0f, 0f);

            return (go.AddComponent<Stairwell>(), arrival);
        }

        static void Link((Stairwell Stairwell, Transform Arrival) a, (Stairwell Stairwell, Transform Arrival) b)
        {
            a.Stairwell.Configure(b.Stairwell, a.Arrival);
            b.Stairwell.Configure(a.Stairwell, b.Arrival);
        }

        static GameObject MakeQuad(string name, Sprite sprite, Material material, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            // URP の 2D Renderer では既定のスプライトマテリアルが Lit であり、
            // Light2D が無いシーンでは真っ黒に描画される。灰箱では明示的に Unlit を使う。
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }

        static Sprite EnsureWhiteSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null)
            {
                return existing;
            }

            EnsureAssetFolder(Path.GetDirectoryName(SpritePath)!.Replace('\\', '/'));

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            // 4x4 px の板が 1x1 unit になる。以降は transform のスケールだけで伸ばす。
            importer.spritePixelsPerUnit = 4f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        static DayScheduleAsset EnsureDaySchedule()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DayScheduleAsset>(SchedulePath);
            if (existing != null)
            {
                return existing;
            }

            EnsureAssetFolder(Path.GetDirectoryName(SchedulePath)!.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<DayScheduleAsset>();
            AssetDatabase.CreateAsset(asset, SchedulePath);
            return asset;
        }

        /// <summary>AssetDatabase が認識するフォルダを、途中の階層も含めて作る。</summary>
        static void EnsureAssetFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var segments = assetPath.Split('/');
            var current = segments[0];

            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        static void RegisterSceneInBuildSettings()
        {
            if (EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Prepend(new EditorBuildSettingsScene(ScenePath, enabled: true))
                .ToArray();
        }
    }
}
