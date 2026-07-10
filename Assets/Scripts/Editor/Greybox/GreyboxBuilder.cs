using System.IO;
using System.Linq;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Diagnostics;
using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Foraging;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Police;
using KyoumoMushoku.Gameplay.Rendering;
using KyoumoMushoku.Gameplay.Session;
using KyoumoMushoku.Gameplay.Survival;
using KyoumoMushoku.Gameplay.UI;
using KyoumoMushoku.Gameplay.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace KyoumoMushoku.Editor.Greybox
{
    /// <summary>
    /// 第一街区の灰箱シーンを生成する。美術アセットは一切必要としない。
    /// 8つの区域はすべて同一シーンに置く。移動時間そのものがソフトクロックを進めるため、
    /// 区域の切り替えにロードや瞬間移動を挟むと、設計上の圧力が失われる。
    ///
    /// Phase 1 で水源・就寝場所・病院・状態の HUD・SAN の退色を、Phase 2 でゴミ箱3種を配線した。
    /// Phase 3 は商業ゾーンを巡回する警官1名と警戒度の所有者を加え、
    /// 命題③（警察の追い出しがプレッシャーを生み出せるか）を実機で確かめる。
    /// </summary>
    public static class GreyboxBuilder
    {
        const string ScenePath = "Assets/Scenes/FirstDistrict.unity";
        const string SchedulePath = "Assets/Config/DaySchedule.asset";
        const string ItemDatabasePath = "Assets/Config/ItemDatabase.asset";
        const string TrashCanLootPath = "Assets/Config/TrashCanLoot.asset";
        const string VitalsTuningPath = "Assets/Config/VitalsTuning.asset";
        const string SpritePath = "Assets/Art/Greybox/White.png";
        const string AreaPrefabFolder = "Assets/Prefabs/Areas";

        // 背景板は地面際の低い壁に留める。高くすると奥行きの層を覆い隠してしまう。
        const float BackdropHeight = 3.5f;
        const float GroundThickness = 2f;
        const float ZoneVolumeHeight = 12f;

        [MenuItem("KyoumoMushoku/Build Greybox Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureWhiteSpriteAsset();
            EnsureDayScheduleAsset();
            EnsureItemDatabaseAsset();
            EnsureTrashCanLootAsset();
            EnsureVitalsTuningAsset();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // シーンを切り替えると参照のないアセットは破棄される。
            // したがってアセットの読み込みは必ず NewScene のあとに行う。
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            var schedule = AssetDatabase.LoadAssetAtPath<DayScheduleAsset>(SchedulePath);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemDatabaseAsset>(ItemDatabasePath);
            var loot = AssetDatabase.LoadAssetAtPath<TrashCanLootAsset>(TrashCanLootPath);
            var tuning = AssetDatabase.LoadAssetAtPath<VitalsTuningAsset>(VitalsTuningPath);
            var spriteMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            if (white == null || schedule == null || catalog == null || loot == null || tuning == null || spriteMaterial == null)
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

            var player = BuildPlayer(white, spriteMaterial, catalog, tuning);
            BuildCamera(player.transform);
            BuildStairwells();

            // ゴミ箱は時計（昼夜・日替りのリポップ）に配線するため、システムを先に建てる。
            var clock = BuildSystems(schedule);
            BuildInteractables(white, spriteMaterial, catalog, loot, clock);
            var officer = BuildPolice(white, spriteMaterial);
            BuildSessionAndGrade(clock, player);
            BuildCanvas(player, clock);

            var hud = clock.gameObject.AddComponent<PhaseZeroHud>();
            hud.Configure(clock, player.GetComponent<ZoneTracker>(), player.GetComponent<PlayerMotor>(), player.transform);
            hud.ConfigurePolice(clock.GetComponent<ZoneAlertDirector>(), officer);

            EnsureAssetFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log($"Phase 3 greybox built at {ScenePath}. A/D で歩く、Shift で走る、E で調べる／漁る、数字で飲食。");
            Debug.Log("ゴミ箱は E で漁る（探索時間あり・歩くと中断）。夜のコンビニ前は弁当が出る。使い切ると翌日まで空。");
            Debug.Log("コンビニ前を警官が巡回する。見られたまま漁り続けると 注意→警告→追い出し と進む。" +
                      "走れば振り切れる（警官 6.5 < 走り 7.5）。地下通路は地上の警官から見えない。");
            Debug.Log("公園のベンチで寝続けると顔を覚えられ、叩き起こされて回復が半減する。安宿では起こされない。");
            Debug.Log($"1日目は約 {schedule.ToSchedule().ForDay(1).SecondsUntilNight / 60f:F1} 分で夜に入る。");
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

        static GameObject BuildPlayer(Sprite white, Material material, ItemDatabaseAsset catalog, VitalsTuningAsset tuning)
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

            player.AddComponent<PlayerVitals>().Configure(tuning);
            player.AddComponent<PlayerWallet>();
            player.AddComponent<PlayerInventory>().Configure(catalog);
            player.AddComponent<PlayerConsumer>();
            player.AddComponent<PlayerInteractor>();

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

            // SAN の退色（ColorAdjustments）を映すため、カメラのポスト処理を有効にする。
            var cameraData = go.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

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

            // 警戒度の所有者。警官・就寝場所・GameSession がここを探して読む（第五節）。
            clock.gameObject.AddComponent<ZoneAlertDirector>();
            return clock;
        }

        /// <summary>
        /// 警官1名を商業ゾーンに置く。巡回の起点はコンビニ前（第十三節）であり、
        /// ちょうど夜に弁当が出るゴミ箱 B（x=128）を見張る位置になる。
        /// 追跡は自分のエリアの中に留まるため、エリアから離れれば逃げ切れる（第五節・第2段階）。
        /// </summary>
        static PoliceOfficer BuildPolice(Sprite white, Material material)
        {
            var root = new GameObject("Police").transform;

            // 根は等倍のまま置く。姿は子が持ち、頭上の台詞が縦に引き伸ばされないようにする。
            var officer = new GameObject("PoliceOfficer");
            officer.transform.SetParent(root, false);
            officer.transform.position = new Vector3(140f, FirstDistrictLayout.SurfaceY + 1.5f, 0f);

            var body = MakeQuad("Body", white, material, new Color(0.25f, 0.3f, 0.55f), sortingOrder: 6);
            body.transform.SetParent(officer.transform, false);
            body.transform.localScale = new Vector3(1.2f, 3f, 1f);

            var speech = MakeSpeech(officer.transform, new Vector3(0f, 2.4f, 0f));

            var component = officer.AddComponent<PoliceOfficer>();
            component.Configure(AlertZoneId.Commercial, patrolMinX: 116f, patrolMaxX: 146f, speech);
            return component;
        }

        /// <summary>
        /// 話者の頭上に出す世界空間の文字（第十四節）。画面上のトーストと違い、誰が言ったのかが分かる。
        /// </summary>
        static NpcSpeech MakeSpeech(Transform parent, Vector3 localPosition)
        {
            var go = new GameObject("Speech");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var text = go.AddComponent<TextMeshPro>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 3f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(14f, 2.5f);

            if (go.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.sortingOrder = 40;
            }

            var speech = go.AddComponent<NpcSpeech>();
            speech.Configure(text);
            return speech;
        }

        static void BuildSessionAndGrade(GameClockDriver clock, GameObject player)
        {
            var session = clock.gameObject.AddComponent<GameSession>();
            session.Configure(clock, player.transform);

            clock.gameObject.AddComponent<SanityColorGrade>().Configure(player.GetComponent<PlayerVitals>());
        }

        /// <summary>
        /// 水源・就寝場所・病院・ゴミ箱を置く。すべて第十三節の勾配（安全で貧しい端 → 危険で豊かな端）に沿う。
        /// Phase 2 で、食料・廃品を供給していた足場（ItemPickup）を、漁って引くゴミ箱3種に置き換える。
        /// 店（購入・買い取り）は Phase 4 で入る。
        /// </summary>
        static void BuildInteractables(Sprite white, Material material, ItemDatabaseAsset catalog,
            TrashCanLootAsset loot, GameClockDriver clock)
        {
            var root = new GameObject("Interactables").transform;

            // 病院：公園側の最も安全で貧しい端（第十三節）。
            var hospital = MakeInteractableMarker("Hospital", root, white, material,
                new Vector3(-17f, FirstDistrictLayout.SurfaceY + 1.5f, 0f), new Color(0.80f, 0.35f, 0.35f));
            hospital.AddComponent<Hospital>().Configure(medicalFeeYen: 500);

            // 公園の水道：無料・SAN を削らない。
            var tap = MakeInteractableMarker("WaterTap_Park", root, white, material,
                new Vector3(30f, FirstDistrictLayout.SurfaceY + 1f, 0f), new Color(0.35f, 0.6f, 0.85f));
            tap.AddComponent<WaterSource>().Configure("水を飲む", thirstRestored: 35f, sanityCost: 0f);

            // 公衆トイレの水：無料だが尊厳と引き換え。飲むと SAN が減る。
            var toilet = MakeInteractableMarker("WaterTap_Toilet", root, white, material,
                new Vector3(57f, FirstDistrictLayout.SurfaceY + 1f, 0f), new Color(0.45f, 0.55f, 0.55f));
            toilet.AddComponent<WaterSource>().Configure("トイレの水を飲む", thirstRestored: 35f, sanityCost: -6f);

            // 公園のベンチ：静穏ゾーン。無料・回復控えめ。通い詰めると警官に顔を覚えられ、叩き起こされる。
            var bench = MakeInteractableMarker("SleepSpot_Bench", root, white, material,
                new Vector3(10f, FirstDistrictLayout.SurfaceY + 1f, 0f), new Color(0.55f, 0.45f, 0.35f));
            var benchSpot = bench.AddComponent<SleepSpot>();
            benchSpot.Configure("bench_park", "ベンチで寝る", AlertZoneId.Quiet,
                costYen: 0, fullRestore: false, hpRecovery: 20f, thirstRecovery: 0f, hungerRecovery: 0f, sanityRecovery: 10f);

            // 台詞はベンチ本体の子にしない。本体は縦に引き伸ばされており、文字まで歪むためである。
            benchSpot.BindSpeech(MakeSpeech(root, new Vector3(10f, FirstDistrictLayout.SurfaceY + 3.4f, 0f)));

            // 地下通路：生活ゾーン。無料だが SAN の回復が悪い。
            var underpass = MakeInteractableMarker("SleepSpot_Underpass", root, white, material,
                new Vector3(125f, FirstDistrictLayout.UnderpassY + 1f, 0f), new Color(0.4f, 0.38f, 0.42f));
            underpass.AddComponent<SleepSpot>().Configure("underpass_residential", "地下通路で寝る", AlertZoneId.Residential,
                costYen: 0, fullRestore: false, hpRecovery: 25f, thirstRecovery: 0f, hungerRecovery: 0f, sanityRecovery: 5f);

            // 安宿：商業ゾーン。有料・完全回復＋セーブ。
            var inn = MakeInteractableMarker("SleepSpot_Inn", root, white, material,
                new Vector3(172f, FirstDistrictLayout.SurfaceY + 1.5f, 0f), new Color(0.7f, 0.6f, 0.4f));
            inn.AddComponent<SleepSpot>().Configure("inn_commercial", "安宿に泊まる", AlertZoneId.Commercial,
                costYen: 1500, fullRestore: true, hpRecovery: 0f, thirstRecovery: 0f, hungerRecovery: 0f, sanityRecovery: 0f);

            // ゴミ箱3種（第十三節）。勾配に沿って、安全で貧しい公園から、危険で豊かな路地裏・コンビニへ。
            // A：公園。缶・瓶が中心、食品は少ない。静穏ゾーン。
            BuildTrashCan(root, white, material, loot, clock, "TrashCan_A_Park",
                TrashCanKind.Park, new Vector3(16f, FirstDistrictLayout.SurfaceY + 1f, 0f),
                yieldsPerDay: 3, rummageSeconds: 1.8f);

            // C：路地裏の大型ゴミ箱。廃品が中心、食品は状態が悪い。大型ゆえ漁りに時間がかかる。生活ゾーン。
            BuildTrashCan(root, white, material, loot, clock, "TrashCan_C_BackAlley",
                TrashCanKind.BackAlley, new Vector3(86f, FirstDistrictLayout.SurfaceY + 1f, 0f),
                yieldsPerDay: 3, rummageSeconds: 2.6f);

            // B：コンビニ前。昼は乏しいが、夜に弁当・パンが出る。状態は良いが商業ゾーンで危険。
            BuildTrashCan(root, white, material, loot, clock, "TrashCan_B_ConvenienceStore",
                TrashCanKind.ConvenienceStore, new Vector3(128f, FirstDistrictLayout.SurfaceY + 1f, 0f),
                yieldsPerDay: 4, rummageSeconds: 2.2f);
        }

        static void BuildTrashCan(Transform parent, Sprite white, Material material, TrashCanLootAsset loot,
            GameClockDriver clock, string name, TrashCanKind kind, Vector3 position, int yieldsPerDay, float rummageSeconds)
        {
            var can = MakeQuad(name, white, material, new Color(0.55f, 0.5f, 0.35f), sortingOrder: 5);
            can.transform.SetParent(parent, false);
            can.transform.position = position;
            can.transform.localScale = new Vector3(1.4f, 1.8f, 1f);

            var collider = can.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            can.AddComponent<TrashCan>().Configure(kind, loot, clock, yieldsPerDay, rummageSeconds);
        }

        static GameObject MakeInteractableMarker(string name, Transform parent, Sprite white, Material material,
            Vector3 position, Color tint)
        {
            var marker = MakeQuad(name, white, material, tint, sortingOrder: 5);
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.6f, 2f, 1f);

            var collider = marker.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            return marker;
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

        /// <summary>
        /// Screen Space - Overlay の HUD。ポスト処理の後段に描かれるため SAN の退色を受けず、
        /// 文字は常に読める（第三節）。4状態のゲージ、行動プロンプト、カバンの一覧を持つ。
        /// </summary>
        static void BuildCanvas(GameObject player, GameClockDriver clock)
        {
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            var font = TMP_Settings.defaultFontAsset;

            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasT = canvasGo.transform;

            var hp = MakeGauge(canvasT, white, font, "HP", new Color(0.85f, 0.3f, 0.3f), 0);
            var thirst = MakeGauge(canvasT, white, font, "喉", new Color(0.3f, 0.6f, 0.9f), 1);
            var hunger = MakeGauge(canvasT, white, font, "腹", new Color(0.9f, 0.6f, 0.25f), 2);
            var sanity = MakeGauge(canvasT, white, font, "気分", new Color(0.62f, 0.5f, 0.85f), 3);

            var status = MakeText(canvasT, font, "Status", new Vector2(0f, 1f),
                new Vector2(24f, -24f - 4f * 40f - 8f), new Vector2(760f, 80f), 26f, TextAlignmentOptions.TopLeft);

            var hud = canvasGo.AddComponent<VitalsHud>();
            hud.Configure(player.GetComponent<PlayerVitals>(), player.GetComponent<PlayerWallet>(),
                clock, player.GetComponent<ZoneTracker>());
            hud.BindFills(hp, thirst, hunger, sanity, status);

            var interactor = player.GetComponent<PlayerInteractor>();

            var promptText = MakeText(canvasT, font, "InteractionPrompt", new Vector2(0.5f, 0f),
                new Vector2(0f, 90f), new Vector2(1000f, 60f), 34f, TextAlignmentOptions.Center);
            canvasGo.AddComponent<InteractionPrompt>().Configure(interactor, promptText);

            // 漁りの結果を世界の言葉で短く伝えるトースト（第十四節）。プロンプトの少し上に出す。
            var toastText = MakeText(canvasT, font, "ActionToast", new Vector2(0.5f, 0f),
                new Vector2(0f, 170f), new Vector2(1000f, 50f), 30f, TextAlignmentOptions.Center);
            canvasGo.AddComponent<ActionToast>().Configure(interactor, toastText);

            var inventoryText = MakeText(canvasT, font, "Inventory", new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(470f, 780f), 26f, TextAlignmentOptions.TopLeft);
            canvasGo.AddComponent<InventoryView>().Configure(player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerVitals>(), player.GetComponent<PlayerConsumer>(), inventoryText);
        }

        static Image MakeGauge(Transform parent, Sprite white, TMP_FontAsset font, string label, Color color, int row)
        {
            var y = -24f - row * 40f;

            MakeText(parent, font, $"{label}_label", new Vector2(0f, 1f),
                new Vector2(24f, y), new Vector2(56f, 30f), 24f, TextAlignmentOptions.MidlineLeft);

            MakeUIImage(parent, white, new Color(0f, 0f, 0f, 0.5f), "gauge_bg",
                new Vector2(84f, y - 2f), new Vector2(330f, 26f));

            var fill = MakeUIImage(parent, white, color, $"{label}_fill",
                new Vector2(84f, y - 2f), new Vector2(330f, 26f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            return fill;
        }

        static Image MakeUIImage(Transform parent, Sprite sprite, Color color, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text MakeText(Transform parent, TMP_FontAsset font, string name, Vector2 anchor,
            Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
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

        static void EnsureItemDatabaseAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<ItemDatabaseAsset>(ItemDatabasePath) != null)
            {
                return;
            }

            EnsureAssetFolder(Path.GetDirectoryName(ItemDatabasePath)!.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<ItemDatabaseAsset>();
            asset.PopulateDefaults();
            AssetDatabase.CreateAsset(asset, ItemDatabasePath);
            AssetDatabase.SaveAssets();
        }

        static void EnsureTrashCanLootAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<TrashCanLootAsset>(TrashCanLootPath) != null)
            {
                return;
            }

            EnsureAssetFolder(Path.GetDirectoryName(TrashCanLootPath)!.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<TrashCanLootAsset>();
            asset.PopulateDefaults();
            AssetDatabase.CreateAsset(asset, TrashCanLootPath);
            AssetDatabase.SaveAssets();
        }

        static void EnsureVitalsTuningAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<VitalsTuningAsset>(VitalsTuningPath) != null)
            {
                return;
            }

            EnsureAssetFolder(Path.GetDirectoryName(VitalsTuningPath)!.Replace('\\', '/'));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<VitalsTuningAsset>(), VitalsTuningPath);
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
