using KyoumoMushoku.Core.Police;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 警官の台詞と目の印を画面に描く（<see cref="ProtoHud"/> が OnGUI から呼ぶ）。台詞の中身・注目度・段階は警官が持ち、ここは描くだけ。
    ///
    /// 台詞は言った警官の頭の上に出す。誰が言ったかが分かり、手の一言（画面下）とも消し合わない。
    /// 画面の外（背後・横）の警官の台詞は、画面の左右の端にその方向の矢印をつけて出す。声は届くので、見えなくても聞こえる。
    /// 遠すぎる警官の声は届かない（出さない）。
    /// </summary>
    public sealed class ProtoPoliceOverlay
    {
        /// <summary>声が届く距離。</summary>
        const float SpeechReach = 30f;
        const float FadeSeconds = 0.6f;
        const float EdgeMargin = 16f;

        readonly Texture2D _white;
        readonly GUIStyle _bubble;

        public ProtoPoliceOverlay(Texture2D white, GUIStyle baseStyle)
        {
            _white = white;
            _bubble = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = false };
        }

        public void DrawSpeech(Camera camera, float w, float h)
        {
            if (camera == null)
            {
                return;
            }

            var edgeRow = 0;
            foreach (var police in ProtoPolice.All)
            {
                var line = police != null ? police.Speech : null;
                if (line == null)
                {
                    continue;
                }

                var head = police.HeadPosition;
                if ((head - camera.transform.position).sqrMagnitude > SpeechReach * SpeechReach)
                {
                    continue;
                }

                var alpha = Mathf.Clamp01((ProtoPolice.SpeechSeconds - police.SpeechAge) / FadeSeconds);
                // 目の印（頭のすぐ上）より上に出す。
                var screen = camera.WorldToScreenPoint(head + Vector3.up * 0.55f);
                if (InFront(screen, w))
                {
                    var content = new GUIContent(ProtoText.PoliceBubble(line));
                    var size = _bubble.CalcSize(content);
                    var x = Mathf.Clamp(screen.x - size.x / 2f, EdgeMargin, w - size.x - EdgeMargin);
                    var y = Mathf.Clamp(h - screen.y - size.y, EdgeMargin, h - size.y - EdgeMargin);
                    DrawBacked(new Rect(x, y, size.x, size.y), content, alpha);
                }
                else
                {
                    var left = camera.transform.InverseTransformPoint(head).x < 0f;
                    var content = new GUIContent(ProtoText.PoliceFromEdge(line, left));
                    var size = _bubble.CalcSize(content);
                    var x = left ? EdgeMargin : w - size.x - EdgeMargin;
                    var y = h * 0.4f + edgeRow * (size.y + 12f);
                    edgeRow++;
                    DrawBacked(new Rect(x, y, size.x, size.y), content, alpha);
                }
            }
        }

        /// <summary>目の印が出る距離（これより遠い警官の目には気づかない）。</summary>
        const float EyeReach = 40f;
        const float EyeWidth = 56f;
        const float EyeHeight = 34f;
        const int EyeLevels = 8;

        Texture2D[] _eyes;

        /// <summary>
        /// こちらを気にしている警官の目の印。目の開き具合が注目度（冷めていく間は閉じていく）、追われている間は見開いて脈打つ。
        /// 視界の中なら頭の上、外なら画面の左右の端（方向）に出す。どこまで気づけるかは SAN で決まる（<see cref="PoliceAwarenessSight"/>）：
        /// 崩壊していると目の前の目にも気づかず、ふつうでも低いと背後の目には気づかない。色は使わず形で語る（第十七節「色の言葉」）。
        /// </summary>
        public void DrawEyes(Camera camera, float w, float h, float sanity)
        {
            if (camera == null)
            {
                return;
            }

            var awareness = PoliceAwarenessSight.Read(sanity);
            if (awareness == PoliceAwareness.None)
            {
                return;
            }

            _eyes ??= BuildEyes();
            var edgeRow = 0;
            foreach (var police in ProtoPolice.All)
            {
                if (police == null || police.Suspicion <= 0.5f)
                {
                    continue;
                }

                var head = police.HeadPosition;
                if ((head - camera.transform.position).sqrMagnitude > EyeReach * EyeReach)
                {
                    continue;
                }

                var screen = camera.WorldToScreenPoint(head);
                var onScreen = InFront(screen, w);
                if (!PoliceAwarenessSight.Shows(awareness, onScreen))
                {
                    continue;
                }

                var pursuing = police.Stage == PoliceStage.Pursuing;
                var open = pursuing ? 1f : Mathf.Clamp01(police.Suspicion / PoliceEscalation.PursuingThreshold);
                var scale = pursuing ? 1.15f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
                var size = new Vector2(EyeWidth, EyeHeight) * scale;
                Vector2 center;
                if (onScreen)
                {
                    center = new Vector2(screen.x, Mathf.Clamp(h - screen.y - size.y / 2f, EdgeMargin + size.y / 2f, h - EdgeMargin - size.y / 2f));
                }
                else
                {
                    var left = camera.transform.InverseTransformPoint(head).x < 0f;
                    center = new Vector2(left ? EdgeMargin + size.x / 2f : w - EdgeMargin - size.x / 2f, h * 0.32f + edgeRow * (size.y + 10f));
                    edgeRow++;
                }

                var texture = _eyes[Mathf.Clamp(Mathf.RoundToInt(open * (EyeLevels - 1)), 0, EyeLevels - 1)];
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.92f);
                GUI.DrawTexture(new Rect(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y), texture);
                GUI.color = previous;
            }
        }

        /// <summary>作った目の印を捨てる（HUD が消えるとき）。</summary>
        public void Release()
        {
            if (_eyes == null)
            {
                return;
            }

            foreach (var eye in _eyes)
            {
                Object.Destroy(eye);
            }

            _eyes = null;
        }

        /// <summary>目の印を、開き具合の段ごとに作る（0＝閉じた一本線、最後＝見開いた目）。</summary>
        static Texture2D[] BuildEyes()
        {
            const int width = 64;
            const int height = 40;
            var eyes = new Texture2D[EyeLevels];
            for (var level = 0; level < EyeLevels; level++)
            {
                var open = level / (float)(EyeLevels - 1);
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var edge = 2.2f / (height / 2f);
                var iris = Mathf.Lerp(0f, 8.5f, Mathf.Clamp01(open * 1.6f));
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var u = (x + 0.5f) / width * 2f - 1f;
                        var v = (y + 0.5f) / height * 2f - 1f;
                        var lid = 0.9f * open * (1f - u * u);
                        var color = Color.clear;
                        if (Mathf.Abs(u) < 0.92f && Mathf.Abs(v) <= Mathf.Max(lid, edge * 0.6f))
                        {
                            var rim = Mathf.Abs(v) > lid - edge || Mathf.Abs(u) > 0.86f;
                            var inIris = new Vector2(u * width / 2f, v * height / 2f).magnitude < iris;
                            color = rim || inIris ? Color.white : new Color(0f, 0f, 0f, 0.55f);
                        }

                        texture.SetPixel(x, y, color);
                    }
                }

                texture.Apply();
                eyes[level] = texture;
            }

            return eyes;
        }

        /// <summary>
        /// 視界の中（前にいて、左右が画面に収まる）か。上下にはみ出すだけなら中に数え、画面の上端・下端に寄せて描く
        /// （すぐ目の前の警官の頭の上は画面の上に出てしまうが、背後にいるわけではない）。
        /// </summary>
        static bool InFront(Vector3 screen, float w) => screen.z > 0f && screen.x >= 0f && screen.x <= w;

        void DrawBacked(Rect rect, GUIContent content, float alpha)
        {
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f * alpha);
            GUI.DrawTexture(new Rect(rect.x - 10f, rect.y - 3f, rect.width + 20f, rect.height + 6f), _white);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, content, _bubble);
            GUI.color = previous;
        }
    }
}
