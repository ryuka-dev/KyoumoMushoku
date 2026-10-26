using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KyoumoMushoku.Editor.Build
{
    /// <summary>
    /// ゲームクリエイター甲子園 2026 に出す Windows 版を作る（作業計画 第一の二節 K5・K7）。
    ///
    /// 提出するのは一人称の切片（Slice_Chapter1）なので、起動シーンを Build Settings から取らず、ここで明示する。
    /// Build Settings の登録（起動シーンは 2D 版の FirstDistrict）は手が持つもので、触れない（CLAUDE.md シーンの所有権）。
    /// 出力は Builds/Submission/（.gitignore 済み）。実行ファイル名は KyoumoMushoku.exe。
    /// </summary>
    public static class SubmissionBuild
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/Slice_Chapter1.unity",
        };

        const string OutputDirectory = "Builds/Submission";
        const string ExecutableName = "KyoumoMushoku.exe";

        [MenuItem("KyoumoMushoku/Build Submission (Windows)")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = Path.Combine(OutputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[SubmissionBuild] 成功：{summary.outputPath}（{summary.totalSize / (1024f * 1024f):F1} MB・{summary.totalTime.TotalSeconds:F0} 秒）");
            }
            else
            {
                Debug.LogError($"[SubmissionBuild] 失敗：{summary.result}・エラー {summary.totalErrors} 件");
            }
        }
    }
}
