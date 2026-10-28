using KyoumoMushoku.Core.Police;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 警官の体と音（<see cref="ProtoPolice"/> の段階を、体と耳で読めるようにする）。状態は警官が持ち、ここは見せて鳴らすだけ。
    ///
    /// 体：部品分割の剛体（脚・腕・頭）を振る。歩けば手足が振れ、走れば大きく振れる。気づいたら頭がこちらを向き、
    /// 警告するときは右腕でこちらを指す。部品の軸は模型では中心にあるので、起動時に肩・股・首の位置へ軸を足す
    /// （実行時の階層だけを変え、シーンと模型は書き換えない）。
    /// 音：立体音で鳴らす（どちらから来るかが耳で分かる）。追い始めに合図（素材集の音・<see cref="SliceTuningAsset.PoliceAlarm"/>）、
    /// 追ってくる間だけ小さな靴音、口を開くたびに短い声。背後から追われても、耳で気づける。
    /// 呼子の合成は鋭すぎ、巡回中の足音は煩かったのでやめた（2026-10-28・作者の指摘）。
    /// </summary>
    [RequireComponent(typeof(ProtoPolice))]
    public sealed class ProtoPoliceBody : MonoBehaviour
    {
        const float WalkSwingDegrees = 24f;
        const float RunSwingDegrees = 48f;
        const float RunSpeed = 4f;
        const float StrideMeters = 1.4f;
        const float PointDegrees = -80f;
        const float HeadTurnLimit = 70f;
        const float BobMeters = 0.035f;
        const float StepMinSpeed = 2.5f;

        ProtoPolice _police;
        Transform _model;
        Transform _legL;
        Transform _legR;
        Transform _armL;
        Transform _armR;
        Transform _head;
        Vector3 _lateral;
        Vector3 _modelRest;
        AudioSource _audio;
        Vector3 _lastPosition;
        float _phase;
        float _point;
        float _headYaw;
        PoliceStage _lastStage;

        void Awake()
        {
            _police = GetComponent<ProtoPolice>();
            _model = transform.Find("Model");
            if (_model != null)
            {
                _modelRest = _model.localPosition;
                var head = _model.Find("Head");
                var visor = _model.Find("Visor");
                var forward = head != null && visor != null
                    ? Vector3.ProjectOnPlane(Center(visor) - Center(head), Vector3.up).normalized
                    : transform.forward;
                _lateral = _model.InverseTransformDirection(Vector3.Cross(Vector3.up, forward)).normalized;
                _legL = Pivot("LegL", top: true);
                _legR = Pivot("LegR", top: true);
                _armL = Pivot("ArmL", top: true);
                _armR = Pivot("ArmR", top: true);
                _head = Pivot("Head", top: false, "Cap", "Visor", "CapBadge");
            }

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.minDistance = 2f;
            _audio.maxDistance = 32f;
            _lastPosition = transform.position;
        }

        void OnEnable() => GetComponent<ProtoPolice>().Spoke += OnSpoke;

        void OnDisable() => GetComponent<ProtoPolice>().Spoke -= OnSpoke;

        void OnSpoke()
        {
            _audio.pitch = 1f;
            _audio.PlayOneShot(ProtoSounds.Voice, 0.8f);
        }

        void LateUpdate()
        {
            var stage = _police.Stage;
            var slice = _police.Slice;
            if (stage == PoliceStage.Pursuing && _lastStage != PoliceStage.Pursuing && slice != null && slice.PoliceAlarm != null)
            {
                _audio.pitch = 1f;
                _audio.PlayOneShot(slice.PoliceAlarm, slice.PoliceAlarmVolume);
            }

            _lastStage = stage;

            var delta = Time.deltaTime;
            if (delta <= 0f || _model == null)
            {
                return;
            }

            // 連れて行く間の瞬間移動のような大きな跳びは、歩きとして数えない。
            var moved = Vector3.ProjectOnPlane(transform.position - _lastPosition, Vector3.up).magnitude;
            _lastPosition = transform.position;
            var speed = moved > 3f ? 0f : moved / delta;

            // 手足の振り。靴音は追ってくる間だけ、半歩ごとに小さく（巡回の足音は鳴らさない＝煩くしない）。
            var previousSide = Mathf.Sign(Mathf.Sin(_phase));
            _phase += speed / StrideMeters * Mathf.PI * delta;
            var running = Mathf.Clamp01(speed / RunSpeed);
            var swing = speed < 0.1f ? 0f : Mathf.Lerp(WalkSwingDegrees, RunSwingDegrees, running) * Mathf.Sin(_phase);
            if (stage == PoliceStage.Pursuing && speed > StepMinSpeed && Mathf.Sign(Mathf.Sin(_phase)) != previousSide && slice != null)
            {
                _audio.pitch = Random.Range(0.9f, 1.1f);
                _audio.PlayOneShot(ProtoSounds.Step, slice.PoliceStepVolume);
            }

            // 警告の間は右腕でこちらを指す。
            _point = Mathf.MoveTowards(_point, stage == PoliceStage.Warning ? 1f : 0f, delta * 4f);
            Swing(_legL, swing);
            Swing(_legR, -swing);
            Swing(_armL, -swing * 0.8f);
            if (_armR != null)
            {
                _armR.localRotation = Quaternion.AngleAxis(Mathf.Lerp(swing * 0.8f, PointDegrees, _point), _lateral);
            }

            _model.localPosition = _modelRest + Vector3.up * (Mathf.Abs(Mathf.Sin(_phase)) * BobMeters * running);

            // 気づいている間は、頭がこちらを向く（体の向きから左右に 70 度まで）。
            var target = 0f;
            if (stage >= PoliceStage.Noticing && _police.Player != null)
            {
                var toPlayer = Vector3.ProjectOnPlane(_police.Player.transform.position - transform.position, Vector3.up);
                target = Mathf.Clamp(Vector3.SignedAngle(transform.forward, toPlayer, Vector3.up), -HeadTurnLimit, HeadTurnLimit);
            }

            _headYaw = Mathf.MoveTowards(_headYaw, target, delta * 240f);
            if (_head != null)
            {
                _head.localRotation = Quaternion.AngleAxis(_headYaw, Vector3.up);
            }
        }

        void Swing(Transform pivot, float degrees)
        {
            if (pivot != null)
            {
                pivot.localRotation = Quaternion.AngleAxis(degrees, _lateral);
            }
        }

        /// <summary>
        /// 部品の上端（肩・股）か下端（首）に軸を置き、部品をその子にする。<paramref name="riders"/> は一緒に動く部品（帽子など）。
        /// </summary>
        Transform Pivot(string part, bool top, params string[] riders)
        {
            var child = _model.Find(part);
            var renderer = child != null ? child.GetComponent<Renderer>() : null;
            if (renderer == null)
            {
                return null;
            }

            var bounds = renderer.bounds;
            var pivot = new GameObject(part + "Pivot").transform;
            pivot.SetParent(_model, false);
            pivot.position = new Vector3(bounds.center.x, top ? bounds.max.y : bounds.min.y, bounds.center.z);
            child.SetParent(pivot, true);
            foreach (var name in riders)
            {
                var rider = _model.Find(name);
                if (rider != null)
                {
                    rider.SetParent(pivot, true);
                }
            }

            return pivot;
        }

        static Vector3 Center(Transform part)
        {
            var renderer = part.GetComponent<Renderer>();
            return renderer != null ? renderer.bounds.center : part.position;
        }
    }
}
