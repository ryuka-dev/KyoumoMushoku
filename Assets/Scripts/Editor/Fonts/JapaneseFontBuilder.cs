using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace KyoumoMushoku.Editor.Fonts
{
    /// <summary>
    /// Source Han Sans から日本語の TMP フォントアセット（動的SDF）を生成し、TMP の既定フォントに設定する。
    ///
    /// 動的アトラス（Dynamic）を選ぶのは、日本語の全字形を事前に焼き込むと巨大になるためである。
    /// 必要な字形は実行時に元フォントから追加される。元の .otf はビルドに含める必要がある。
    ///
    /// 太字は疑似ボールドではなく本物の Bold を weight table に接続する。
    /// </summary>
    public static class JapaneseFontBuilder
    {
        const string RegularOtf = "Assets/Fonts/SourceHanSans-Regular.otf";
        const string BoldOtf = "Assets/Fonts/SourceHanSans-Bold.otf";
        const string RegularSdf = "Assets/Fonts/SourceHanSans-Regular SDF.asset";
        const string BoldSdf = "Assets/Fonts/SourceHanSans-Bold SDF.asset";

        // Bold の weight table 上の位置（700）。
        const int BoldWeightIndex = 7;

        [MenuItem("KyoumoMushoku/Phase 1/Build Japanese Font Assets")]
        public static void Build()
        {
            var regular = CreateDynamicSdf(RegularOtf, RegularSdf, "SourceHanSans-Regular SDF");
            var bold = CreateDynamicSdf(BoldOtf, BoldSdf, "SourceHanSans-Bold SDF");

            if (regular == null || bold == null)
            {
                return;
            }

            WireBoldWeight(regular, bold);
            SetAsDefault(regular);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("日本語フォントアセットを生成し、TMP の既定フォントに設定した。");
        }

        static TMP_FontAsset CreateDynamicSdf(string otfPath, string assetPath, string assetName)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(otfPath);
            if (sourceFont == null)
            {
                Debug.LogError($"元フォントが見つからない：{otfPath}");
                return null;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError($"フォントアセットの生成に失敗した：{otfPath}");
                return null;
            }

            fontAsset.name = assetName;
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // アトラステクスチャとマテリアルをサブアセットとして永続化しないと、再読み込み後に失われる。
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                var atlas = fontAsset.atlasTextures[0];
                atlas.name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                fontAsset.atlasTextures[0].hideFlags = HideFlags.HideInHierarchy;
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                fontAsset.material.hideFlags = HideFlags.HideInHierarchy;
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        static void WireBoldWeight(TMP_FontAsset regular, TMP_FontAsset bold)
        {
            var weights = regular.fontWeightTable;
            if (weights == null || weights.Length <= BoldWeightIndex)
            {
                return;
            }

            weights[BoldWeightIndex].regularTypeface = bold;
            weights[BoldWeightIndex].italicTypeface = bold;
            EditorUtility.SetDirty(regular);
        }

        static void SetAsDefault(TMP_FontAsset regular)
        {
            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                Debug.LogWarning("TMP Settings が見つからないため、既定フォントを設定できなかった。");
                return;
            }

            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = regular;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
