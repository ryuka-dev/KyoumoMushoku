using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 約定の守り（ローカライズの口子）。プレイヤー向けの文字列は、必ず域別のテキストモジュール
    /// （命名規約：<c>*Text.cs</c> / <c>*TextLabels.cs</c>）を通す。ここでは、テキストモジュール
    /// 以外の実行時ソースに、日本語を含む文字列リテラルが直書きされていないことを走査で保証する。
    ///
    /// これは「未来の追加が既存の仕組みを複用するか」を自覚ではなくテストで担保するための守衛である。
    /// 新しく硬編码したプレイヤー文字列はこのテストを赤にする。見つかったら、テキストモジュールへ
    /// 移すか、真にデータ（表示名・呼び名）の seed なら <see cref="AllowedDataFiles"/> に加える。
    ///
    /// Editor スクリプト（シーン生成の scaffolding）とテストは対象外。コメントは剥がしてから走査する
    /// ため、日本語コメントの多い本コードベースでも誤検出しない。近似だが、この規約を守るには十分。
    /// </summary>
    public sealed class LocalizationConventionTests
    {
        /// <summary>
        /// テキストモジュール以外で日本語リテラルを許すファイル。データ seed（表示名・呼び名）か、
        /// 診断専用（ログに出る失敗理由）のいずれか。増やすときは必ず理由を添えること。
        /// ここが膨らむのは仕組みの崩れの兆候である。
        /// </summary>
        static readonly string[] AllowedFiles =
        {
            "ItemDatabaseAsset.cs", // アイテムの表示名はカタログ data
            "FileSaveStore.cs",     // セーブ読み込みの失敗理由は診断（ログに出るだけでプレイヤー UI ではない）
        };

        static readonly Regex StringLiteral = new Regex("\"(?:\\\\.|[^\"\\\\])*\"", RegexOptions.Compiled);

        [Test]
        public void NoHardcodedJapaneseStringsOutsideTextModules()
        {
            // Gameplay 層だけを対象にする。Core は純粋ロジックであり、その日本語は例外メッセージや
            // 検証理由（診断）であってプレイヤー UI ではない。プレイヤー向けの表示は Gameplay/UI の
            // テキストモジュールが担う。
            var scriptsRoot = Path.Combine(Application.dataPath, "Scripts", "Gameplay");
            var offenders = new List<string>();

            foreach (var path in Directory.EnumerateFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (ShouldSkipFile(path))
                {
                    continue;
                }

                ScanFile(path, offenders);
            }

            Assert.IsEmpty(offenders,
                "テキストモジュール（*Text / *TextLabels）以外に日本語リテラルが直書きされている。" +
                "テキストモジュール経由に移すか、真にデータ seed なら AllowedDataFiles に加えること。\n" +
                string.Join("\n", offenders));
        }

        static bool ShouldSkipFile(string path)
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/Editor/") || normalized.Contains("/Tests/"))
            {
                return true;
            }

            var name = Path.GetFileName(path);

            // テキストモジュール（許される家）。命名規約で判定するので、新モジュールは自動で豁免される。
            if (name.EndsWith("Text.cs", StringComparison.Ordinal) ||
                name.EndsWith("TextLabels.cs", StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var allowed in AllowedFiles)
            {
                if (name.Equals(allowed, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// プレイヤー向けではない行を除く。属性（Tooltip/Header など・属性付きフィールドの既定値も含む）、
        /// ログ、例外メッセージ、nameof を挟む診断は、日本語でもローカライズの対象ではない。
        /// </summary>
        static bool IsExcludedContext(string code)
        {
            var trimmed = code.TrimStart();
            return trimmed.StartsWith("[", StringComparison.Ordinal) ||
                   code.Contains("Debug.Log") ||
                   code.Contains("nameof(") ||
                   code.Contains("Exception(") ||
                   code.Contains("throw ");
        }

        static void ScanFile(string path, List<string> offenders)
        {
            var lines = File.ReadAllLines(path);
            var inBlockComment = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var code = StripComments(lines[i], ref inBlockComment);
                if (code.Length == 0 || IsExcludedContext(code))
                {
                    continue;
                }

                foreach (Match match in StringLiteral.Matches(code))
                {
                    if (ContainsJapanese(match.Value))
                    {
                        offenders.Add($"{RelativePath(path)}:{i + 1}  {match.Value.Trim()}");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 行から、文字列・文字リテラルを保ったままコメント（<c>//</c> と <c>/* */</c>）を取り除く。
        /// 文字列の中の <c>//</c> をコメントと誤認しないよう、素朴だが状態を追う。
        /// </summary>
        static string StripComments(string line, ref bool inBlockComment)
        {
            var sb = new StringBuilder(line.Length);
            var inString = false;
            var inChar = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                var next = i + 1 < line.Length ? line[i + 1] : '\0';

                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    continue;
                }

                if (inString)
                {
                    sb.Append(c);
                    if (c == '\\' && next != '\0')
                    {
                        sb.Append(next);
                        i++;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (inChar)
                {
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '\'')
                    {
                        inChar = false;
                    }

                    continue;
                }

                if (c == '/' && next == '/')
                {
                    break;
                }

                if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    sb.Append(c);
                    continue;
                }

                if (c == '\'')
                {
                    inChar = true;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        static bool ContainsJapanese(string text)
        {
            foreach (var ch in text)
            {
                // ひらがな・カタカナ・CJK 統合漢字・全角記号（（）や 円 など）。
                if ((ch >= 0x3040 && ch <= 0x30FF) ||
                    (ch >= 0x4E00 && ch <= 0x9FFF) ||
                    (ch >= 0xFF00 && ch <= 0xFFEF))
                {
                    return true;
                }
            }

            return false;
        }

        static string RelativePath(string path)
        {
            var normalized = path.Replace('\\', '/');
            var index = normalized.IndexOf("/Assets/", StringComparison.Ordinal);
            return index >= 0 ? normalized.Substring(index + 1) : normalized;
        }
    }
}
