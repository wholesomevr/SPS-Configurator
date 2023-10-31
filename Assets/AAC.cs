using System.Collections.Generic;
using UnityEngine;
using AnimatorAsCode.V1.VRC;
using AnimatorAsCode.V1;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using VF;
using VF.Model;
using VF.Model.Feature;
using VF.Utils;
using SerializedObject = UnityEditor.SerializedObject;

namespace Wholesome
{
    public class AAC : MonoBehaviour
    {
    }

    [CustomEditor(typeof(AAC))]
    public class AACEditor : Editor
    {
        
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Build"))
            {
                var obj = target as AAC;
                AACTools.Create(obj.gameObject);
            }
            
        }
    }

    public class ControllerBuilder
    {
        public readonly AacFlBase Aac;
        private Dictionary<string, AacFlClip> _oneClips = new Dictionary<string, AacFlClip>();
        private Dictionary<string, AacFlClip> _minusOneClips = new Dictionary<string, AacFlClip>();
        private Dictionary<string, AacFlClip> _zeroClips = new Dictionary<string, AacFlClip>();

        private Dictionary<float, AacFlFloatParameter> _constants = new Dictionary<float, AacFlFloatParameter>();
        private Dictionary<string, AacFlFloatParameter> _params1000 = new Dictionary<string, AacFlFloatParameter>();

        private AacFlLayer _timeLayer;
        private AacFlFloatParameter _time;
        private AacFlFloatParameter _lastTime;
        public readonly AacFlFloatParameter FrameTime;
        private AacFlLayer _layer;
        private DBTBuilder _dbt;

        public ControllerBuilder(AacFlBase aac, AacFlController controller)
        {
            Aac = aac;
            _timeLayer= controller.NewLayer("Time");
            _time = _timeLayer.FloatParameter("Time");
            _lastTime = _timeLayer.FloatParameter("LastTime");
            FrameTime = _timeLayer.FloatParameter("FrameTime");
            _timeLayer.NewState("Time")
                .WithAnimation(aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.AnimatesAnimator(_time)
                            .WithSecondsUnit(keyframes =>
                            {
                                keyframes
                                    .Linear(0, 0)
                                    .Linear(20_000, 20_000);
                            });
                    }));
            _layer = controller.NewLayer();
            _dbt = DBT();
            _layer.NewState("Hold")
                .WithAnimation(_dbt
                    .Set(_lastTime, _time)
                    .Subtract(FrameTime, _time, _lastTime)
                    .BlendTree);
        }

        public DBTBuilder DBT()
        {
            return new DBTBuilder(this);
        }

        public AacFlClip ValueClipUncached(AacFlFloatParameter parameter, float value)
        {
            var clip = Aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(parameter)
                        .WithOneFrame(value);
                });
            return clip;
        }
        
        public AacFlClip OneClip(AacFlFloatParameter parameter)
        {
            AacFlClip clip;
            if (!_oneClips.TryGetValue(parameter.Name, out clip))
            {
                clip = Aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.AnimatesAnimator(parameter)
                            .WithOneFrame(1);
                    });
                _oneClips.Add(parameter.Name, clip);
            }
            return clip;
        }
        
        public AacFlClip MinusOneClip(AacFlFloatParameter parameter)
        {
            AacFlClip clip;
            if (!_minusOneClips.TryGetValue(parameter.Name, out clip))
            {
                clip = Aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.AnimatesAnimator(parameter)
                            .WithOneFrame(-1);
                    });
                _minusOneClips.Add(parameter.Name, clip);
            }
            return clip;
        }
        
        public AacFlClip ZeroClip(AacFlFloatParameter parameter)
        {
            AacFlClip clip;
            if (!_zeroClips.TryGetValue(parameter.Name, out clip))
            {
                clip = Aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.AnimatesAnimator(parameter)
                            .WithOneFrame(0);
                    });
                _zeroClips.Add(parameter.Name, clip);
            }
            return clip;
        }

        public AacFlFloatParameter Constant(float value)
        {
            AacFlFloatParameter constant;
            if (!_constants.TryGetValue(value, out constant))
            {
                constant = _layer.FloatParameter(value.ToString());
                _layer.OverrideValue(constant, value);
                _constants.Add(value, constant);
            }

            return constant;
        }

        public AacFlFloatParameter Parameter1000(AacFlFloatParameter parameter)
        {
            AacFlFloatParameter param1000;
            if (!_params1000.TryGetValue(parameter.Name, out param1000))
            {
                param1000 = _layer.FloatParameter($"{parameter.Name}1000");
                _params1000.Add(parameter.Name, param1000);
            }

            return param1000;
        }

        public void Hold(AacFlFloatParameter parameter)
        {
            _dbt.Hold(parameter);
        }
    }

    public class DBTBuilder
    {
        private ControllerBuilder _controllerBuilder;
        public readonly AacFlBlendTreeDirect BlendTree;

        internal DBTBuilder(ControllerBuilder controllerBuilder)
        {
            _controllerBuilder = controllerBuilder;
            BlendTree = _controllerBuilder.Aac.NewBlendTree().Direct();
        }

        public DBTBuilder Multiply(AacFlFloatParameter result, params AacFlFloatParameter[] parameters)
        {
            AacFlBlendTreeDirect multiply = null;
            foreach (var param in parameters)
            {
                if (multiply == null)
                {
                    multiply = _controllerBuilder.Aac.NewBlendTree().Direct().WithAnimation(_controllerBuilder.OneClip(result), param);
                }
                else
                {
                    multiply = _controllerBuilder.Aac.NewBlendTree().Direct().WithAnimation(multiply, param);
                }
            }

            BlendTree.WithAnimation(multiply, _controllerBuilder.Constant(1));
            return this;
        }
        
        public DBTBuilder Set(AacFlFloatParameter result, AacFlFloatParameter parameter)
        {
            return Multiply(result, parameter);
        }
        
        public DBTBuilder Set0(AacFlFloatParameter result)
        {
            BlendTree.WithAnimation(_controllerBuilder.ZeroClip(result), _controllerBuilder.Constant(1));
            return this;
        }
        
        public DBTBuilder Add(AacFlFloatParameter result, params AacFlFloatParameter[] parameters)
        {
            foreach (var param in parameters)
            {
                BlendTree.WithAnimation(_controllerBuilder.OneClip(result), param);
            }
            return this;
        }
        
        public DBTBuilder Add1D(AacFlFloatParameter result, AacFlFloatParameter parameter1, AacFlFloatParameter parameter2)
        {
            BlendTree.WithAnimation(_controllerBuilder.Aac.NewBlendTree().Simple1D(parameter1)
                .WithAnimation(_controllerBuilder.ValueClipUncached(result, -1f), -1f)
                .WithAnimation(_controllerBuilder.ValueClipUncached(result, 1f), 1f)
                , _controllerBuilder.Constant(1));
            BlendTree.WithAnimation(_controllerBuilder.Aac.NewBlendTree().Simple1D(parameter2)
                    .WithAnimation(_controllerBuilder.ValueClipUncached(result, -1f), -1f)
                    .WithAnimation(_controllerBuilder.ValueClipUncached(result, 1f), 1f)
                , _controllerBuilder.Constant(1));
            return this;
        }
        
        public DBTBuilder Subtract(AacFlFloatParameter result, params AacFlFloatParameter[] parameters)
        {
            BlendTree.WithAnimation(_controllerBuilder.OneClip(result), parameters[0]);
            for (int i = 1; i < parameters.Length; i++)
            {
                BlendTree.WithAnimation(_controllerBuilder.MinusOneClip(result), parameters[i]);
            }
            return this;
        }

        public DBTBuilder Divide(AacFlFloatParameter result, AacFlFloatParameter dividend, AacFlFloatParameter divisor)
        {
            Multiply(_controllerBuilder.Parameter1000(divisor), divisor, _controllerBuilder.Constant(1000));
            var divisionTree = _controllerBuilder.Aac.NewBlendTree().Direct()
                .WithAnimation(_controllerBuilder.Aac.DummyClipLasting(1f, AacFlUnit.Frames), _controllerBuilder.Parameter1000(divisor))
                .WithAnimation(_controllerBuilder.OneClip(result), _controllerBuilder.Constant(1e-3f));
            using (var so = new SerializedObject(divisionTree.BlendTree))
            {
                so.FindProperty("m_NormalizedBlendValues").boolValue = true;
                so.ApplyModifiedProperties();
            }

            BlendTree.WithAnimation(_controllerBuilder.Aac.NewBlendTree().Direct()
                .WithAnimation(divisionTree, _controllerBuilder.Constant(1e6f)), dividend);
            return this;
        }

        public DBTBuilder DivideConstant(AacFlFloatParameter result, AacFlFloatParameter dividend, float constant)
        {
            var one = _controllerBuilder.Constant(1);
            var constantMinus1 = _controllerBuilder.Constant(constant - 1);
            var divisionTree = _controllerBuilder.Aac.NewBlendTree().Direct()
                .WithAnimation(_controllerBuilder.Aac.DummyClipLasting(1f, AacFlUnit.Frames), constantMinus1)
                .WithAnimation(_controllerBuilder.OneClip(result), one);
            using (var so = new SerializedObject(divisionTree.BlendTree))
            {
                so.FindProperty("m_NormalizedBlendValues").boolValue = true;
                so.ApplyModifiedProperties();
            }

            BlendTree.WithAnimation(divisionTree, dividend);
            return this;
        }

        public DBTBuilder Clamp(AacFlFloatParameter result, AacFlFloatParameter parameter, float higherBound)
        {
            BlendTree
                .WithAnimation(_controllerBuilder.Aac.NewBlendTree().Simple1D(parameter)
                    .WithAnimation(_controllerBuilder.ZeroClip(result), 0)
                    .WithAnimation(_controllerBuilder.ValueClipUncached(result, higherBound), higherBound), _controllerBuilder.Constant(1));
            return this;
        }

        public DBTBuilder SmoothSliding(AacFlFloatParameter result, AacFlFloatParameter parameter, AacFlLayer layer, int windowSize = 5)
        {
            var window = new List<AacFlFloatParameter>();
            window.Add(parameter);
            for (var i = 0; i < windowSize - 1; i++)
            {
                var param = layer.FloatParameter($"{parameter.Name}{i + 1}");
                window.Add(param);
            }

            
            for (var i = windowSize-2; i >= 0 ; i--)
            {
                Set(window[i+1], window[i]);
            }

            var sum = layer.FloatParameter($"{parameter.Name}Sum");
            Add(sum, window.ToArray());
            DivideConstant(result, sum, windowSize);
            return this;
        }
        
        public DBTBuilder SmoothExp(AacFlFloatParameter result, AacFlFloatParameter parameter, AacFlLayer layer,
            float stepSize = 12)
        {
            var lastValue = layer.FloatParameter("LastValue");
            var delta = layer.FloatParameter("Delta");
            Multiply(delta, _controllerBuilder.Constant(stepSize), _controllerBuilder.FrameTime);
            var deltaClamped = layer.FloatParameter("ClampedDelta");
            Clamp(deltaClamped, delta, 0.9f);
            var oneMinusDelta = layer.FloatParameter("1-Delta");
            Subtract(oneMinusDelta, _controllerBuilder.Constant(1), deltaClamped);
            Multiply(result, deltaClamped, parameter);
            Multiply(result, oneMinusDelta, lastValue);
            Set(lastValue, parameter);
            return this;
        }

        public DBTBuilder Hold(AacFlFloatParameter parameter)
        {
            BlendTree.WithAnimation(_controllerBuilder.OneClip(parameter), parameter);
            return this;
        }
    }

    public static class AACTools
    {

        [MenuItem("Wholesome/AAC Test")]
        private static void CreateStartEnd()
        {
            var gameObject = Selection.activeGameObject;
            AnimatorController assetContainer = new AnimatorController();
            AssetDatabase.CreateAsset(assetContainer, "Assets/AAC_SFX_Test.controller");
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = "Wholesome",
                AnimatorRoot = gameObject.transform,
                DefaultValueRoot = gameObject.transform,
                AssetContainer = assetContainer,
                AssetKey = "WH",
                DefaultsProvider = new AacDefaultsProvider(writeDefaults: true)
            });
            var controller = aac.NewAnimatorController();
            var cb = new ControllerBuilder(aac, controller);

            var velocityLayer = controller.NewLayer("Velocity");
            var smoothedProximity = velocityLayer.FloatParameter("SmoothedProximity");
            var proximity = velocityLayer.FloatParameter("Proximity");
            var lastProximity = velocityLayer.FloatParameter("LastProximity");
            var proximityDelta = velocityLayer.FloatParameter("ProximityDelta");
            var proximityVelocity = velocityLayer.FloatParameter("ProximityVelocity");
            var velocityState = velocityLayer.NewState("Velocity")
                .WithAnimation(cb.DBT()
                    //.SmoothSliding(smoothedProximity, proximity, velocityLayer, 10)
                    .SmoothExp(smoothedProximity, proximity, velocityLayer, 12)
                    .Set(lastProximity, smoothedProximity)
                    .Subtract(proximityDelta, smoothedProximity, lastProximity)
                    .Divide(proximityVelocity, proximityDelta, cb.FrameTime)
                    .BlendTree);
            
            var startLayer = controller.NewLayer("Start");
            var start = startLayer.FloatParameter("Start");
            var startPlusOffset = startLayer.FloatParameter("Start+Offset");
            var startDiff = startLayer.FloatParameter("StartDiff");
            var startPlusOffsetDiff = startLayer.FloatParameter("Start+OffsetDiff");
            
            var endLayer = controller.NewLayer("End");
            var end = endLayer.FloatParameter("End");
            var endMinusOffset = endLayer.FloatParameter("End-Offset");
            var endDiff = endLayer.FloatParameter("EndDiff");
            var endMinusOffsetDiff = endLayer.FloatParameter("End-OffsetDiff");

            var startCheck = startLayer.NewState("Check")
                .WithAnimation(cb.DBT()
                    .Subtract(startDiff, proximity, start)
                    .BlendTree
                );
            var startSet = startLayer.NewState("Set")
                .WithAnimation(cb.DBT()
                    .Set(start, proximity)
                    .BlendTree);
            startCheck.TransitionsTo(startSet)
                .When(startDiff.IsLessThan(0));
            startSet.Exits().When(cb.Constant(1).IsGreaterThan(0));
            
            var endCheck = endLayer.NewState("Check")
                .WithAnimation(cb.DBT()
                    .Subtract(endDiff, end, proximity)
                    .BlendTree
                );
            var endSet = endLayer.NewState("Set")
                .WithAnimation(cb.DBT()
                    .Set(end, proximity)
                    .BlendTree);
            endCheck.TransitionsTo(endSet).When(endDiff.IsLessThan(0));
            endSet.Exits().When(cb.Constant(1).IsGreaterThan(0));

            var diffLayer = controller.NewLayer("Diff");
            var diff = diffLayer.NewState("Diff")
                .WithAnimation(cb.DBT()
                    .Add(startPlusOffset, start, cb.Constant(0.01f))
                    .Subtract(endMinusOffset, end, cb.Constant(0.01f))
                    .Subtract(startPlusOffsetDiff, startPlusOffset, proximity)
                    .Subtract(endMinusOffsetDiff, proximity, endMinusOffset)
                    .BlendTree);

            var resetLayer = controller.NewLayer("Resetter");
            var resetIdleStart = resetLayer.NewState("Idle Start");
                /*.WithAnimation(cb.DBT()
                    .Subtract(startPlusOffsetDiff, startPlusOffset, proximity)
                    .BlendTree);*/
            var resetEnd = resetLayer.NewState("Reset End")
                .WithAnimation(cb.DBT()
                    .Set0(end)
                    .BlendTree);
            var resetIdleEnd = resetLayer.NewState("Idle End");
                /*.WithAnimation(cb.DBT()
                    .Subtract(endMinusOffsetDiff, proximity, endMinusOffset)
                    .BlendTree);*/
            var resetStart = resetLayer.NewState("Reset Start")
                .WithAnimation(cb.DBT()
                    .Set(start, cb.Constant(1))
                    .BlendTree);
            resetIdleStart.TransitionsTo(resetEnd).When(startPlusOffsetDiff.IsLessThan(0));
            resetEnd.TransitionsTo(resetIdleEnd).When(cb.Constant(1).IsGreaterThan(0));
            resetIdleEnd.TransitionsTo(resetStart).When(endMinusOffsetDiff.IsLessThan(0));
            resetStart.Exits().When(cb.Constant(1).IsGreaterThan(0));
            
            var sfxInLayer = controller.NewLayer("SFX In");
            var randomIn = sfxInLayer.IntParameter("Random In");
            var inIdle = sfxInLayer.NewState("Idle");
            var inTransform = gameObject.transform.Find("Sound/In");
            var ins = CreateAudioStates(aac, sfxInLayer, inTransform);
            inIdle.DrivingRandomizesUnsynced(randomIn, 0, ins.Count - 1);
            for (var i = 0; i < ins.Count; i++)
            {
                inIdle.TransitionsTo(ins[i])
                    .When(randomIn.IsEqualTo(i))
                    .And(startPlusOffsetDiff.IsLessThan(0));
                ins[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(endMinusOffsetDiff.IsLessThan(0));

            }

            var sfxOutLayer = controller.NewLayer("SFX Out");
            var randomOut = sfxOutLayer.IntParameter("Random Out");
            var outIdle = sfxOutLayer.NewState("Idle");
            var outTransform = gameObject.transform.Find("Sound/Out");
            var outs = CreateAudioStates(aac, sfxOutLayer, outTransform);
            outIdle.DrivingRandomizesUnsynced(randomOut, 0, outs.Count - 1);
            for (var i = 0; i < outs.Count; i++)
            {
                outIdle.TransitionsTo(outs[i])
                    .When(randomOut.IsEqualTo(i))
                    .And(endMinusOffsetDiff.IsLessThan(0));
                /*outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(proximity.IsLessThan(0.001f));*/
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(startPlusOffsetDiff.IsLessThan(0));
            }

            /*var sfxClapLayer = sfx.NewLayer("SFX Clap");
            var randomClap = sfxClapLayer.IntParameter("Random Clap");
            var clapIdle = sfxClapLayer.NewState("Idle");
            var clapIn = sfxClapLayer.NewState("In");
            var clapTransform = gameObject.transform.Find("Sound/Clap");
            var claps = CreateAudioStates(aac, sfxClapLayer, clapTransform);
            clapIdle.DrivingRandomizesUnsynced(randomClap, 0, claps.Count - 1);
            for (var i = 0; i < claps.Count; i++)
            {
                clapIdle.TransitionsTo(clapIn)
                    .When(inertia.IsGreaterThan(0.03f));
                clapIn.TransitionsTo(claps[i])
                    .When(randomClap.IsEqualTo(i))
                    .And(inertia.IsLessThan(0.02f))
                    .And(maxInertia.IsGreaterThan(0.06f));
                clapIn.Exits()
                    .When(inertia.IsLessThan(0.02f));
                claps[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(inertia.IsLessThan(-0.03f));
            }*/
            
            
            var debugLayer = controller.NewLayer("Debug");
            debugLayer.NewState("Debug")
                .WithAnimation(aac.NewBlendTree().Simple1D(proximityVelocity)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, -1)), -10)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, 1)), 10)
                );
            /*.WithAnimation(aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.Animates(gameObject.transform.Find("Sender/Container"), "m_LocalScale.z")
                        .WithSecondsUnit(keyframes => { keyframes.Linear(0, 0).Linear(1, 1); });
                })).MotionTime(inertia);*/
            
            cb.Hold(start);
            cb.Hold(end);
            AssetDatabase.SaveAssets();
            gameObject.GetComponent<Animator>().runtimeAnimatorController = controller.AnimatorController;
        }
        
        [MenuItem("Wholesome/AAC Delta Test")]
        private static void CreateDelta()
        {
            var gameObject = Selection.activeGameObject;
            AnimatorController assetContainer = new AnimatorController();
            AssetDatabase.CreateAsset(assetContainer, "Assets/AAC_SFX_Delta_Test.controller");
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = "Wholesome",
                AnimatorRoot = gameObject.transform,
                DefaultValueRoot = gameObject.transform,
                AssetContainer = assetContainer,
                AssetKey = "WH",
                DefaultsProvider = new AacDefaultsProvider(writeDefaults: true)
            });
            var controller = aac.NewAnimatorController("SFX");
            var cb = new ControllerBuilder(aac, controller);

            var velocityLayer = controller.NewLayer("Velocity");
            var smoothedProximity = velocityLayer.FloatParameter("SmoothedProximity");
            var proximity = velocityLayer.FloatParameter("Proximity");
            var lastProximity = velocityLayer.FloatParameter("LastProximity");
            var proximityDelta = velocityLayer.FloatParameter("ProximityDelta");
            var smoothedProximityDelta = velocityLayer.FloatParameter("SmoothedProximityDelta");
            var proximityVelocity = velocityLayer.FloatParameter("ProximityVelocity");
            var traveled = velocityLayer.FloatParameter("Traveled");
            var velocityState = velocityLayer.NewState("Velocity")
                .WithAnimation(cb.DBT()
                    //.SmoothSliding(smoothedProximity, proximity, velocityLayer, 10)
                    //.SmoothExp(smoothedProximity, proximity, velocityLayer, 12)
                    .Subtract(proximityDelta, proximity, lastProximity)
                    .SmoothSliding(smoothedProximityDelta, proximityDelta, velocityLayer, 5)
                    .Divide(proximityVelocity, smoothedProximityDelta, cb.FrameTime)
                    .Add1D(traveled, traveled, proximityDelta)
                    .Set(lastProximity, proximity)
                    .BlendTree);

            var resetLayer = controller.NewLayer("Resetter");
            var resetIdleStart = resetLayer.NewState("Idle Start");
                /*.WithAnimation(cb.DBT()
                    .Subtract(startPlusOffsetDiff, startPlusOffset, proximity)
                    .BlendTree);*/
            var resetEnd = resetLayer.NewState("Reset End")
                .WithAnimation(cb.DBT()
                    .Set0(traveled)
                    .BlendTree);
            var resetIdleEnd = resetLayer.NewState("Idle End");
                /*.WithAnimation(cb.DBT()
                    .Subtract(endMinusOffsetDiff, proximity, endMinusOffset)
                    .BlendTree);*/
            var resetStart = resetLayer.NewState("Reset Start")
                .WithAnimation(cb.DBT()
                    .Set0(traveled)
                    .BlendTree);
            
            resetIdleStart.TransitionsTo(resetEnd).When(proximityDelta.IsLessThan(0));
            resetEnd.TransitionsTo(resetIdleEnd).When(cb.Constant(1).IsGreaterThan(0));
            resetIdleEnd.TransitionsTo(resetStart).When(proximityDelta.IsGreaterThan(0));
            resetStart.Exits().When(cb.Constant(1).IsGreaterThan(0));
            
            var sfxInLayer = controller.NewLayer("SFX In");
            var randomIn = sfxInLayer.IntParameter("Random In");
            var inIdle = sfxInLayer.NewState("Idle");
            var inTransform = gameObject.transform.Find("Sound/In");
            var ins = CreateAudioStates(aac, sfxInLayer, inTransform);
            inIdle.DrivingRandomizesUnsynced(randomIn, 0, ins.Count - 1);
            for (var i = 0; i < ins.Count; i++)
            {
                inIdle.TransitionsTo(ins[i])
                    .When(randomIn.IsEqualTo(i))
                    .And(traveled.IsGreaterThan(0.01f));
                ins[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(traveled.IsLessThan(-0.01f));

            }

            var sfxOutLayer = controller.NewLayer("SFX Out");
            var randomOut = sfxOutLayer.IntParameter("Random Out");
            var outIdle = sfxOutLayer.NewState("Idle");
            var outTransform = gameObject.transform.Find("Sound/Out");
            var outs = CreateAudioStates(aac, sfxOutLayer, outTransform);
            outIdle.DrivingRandomizesUnsynced(randomOut, 0, outs.Count - 1);
            for (var i = 0; i < outs.Count; i++)
            {
                outIdle.TransitionsTo(outs[i])
                    .When(randomOut.IsEqualTo(i))
                    .And(traveled.IsLessThan(-0.01f));
                /*outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(proximity.IsLessThan(0.001f));*/
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(traveled.IsGreaterThan(0.01f));
            }

            var sfxClapLayer = controller.NewLayer("SFX Clap");
            var randomClap = sfxClapLayer.IntParameter("Random Clap");
            var clapIdle = sfxClapLayer.NewState("Idle");
            var clapWait = sfxClapLayer.NewState("Wait");
            var clapTransform = gameObject.transform.Find("Sound/Clap");
            var claps = CreateAudioStates(aac, sfxClapLayer, clapTransform);
            clapIdle.DrivingRandomizesUnsynced(randomClap, 0, claps.Count - 1);
            clapIdle.TransitionsTo(clapWait)
                .When(proximityVelocity.IsGreaterThan(2f));
            for (var i = 0; i < claps.Count; i++)
            {
                clapWait.TransitionsTo(claps[i])
                    .When(randomClap.IsEqualTo(i))
                    .And(proximityVelocity.IsLessThan(1f));
                claps[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(traveled.IsLessThan(-0.01f));
            }
            
            
            var debugLayer = controller.NewLayer("Debug");
            debugLayer.NewState("Debug")
                .WithAnimation(aac.NewBlendTree().Simple1D(proximityVelocity)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, -1)), -10)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, 1)), 10)
                );
            /*.WithAnimation(aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.Animates(gameObject.transform.Find("Sender/Container"), "m_LocalScale.z")
                        .WithSecondsUnit(keyframes => { keyframes.Linear(0, 0).Linear(1, 1); });
                })).MotionTime(inertia);*/
            
            AssetDatabase.SaveAssets();
            gameObject.GetComponent<Animator>().runtimeAnimatorController = controller.AnimatorController;
        }
        
        [MenuItem("Wholesome/AAC Delta Test VRCF")]
        private static void CreateDeltaVRCF()
        {
            var gameObject = PrefabUtility.LoadPrefabContents("Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab");
            //Create(gameObject, true);
            AnimatorController assetContainer = new AnimatorController();
            AssetDatabase.CreateAsset(assetContainer, "Packages/wholesomevr.sps-configurator/Assets/SFX/AAC_SFX.controller");
            var controller = CreateDeltaController(gameObject, assetContainer);
            AssetDatabase.SaveAssets();
            SaveControllerVRCF(controller.AnimatorController, gameObject);
            PrefabUtility.SaveAsPrefabAsset(gameObject, "Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab");
            PrefabUtility.UnloadPrefabContents(gameObject);
            /*var debugLayer = controller.NewLayer("Debug");
            debugLayer.NewState("Debug")
                .WithAnimation(aac.NewBlendTree().Simple1D(proximityVelocity)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, -1)), -10)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, 1)), 10)
                );*/
            /*.WithAnimation(aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.Animates(gameObject.transform.Find("Sender/Container"), "m_LocalScale.z")
                        .WithSecondsUnit(keyframes => { keyframes.Linear(0, 0).Linear(1, 1); });
                })).MotionTime(inertia);*/
        }

        private static AacFlController CreateDeltaController(GameObject gameObject, Object assetContainer)
        {
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = "Wholesome",
                AnimatorRoot = gameObject.transform,
                DefaultValueRoot = gameObject.transform,
                AssetContainer = assetContainer,
                AssetKey = "WH",
                DefaultsProvider = new AacDefaultsProvider(writeDefaults: true)
            });
            var controller = aac.NewAnimatorController("SFX");
            var cb = new ControllerBuilder(aac, controller);

            var velocityLayer = controller.NewLayer("Velocity");
            var smoothedProximity = velocityLayer.FloatParameter("SmoothedProximity");
            var proximity = velocityLayer.FloatParameter("WH_SFX_Depth");
            var on = velocityLayer.BoolParameter("WH_SFX_On");
            var lastProximity = velocityLayer.FloatParameter("LastProximity");
            var proximityDelta = velocityLayer.FloatParameter("ProximityDelta");
            var smoothedProximityDelta = velocityLayer.FloatParameter("SmoothedProximityDelta");
            var proximityVelocity = velocityLayer.FloatParameter("ProximityVelocity");
            var traveled = velocityLayer.FloatParameter("Traveled");
            var velocityState = velocityLayer.NewState("Velocity")
                .WithAnimation(cb.DBT()
                    //.SmoothSliding(smoothedProximity, proximity, velocityLayer, 10)
                    //.SmoothExp(smoothedProximity, proximity, velocityLayer, 12)
                    .Subtract(proximityDelta, proximity, lastProximity)
                    .SmoothSliding(smoothedProximityDelta, proximityDelta, velocityLayer, 5)
                    .Divide(proximityVelocity, smoothedProximityDelta, cb.FrameTime)
                    .Add1D(traveled, traveled, proximityDelta)
                    .Set(lastProximity, proximity)
                    .BlendTree);

            var resetLayer = controller.NewLayer("Resetter");
            var resetIdleStart = resetLayer.NewState("Idle Start");
                /*.WithAnimation(cb.DBT()
                    .Subtract(startPlusOffsetDiff, startPlusOffset, proximity)
                    .BlendTree);*/
            var resetEnd = resetLayer.NewState("Reset End")
                .WithAnimation(cb.DBT()
                    .Set0(traveled)
                    .BlendTree);
            var resetIdleEnd = resetLayer.NewState("Idle End");
                /*.WithAnimation(cb.DBT()
                    .Subtract(endMinusOffsetDiff, proximity, endMinusOffset)
                    .BlendTree);*/
            var resetStart = resetLayer.NewState("Reset Start")
                .WithAnimation(cb.DBT()
                    .Set0(traveled)
                    .BlendTree);
            
            resetIdleStart.TransitionsTo(resetEnd).When(proximityDelta.IsLessThan(-0.005f));
            resetEnd.TransitionsTo(resetIdleEnd).When(cb.Constant(1).IsGreaterThan(0));
            resetIdleEnd.TransitionsTo(resetStart).When(proximityDelta.IsGreaterThan(0.005f));
            resetStart.Exits().When(cb.Constant(1).IsGreaterThan(0));
            
            var sfxInLayer = controller.NewLayer("SFX In");
            var randomIn = sfxInLayer.IntParameter("Random In");
            var inIdle = sfxInLayer.NewState("Idle");
            var inTransform = gameObject.transform.Find("Sound/In");
            var ins = CreateAudioStates(aac, sfxInLayer, inTransform);
            inIdle.DrivingRandomizesUnsynced(randomIn, 0, ins.Count - 1);
            var inOff = sfxInLayer.NewState("Off");
            inIdle.TransitionsTo(inOff).When(on.IsFalse());
            inOff.TransitionsTo(inIdle).When(on.IsTrue());
            for (var i = 0; i < ins.Count; i++)
            {
                inIdle.TransitionsTo(ins[i])
                    .When(randomIn.IsEqualTo(i))
                    .And(traveled.IsGreaterThan(0.03f));
                ins[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(traveled.IsLessThan(-0.03f));

            }

            var sfxOutLayer = controller.NewLayer("SFX Out");
            var randomOut = sfxOutLayer.IntParameter("Random Out");
            var outIdle = sfxOutLayer.NewState("Idle");
            var outTransform = gameObject.transform.Find("Sound/Out");
            var outs = CreateAudioStates(aac, sfxOutLayer, outTransform);
            outIdle.DrivingRandomizesUnsynced(randomOut, 0, outs.Count - 1);
            var outOff = sfxOutLayer.NewState("Off");
            outIdle.TransitionsTo(outOff).When(on.IsFalse());
            outOff.TransitionsTo(outIdle).When(on.IsTrue());
            for (var i = 0; i < outs.Count; i++)
            {
                outIdle.TransitionsTo(outs[i])
                    .When(randomOut.IsEqualTo(i))
                    .And(traveled.IsLessThan(-0.03f));
                /*outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(proximity.IsLessThan(0.001f));*/
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(traveled.IsGreaterThan(0.03f));
            }

            var sfxClapLayer = controller.NewLayer("SFX Clap");
            var randomClap = sfxClapLayer.IntParameter("Random Clap");
            var clapIdle = sfxClapLayer.NewState("Idle");
            var clapWait = sfxClapLayer.NewState("Wait");
            var clapTransform = gameObject.transform.Find("Sound/Clap");
            var claps = CreateAudioStates(aac, sfxClapLayer, clapTransform);
            clapIdle.DrivingRandomizesUnsynced(randomClap, 0, claps.Count - 1);
            var clapOff = sfxClapLayer.NewState("Off");
            clapIdle.TransitionsTo(clapOff).When(on.IsFalse());
            clapOff.TransitionsTo(clapIdle).When(on.IsTrue());
            clapIdle.TransitionsTo(clapWait)
                .When(proximityVelocity.IsGreaterThan(2.5f));
            for (var i = 0; i < claps.Count; i++)
            {
                clapWait.TransitionsTo(claps[i])
                    .When(randomClap.IsEqualTo(i))
                    .And(proximityVelocity.IsLessThan(2f));
                claps[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(traveled.IsLessThan(-0.03f));
            }
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void SaveControllerVRCF(AnimatorController controller, GameObject gameObject)
        {
            gameObject.GetComponent<Animator>().runtimeAnimatorController = controller;
            var ctr = (gameObject.GetComponent<VRCFury>().config.features.Find(feature => feature is FullController) as
                FullController).controllers[0].controller;
            ctr = controller;
            ctr.id = VrcfObjectId.ObjectToId(controller);
            ctr.objRef = controller;
        }
        
        private static List<AacFlState> CreateAudioStates(AacFlBase aac, AacFlLayer layer, Transform parentTransform)
        {
            var states = new List<AacFlState>();
            for (var i = 0; i < parentTransform.childCount; i++)
            {
                var audioSource = parentTransform.GetChild(i).GetComponent<AudioSource>();
                if (audioSource.gameObject.activeSelf)
                {
                    var state = layer.NewState($"Sound {i}")
                        .WithAnimation(aac.NewClip()
                            .Animating(editClip =>
                            {
                                editClip.Animates(audioSource, "m_Enabled")
                                    .WithFixedSeconds(audioSource.clip.length, 1);
                            }));
                    states.Add(state);
                }
            }
            return states;
        }

        public static void Create(GameObject gameObject, bool vrcf = false)
        {
            AnimatorController assetContainer = new AnimatorController();
            var path = vrcf
                ? "Packages/wholesomevr.sps-configurator/Assets/SFX/AAC_SFX.controller"
                : "Assets/AAC_SFX.controller";
            AssetDatabase.CreateAsset(assetContainer, path);

            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = "Wholesome",
                AnimatorRoot = gameObject.transform,
                DefaultValueRoot = gameObject.transform,
                AssetContainer = assetContainer,
                AssetKey = "WH",
                DefaultsProvider = new AacDefaultsProvider(writeDefaults: true)
            });
            var sfx = aac.NewAnimatorController("SFX");

            var timeLayer = sfx.NewLayer("Time");
            var timeParameter = timeLayer.FloatParameter("Time");
            var timeAnimation = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(timeParameter)
                        .WithSecondsUnit(keyframes =>
                        {
                            keyframes
                                .Linear(0, 0)
                                .Linear(20_000, 20_000);
                        });
                });
            timeLayer.NewState("Time")
                .WithAnimation(timeAnimation);
            var inertiaLayer = sfx.NewLayer("Inertia");
            var one = inertiaLayer.FloatParameter("One");
            inertiaLayer.OverrideValue(one, 1);
            var lastTimeParameter = inertiaLayer.FloatParameter("LastTime");
            var lastTimeAnimation = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(lastTimeParameter)
                        .WithOneFrame(1);
                });
            var frameTimeParameter = inertiaLayer.FloatParameter("FrameTime");
            var frameTime1000Param = inertiaLayer.FloatParameter("FrameTime1000");
            var frameTime1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(frameTimeParameter)
                        .WithOneFrame(1);
                });
            var frameTimeMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(frameTimeParameter)
                        .WithOneFrame(-1);
                });
            var sfxOn = inertiaLayer.BoolParameter("WH_SFX_On");
            var proximityParam = inertiaLayer.FloatParameter("WH_SFX_Depth");
            var lastProximityParam = inertiaLayer.FloatParameter("LastProximity");
            var proximityDelta = inertiaLayer.FloatParameter("ProximityDelta");
            var proximityDeltaAbs = inertiaLayer.FloatParameter("ProximityDeltaAbs");
            var proximitySpeedAbs = inertiaLayer.FloatParameter("ProximitySpeedAbs");
            var inertia = inertiaLayer.FloatParameter("Inertia");
            var inertiaPositive = inertiaLayer.FloatParameter("InertiaPositive");
            var inertiaNegative = inertiaLayer.FloatParameter("InertiaNegative");
            /*var acceleration = inertiaLayer.FloatParameter("Acceleration");
            var accelerationPositive = inertiaLayer.FloatParameter("AccelerationPositive");
            var accelerationNegative = inertiaLayer.FloatParameter("AccelerationNegative");*/
            var distance = inertiaLayer.FloatParameter("Distance");
            var start = inertiaLayer.FloatParameter("Start");
            var end = inertiaLayer.FloatParameter("End");
            var startDiff = inertiaLayer.FloatParameter("StartDiff");
            var endDiff = inertiaLayer.FloatParameter("EndDiff");
            var startPlusOffset = inertiaLayer.FloatParameter("Start+Offset");
            var endMinusOffset = inertiaLayer.FloatParameter("End-Offset");
            var offset = inertiaLayer.FloatParameter("Offset");
            inertiaLayer.OverrideValue(offset, 0.01f);

            var maxInertia = inertiaLayer.FloatParameter("Max Inertia");
            var maxInertiaDelta = inertiaLayer.FloatParameter("Max Inertia Delta");
            
            var a = inertiaLayer.FloatParameter("A");
            inertiaLayer.OverrideValue(a, 20);
            var b = inertiaLayer.FloatParameter("B");
            inertiaLayer.OverrideValue(b, 0.015f);
            var r = inertiaLayer.FloatParameter("R");
            var oneE6 = inertiaLayer.FloatParameter("1e+6");
            inertiaLayer.OverrideValue(oneE6, 1e+6f);
            var oneEMinus3 = inertiaLayer.FloatParameter("1e-3");
            inertiaLayer.OverrideValue(oneEMinus3, 1e-3f);
            var r1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(r)
                        .WithOneFrame(1);
                });
            var rMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(r)
                        .WithOneFrame(-1);
                });
            var lastProximity1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(lastProximityParam)
                        .WithOneFrame(1);
                });
            var proximityDelta1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(proximityDelta)
                        .WithOneFrame(1);
                });
            var proximityDeltaMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(proximityDelta)
                        .WithOneFrame(-1);
                });
            var inertia1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(inertia)
                        .WithOneFrame(1);
                });
            var inertiaMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(inertia)
                        .WithOneFrame(-1);
                });
            var proximityDeltaAbs1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(proximityDeltaAbs)
                        .WithOneFrame(1);
                });
            var proximityDeltaAbs0 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(proximityDeltaAbs)
                        .WithOneFrame(0);
                });
            var inertiaPositive1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(inertiaPositive)
                        .WithOneFrame(1);
                });
            var inertiaNegative1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(inertiaNegative)
                        .WithOneFrame(1);
                });
            var proximitySpeedAbs1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(proximitySpeedAbs)
                        .WithOneFrame(1);
                });
            var frameTime1000 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(frameTime1000Param)
                        .WithOneFrame(1000);
                });
            
            var maxInertia1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(maxInertia)
                        .WithOneFrame(1);
                });
            var maxInertia0 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(maxInertia)
                        .WithOneFrame(0);
                });
            var maxInertiaDelta1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(maxInertiaDelta)
                        .WithOneFrame(1);
                });
            var maxInertiaDeltaMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(maxInertiaDelta)
                        .WithOneFrame(-1);
                });
            var start1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(start)
                        .WithOneFrame(1);
                });
            var start0 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(start)
                        .WithOneFrame(0);
                });
            var end1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(end)
                        .WithOneFrame(1);
                });
            var end0 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(end)
                        .WithOneFrame(0);
                });
            var startDiff1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(startDiff)
                        .WithOneFrame(1);
                });
            var startDiffMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(startDiff)
                        .WithOneFrame(-1);
                });
            var endDiff1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(endDiff)
                        .WithOneFrame(1);
                });
            var endDiffMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(endDiff)
                        .WithOneFrame(-1);
                });
            var startPlusOffset1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(startPlusOffset)
                        .WithOneFrame(1);
                });
            var endMinusOffset1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(endMinusOffset)
                        .WithOneFrame(1);
                });
            var endMinusOffsetMinus1 = aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.AnimatesAnimator(endMinusOffset)
                        .WithOneFrame(-1);
                });
            var divisionTree = aac.NewBlendTree().Direct()
                .WithAnimation(aac.DummyClipLasting(1f, AacFlUnit.Frames), frameTime1000Param)
                .WithAnimation(proximitySpeedAbs1, oneEMinus3);
            using (var so = new SerializedObject(divisionTree.BlendTree))
            {
                so.FindProperty("m_NormalizedBlendValues").boolValue = true;
                so.ApplyModifiedProperties();
            }
            inertiaLayer.NewState("Blend Tree")
                .WithAnimation(aac.NewBlendTree()
                    .Direct()
                    .WithAnimation(frameTime1, timeParameter)
                    .WithAnimation(frameTimeMinus1, lastTimeParameter)
                    .WithAnimation(frameTime1000, frameTimeParameter)
                    .WithAnimation(proximityDelta1, proximityParam)
                    .WithAnimation(proximityDeltaMinus1, lastProximityParam)
                    .WithAnimation(
                        aac.NewBlendTree().Simple1D(proximityDelta)
                            .WithAnimation(proximityDeltaAbs1, -1)
                            .WithAnimation(proximityDeltaAbs0, 0)
                            .WithAnimation(proximityDeltaAbs1, 1),
                        one)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(divisionTree, oneE6), proximityDeltaAbs)
                    .WithAnimation(r1, one)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(rMinus1, frameTimeParameter), a)
                    .WithAnimation(lastProximity1, proximityParam)
                    .WithAnimation(lastTimeAnimation, timeParameter)
                    .WithAnimation(maxInertia1, maxInertia)
                    .WithAnimation(start1, start)
                    .WithAnimation(end1, end)
                    .WithAnimation(startPlusOffset1, startPlusOffset)
                    .WithAnimation(endMinusOffset1, endMinusOffset)
                );
            
            var inertia2Layer = sfx.NewLayer("Inertia2");
            var negative = inertia2Layer.NewState("Negative")
                .WithAnimation(aac.NewBlendTree()
                    .Direct()
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaPositive1, inertiaPositive), r)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaNegative1, inertiaNegative), r)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaNegative1, proximitySpeedAbs), b)
                    .WithAnimation(inertia1, inertiaPositive)
                    .WithAnimation(inertiaMinus1, inertiaNegative)
                );
            var positive = inertia2Layer.NewState("Positive")
                .WithAnimation(aac.NewBlendTree()
                    .Direct()
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaPositive1, inertiaPositive), r)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaNegative1, inertiaNegative), r)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaPositive1, proximitySpeedAbs), b)
                    .WithAnimation(inertia1, inertiaPositive)
                    .WithAnimation(inertiaMinus1, inertiaNegative)
                );
            negative.TransitionsTo(positive).When(proximityDelta.IsGreaterThan(0));
            positive.TransitionsTo(negative).When(proximityDelta.IsLessThan(-0.0001f));
            
            var maxInertiaLayer = sfx.NewLayer("Max Inertia");
            var maxInertiaIdle = maxInertiaLayer.NewState("Idle")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(maxInertiaDelta1, maxInertia)
                    .WithAnimation(maxInertiaDeltaMinus1, inertia)
                );
            var maxInertiaSet = maxInertiaLayer.NewState("Max Inertia")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(maxInertia1, inertia));
            var maxInertiaReset = maxInertiaLayer.NewState("Reset Max Inertia")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(maxInertia0, one));
            maxInertiaIdle.TransitionsTo(maxInertiaSet).When(maxInertiaDelta.IsLessThan(0f));
            maxInertiaIdle.TransitionsTo(maxInertiaReset).When(inertia.IsLessThan(-0.001f));
            maxInertiaSet.Exits().When(one.IsGreaterThan(0));
            maxInertiaReset.Exits().When(one.IsGreaterThan(0));

            
            var startProximityLayer = sfx.NewLayer("Start Proximity");
            var startProximityIdle = startProximityLayer.NewState("Idle")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(startDiff1, proximityParam)
                    .WithAnimation(startDiffMinus1, start)
                );
            var startProximitySet = startProximityLayer.NewState("Set Start Proximity")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(start1, proximityParam)
                    .WithAnimation(startPlusOffset1, start)
                    .WithAnimation(startPlusOffset1, offset)
                );
            var startProximityReset = startProximityLayer.NewState("Reset Start Proximity")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(start1, one)
                );
            startProximityIdle.TransitionsTo(startProximitySet)
                .When(startDiff.IsLessThan(0));
            startProximityIdle.TransitionsTo(startProximityReset)
                .When(endDiff.IsLessThan(0));
            startProximitySet.Exits().When(one.IsGreaterThan(0));
            startProximityReset.Exits().When(one.IsGreaterThan(0));
            
            var endProximityLayer = sfx.NewLayer("End Proximity");
            var endProximityIdle = endProximityLayer.NewState("Idle")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(endDiff1, end)
                    .WithAnimation(endDiffMinus1, proximityParam)
                );
            var endProximitySet = endProximityLayer.NewState("Set End Proximity")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(end1, proximityParam)
                    .WithAnimation(endMinusOffset1, end)
                    .WithAnimation(endMinusOffsetMinus1, offset)
                );
            var endProximityReset = startProximityLayer.NewState("Reset End Proximity")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(end0, one)
                );
            endProximityIdle.TransitionsTo(endProximitySet)
                .When(endDiff.IsLessThan(0));
            startProximitySet.Exits().When(one.IsGreaterThan(0));
            endProximitySet.Exits().When(one.IsGreaterThan(0));

            var sfxInLayer = sfx.NewLayer("SFX In");
            var randomIn = sfxInLayer.IntParameter("Random In");
            var inIdle = sfxInLayer.NewState("Idle");
            var inTransform = gameObject.transform.Find("Sound/In");
            var ins = CreateAudioStates(aac, sfxInLayer, inTransform);
            inIdle.DrivingRandomizesUnsynced(randomIn, 0, ins.Count - 1);
            for (var i = 0; i < ins.Count; i++)
            {
                inIdle.TransitionsTo(ins[i])
                    .When(randomIn.IsEqualTo(i))
                    .And(inertia.IsGreaterThan(0.03f/*0.07f*/));
                ins[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(inertia.IsLessThan(-0.03f));

            }

            var sfxOutLayer = sfx.NewLayer("SFX Out");
            var randomOut = sfxOutLayer.IntParameter("Random Out");
            var outIdle = sfxOutLayer.NewState("Idle");
            var outTransform = gameObject.transform.Find("Sound/Out");
            var outs = CreateAudioStates(aac, sfxOutLayer, outTransform);
            outIdle.DrivingRandomizesUnsynced(randomOut, 0, outs.Count - 1);
            for (var i = 0; i < outs.Count; i++)
            {
                outIdle.TransitionsTo(outs[i])
                    .When(randomOut.IsEqualTo(i))
                    .And(inertia.IsLessThan(-0.03f/*0.07f*/));
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(inertia.IsGreaterThan(0.03f));
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(proximityParam.IsLessThan(0.001f));
            }

            var sfxClapLayer = sfx.NewLayer("SFX Clap");
            var randomClap = sfxClapLayer.IntParameter("Random Clap");
            var clapIdle = sfxClapLayer.NewState("Idle");
            var clapIn = sfxClapLayer.NewState("In");
            var clapTransform = gameObject.transform.Find("Sound/Clap");
            var claps = CreateAudioStates(aac, sfxClapLayer, clapTransform);
            clapIdle.DrivingRandomizesUnsynced(randomClap, 0, claps.Count - 1);
            for (var i = 0; i < claps.Count; i++)
            {
                clapIdle.TransitionsTo(clapIn)
                    .When(inertia.IsGreaterThan(0.03f));
                clapIn.TransitionsTo(claps[i])
                    .When(randomClap.IsEqualTo(i))
                    .And(inertia.IsLessThan(0.02f))
                    .And(maxInertia.IsGreaterThan(0.06f));
                clapIn.Exits()
                    .When(inertia.IsLessThan(0.02f));
                claps[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1)
                    .When(inertia.IsLessThan(-0.03f));
            }
            
            /*for (var i = 0; i < ins.Count; i++)
            {
                inIdle.TransitionsTo(ins[i])
                    .When(randomIn.IsEqualTo(i))
                    .And(inertia.IsGreaterThan(0.03f));
                var next = claps.Count > 0 ? clapIdle : outIdle;
                ins[i].TransitionsTo(next)
                    .AfterAnimationIsAtLeastAtPercent(1);
            }

            for (var i = 0; i < claps.Count; i++)
            {
                clapIdle.TransitionsTo(claps[i])
                    .When(randomClap.IsEqualTo(i))
                    .And(inertia.IsLessThan(0.02f))
                    .And(maxInertia.IsGreaterThan(0.06f));
                clapIdle.TransitionsTo(outIdle)
                    .When(randomClap.IsEqualTo(i))
                    .And(inertia.IsLessThan(0.02f));
                claps[i].TransitionsTo(outIdle)
                    .AfterAnimationIsAtLeastAtPercent(1);
            }
            for (var i = 0; i < outs.Count; i++)
            {
                outIdle.TransitionsTo(outs[i])
                    .When(randomOut.IsEqualTo(i))
                    .And(inertia.IsLessThan(-0.03f));
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1);
            }*/

            /*var off = sfxLayer.NewState("Off");
            off.TransitionsTo(inIdle)
                .When(sfxOn.IsEqualTo(true));
            sfxLayer.AnyTransitionsTo(off)
                .When(sfxOn.IsEqualTo(false));*/
            
            
            /*
            var debugLayer = sfx.NewLayer("Debug");
            debugLayer.NewState("Debug")
                .WithAnimation(aac.NewBlendTree().Simple1D(inertia)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, -1)), -1)
                    .WithAnimation(aac.NewClip()
                        .Scaling(new[] { gameObject.transform.Find("Sender/Container").gameObject },
                            new Vector3(0.1f, 0.1f, 1)), 1)
                );*/
            /*.WithAnimation(aac.NewClip()
                .Animating(editClip =>
                {
                    editClip.Animates(gameObject.transform.Find("Sender/Container"), "m_LocalScale.z")
                        .WithSecondsUnit(keyframes => { keyframes.Linear(0, 0).Linear(1, 1); });
                })).MotionTime(inertia);*/
            AssetDatabase.SaveAssets();
            gameObject.GetComponent<Animator>().runtimeAnimatorController = sfx.AnimatorController;
            if (vrcf)
            {
                var controller = (gameObject.GetComponent<VRCFury>().config.features.Find(feature => feature is FullController) as
                    FullController).controllers[0];
                controller.controller = sfx.AnimatorController;
                controller.controller.id = VrcfObjectId.ObjectToId(sfx.AnimatorController);
                controller.controller.objRef = sfx.AnimatorController;
            }
        }
        
        [MenuItem("Wholesome/AAC")]
        private static void CreateAC()
        {
            var gameObject = Selection.activeGameObject;
            Create(gameObject);
        }
        
        [MenuItem("Wholesome/AAC VRCF")]
        private static void CreateVRCFAC()
        {
            var gameObject = PrefabUtility.LoadPrefabContents("Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab");
            Create(gameObject, true);
            PrefabUtility.SaveAsPrefabAsset(gameObject, "Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab");
            PrefabUtility.UnloadPrefabContents(gameObject);
        }
    }
}