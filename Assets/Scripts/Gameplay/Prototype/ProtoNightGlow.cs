using System;
using System.Collections.Generic;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 夜に灯る窓と、提灯・光る看板、建物に付いた小さな灯り（表現であって権威ではない）。建物の部品 LitGlass…／Glow… は
    /// 発光の材質を共有しており、夜の濃さ（<see cref="ProtoDayNight.Clock"/> の NightBlend01）に合わせて明るさを上げる。
    /// 提灯・店先・裏口の灯りは本物の点光源で、地面や壁に光を落とす（自発光だけだと面が光るだけで周りが暗いままになる）。
    /// どの窓が灯るか（二割強）は生成スクリプトが決める。
    ///
    /// 共有の材質（資産）を実行中に書き換えると、エディタでは資産そのものが変わってしまう。そこで起動時に複製を作り、
    /// その材質を使う描画器をすべて複製へ差し替えて、複製の明るさだけを動かす。資産の発光は黒のまま（昼の見た目）。
    /// </summary>
    public sealed class ProtoNightGlow : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public Material source;
            [ColorUsage(false, true)] public Color nightEmission;
        }

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] Entry[] _entries = new Entry[0];

        [Tooltip("この下にある Light を、夜の濃さに合わせて点ける（置いたときの明るさが夜の明るさ）。")]
        [SerializeField] Transform[] _lightRoots = new Transform[0];

        Material[] _instances = new Material[0];
        Light[] _lights = new Light[0];
        float[] _lightIntensity = new float[0];
        float _lastBlend = -1f;

        public void Configure(ProtoDayNight dayNight, Entry[] entries, Transform[] lightRoots)
        {
            _dayNight = dayNight;
            _entries = entries ?? new Entry[0];
            _lightRoots = lightRoots ?? new Transform[0];
        }

        void Awake()
        {
            var lights = new List<Light>();
            foreach (var root in _lightRoots)
            {
                if (root != null)
                {
                    lights.AddRange(root.GetComponentsInChildren<Light>(true));
                }
            }

            _lights = lights.ToArray();
            _lightIntensity = new float[_lights.Length];
            for (var i = 0; i < _lights.Length; i++)
            {
                _lightIntensity[i] = _lights[i].intensity;
            }

            _instances = new Material[_entries.Length];
            for (var i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].source == null)
                {
                    continue;
                }

                _instances[i] = new Material(_entries[i].source) { name = _entries[i].source.name + " (Night)" };
                _instances[i].EnableKeyword("_EMISSION");
            }

            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var materials = r.sharedMaterials;
                var changed = false;
                for (var m = 0; m < materials.Length; m++)
                {
                    for (var i = 0; i < _entries.Length; i++)
                    {
                        if (_instances[i] != null && materials[m] == _entries[i].source)
                        {
                            materials[m] = _instances[i];
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    r.sharedMaterials = materials;
                }
            }
        }

        void OnDestroy()
        {
            foreach (var m in _instances)
            {
                if (m != null)
                {
                    Destroy(m);
                }
            }
        }

        void Update()
        {
            var t = _dayNight != null && _dayNight.Clock != null ? _dayNight.Clock.NightBlend01 : 0f;
            if (Mathf.Approximately(t, _lastBlend))
            {
                return;
            }

            _lastBlend = t;
            for (var i = 0; i < _instances.Length; i++)
            {
                if (_instances[i] != null)
                {
                    _instances[i].SetColor(EmissionColor, _entries[i].nightEmission * t);
                }
            }

            for (var i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    _lights[i].enabled = t > 0.02f;
                    _lights[i].intensity = _lightIntensity[i] * t;
                }
            }
        }
    }
}
