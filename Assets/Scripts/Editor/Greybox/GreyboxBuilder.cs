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

        // 背景板は地面際の低い壁に留める。高くすると奥行きの層を覆い隠してしまう。
        const float BackdropHeight = 3.5f;
        const float GroundThickness = 2f;
        const float ZoneVolumeHeight = 12f;

        [MenuItem("KyoumoMushoku/Phase 0/Build Greybox Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureWhiteSpriteAsset();
            EnsureDayScheduleAsset();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // シーンを切り替えると参照のないアセットは破棄される。
            // したがってアセットの読み込みは必ず NewScene のあとに行う。
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            var schedule = AssetDatabase.LoadAssetAtPath<DayScheduleAsset>(SchedulePath);
            var spriteMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            if (white == null || schedule == null || spriteMaterial == null)
            {
                Debug.LogError("Greybox build aborted: required assets could not be loaded after scene switch.");
                return;
            }

            var areasRoot = new GameObject("Areas").transform;
            EnsureAssetFolder(AreaPrefabFolder);
            foreach (var area in FirstDistrictLayout.Areas)
            {
                BuildArea(area, white, spriteMaterial, areasRoot);
            }

            BuildParallaxLayers(white, spriteMaterial);
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
            Debug.Log($"Day 1 reaches night after {schedule.ToSchedule().ForDay(1).SecondsUntilNight / 60f:F1} min.");
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

            // 透視投影にすることで、奥行きの層の視差がカメラの移動だけで自動的に生じる。
            // 距離は、プレイ面（Z=0）が従来の正投影と同じ高さで映るように決める。
            camera.orthographic = false;
            camera.fieldOfView = FirstDistrictLayout.CameraFieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.11f, 0.13f);

            var distance = FirstDistrictLayout.CameraVerticalHalfExtent /
                           Mathf.Tan(FirstDistrictLayout.CameraFieldOfView * 0.5f * Mathf.Deg2Rad);

            go.transform.position = new Vector3(target.position.x, target.position.y + 2f, -distance);
            go.AddComponent<CameraFollow>().Configure(target);
        }

        /// <summary>
        /// 奥行きの層を色板で組む。視差が本当に成立するかを、美術を一枚も描かずに確かめるための探り。
        /// </summary>
        static void BuildParallaxLayers(Sprite white, Material material)
        {
            var root = new GameObject("Parallax").transform;

            foreach (var layer in FirstDistrictLayout.Layers)
            {
                var layerRoot = new GameObject(layer.Name).transform;
                layerRoot.SetParent(root, false);

                var span = FirstDistrictLayout.WorldWidth * layer.SpanScale;
                var start = FirstDistrictLayout.WorldCenterX - span * 0.5f;
                var count = Mathf.CeilToInt(span / layer.Spacing);

                for (var i = 0; i < count; i++)
                {
                    var height = Mathf.Lerp(layer.MinHeight, layer.MaxHeight, DeterministicNoise(i, layer.Name));
                    var block = MakeQuad($"{layer.Name}_{i}", white, material, layer.Tint, layer.SortingOrder);
                    block.transform.SetParent(layerRoot, false);
                    block.transform.position = new Vector3(
                        start + i * layer.Spacing,
                        FirstDistrictLayout.SurfaceY + height * 0.5f,
                        layer.Z);
                    block.transform.localScale = new Vector3(layer.Width, height, 1f);
                }
            }
        }

        /// <summary>ビルの高さを毎回同じにするための、乱数を使わない擬似ノイズ。</summary>
        static float DeterministicNoise(int index, string salt)
        {
            var seed = index * 12.9898f + salt.Length * 78.233f;
            var value = Mathf.Sin(seed) * 43758.5453f;
            return Mathf.Abs(value - Mathf.Floor(value));
        }

        static GameClockDriver BuildSystems(DayScheduleAsset schedule)
        {
            var clock = new GameObject("Systems").AddComponent<GameClockDriver>();
            clock.Configure(schedule);
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

        static void EnsureWhiteSpriteAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) != null)
            {
                return;
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
        }

        static void EnsureDayScheduleAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<DayScheduleAsset>(SchedulePath) != null)
            {
                return;
            }

            EnsureAssetFolder(Path.GetDirectoryName(SchedulePath)!.Replace('\\', '/'));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DayScheduleAsset>(), SchedulePath);
            AssetDatabase.SaveAssets();
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
