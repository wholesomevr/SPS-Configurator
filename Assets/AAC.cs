using System.Collections.Generic;
using UnityEngine;
using AnimatorAsCode.V1.VRC;
using AnimatorAsCode.V1;
using UnityEditor;
using UnityEditor.Animations;
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

    public static class AACTools
    {
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
            //var keepLayer = sfx.NewLayer("Keep");
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
            /*keepLayer.NewState("Blend Tree")
                .WithAnimation(aac.NewBlendTree().Direct()
                    .WithAnimation(inertiaPositive1, inertiaPositive)
                    .WithAnimation(inertiaNegative1, inertiaNegative));*/
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
                    //.WithAnimation(maxInertia1, maxInertia)
                );
            
            var inertia2Layer = sfx.NewLayer("Inertia2");
            var negative = inertia2Layer.NewState("Negative")
                .WithAnimation(aac.NewBlendTree()
                    .Direct()
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaPositive1, inertiaPositive), r)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaNegative1, inertiaNegative), r)
                    /*.WithAnimation(
                        aac.NewBlendTree().Direct()
                            .WithAnimation(
                                aac.NewBlendTree().Direct().WithAnimation(inertiaNegative1, frameTimeParameter), proximityDeltaAbs), b)*/
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaNegative1, proximitySpeedAbs), b)
                    .WithAnimation(inertia1, inertiaPositive)
                    .WithAnimation(inertiaMinus1, inertiaNegative)
                );
            var positive = inertia2Layer.NewState("Positive")
                .WithAnimation(aac.NewBlendTree()
                    .Direct()
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaPositive1, inertiaPositive), r)
                    .WithAnimation(aac.NewBlendTree().Direct().WithAnimation(inertiaNegative1, inertiaNegative), r)
                    /*.WithAnimation(
                        aac.NewBlendTree().Direct()
                            .WithAnimation(
                                aac.NewBlendTree().Direct().WithAnimation(inertiaPositive1, frameTimeParameter),
                                proximityDeltaAbs), b)*/
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
                .DrivingCopies(inertia, maxInertia);
            maxInertiaIdle.TransitionsTo(maxInertiaSet).When(maxInertiaDelta.IsLessThan(0f));
            maxInertiaSet.Exits().AfterAnimationIsAtLeastAtPercent(0.01f);
            
            /*
            var sfxLayer = sfx.NewLayer("SFX");
            var idle = sfxLayer.NewState("Idle");
            var off = sfxLayer.NewState("Off");
            off.TransitionsTo(idle).When(sfxOn.IsGreaterThan(0.1f));
            idle.TransitionsTo(off).When(sfxOn.IsLessThan(0.9f));
            var fastIn = sfxLayer.NewState("Fast In")
                .WithAnimation(aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.Animates(gameObject.transform.Find("Sound/Fast In").GetComponent<AudioSource>(), "m_Enabled")
                            .WithFixedSeconds(0.032f, 1);
                    }));
            var fastClap = sfxLayer.NewState("Fast Clap")
                .WithAnimation(aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.Animates(gameObject.transform.Find("Sound/Fast Clap").GetComponent<AudioSource>(), "m_Enabled")
                            .WithFixedSeconds(0.048f, 1);
                    }));
            var fastOut = sfxLayer.NewState("Fast Out")
                .WithAnimation(aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.Animates(gameObject.transform.Find("Sound/Fast Out").GetComponent<AudioSource>(), "m_Enabled")
                            .WithFixedSeconds(0.204f, 1);
                    }));
            var slowIn = sfxLayer.NewState("Slow In")
                .WithAnimation(aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.Animates(gameObject.transform.Find("Sound/Slow In").GetComponent<AudioSource>(), "m_Enabled")
                            .WithFixedSeconds(0.081f, 1);
                    }));
            var slowOut = sfxLayer.NewState("Slow Out")
                .WithAnimation(aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.Animates(gameObject.transform.Find("Sound/Slow Out").GetComponent<AudioSource>(), "m_Enabled")
                            .WithFixedSeconds(0.262f, 1);
                    }));
            var ultraFast = sfxLayer.NewState("Ultra Fast")
                .WithAnimation(aac.NewClip()
                    .Animating(editClip =>
                    {
                        editClip.Animates(gameObject.transform.Find("Sound/Ultra Fast").GetComponent<AudioSource>(), "m_Enabled")
                            .WithFixedSeconds(0.448f, 1);
                    }));
            idle.TransitionsTo(fastIn)
                .AfterAnimationIsAtLeastAtPercent(1f)
                .When(inertia.IsGreaterThan(0.15f));
                //.And(inertia.IsLessThan(0.23f));
                //.When(proximitySpeedAbs.IsGreaterThan(0.2f))
                //.And(proximityDelta.IsGreaterThan(0));
            fastIn.TransitionsTo(fastClap)
                .AfterAnimationIsAtLeastAtPercent(1f)
                .When(inertia.IsLessThan(0.03f));
            fastClap.TransitionsTo(fastOut)
                .AfterAnimationIsAtLeastAtPercent(1f)
                .When(inertia.IsLessThan(-0.03f));
            fastOut.Exits()
                .AfterAnimationIsAtLeastAtPercent(1f);
            /*idle.TransitionsTo(slowIn)
                .AfterAnimationIsAtLeastAtPercent(1f)
                .When(inertia.IsGreaterThan(0.08f))
                .And(inertia.IsLessThan(0.15f));
            slowIn.TransitionsTo(slowOut)
                .AfterAnimationIsAtLeastAtPercent(1f)
                .When(inertia.IsLessThan(-0.01f));
            slowOut.Exits()
                .AfterAnimationIsAtLeastAtPercent(1f);
            idle.TransitionsTo(ultraFast)
                .AfterAnimationIsAtLeastAtPercent(1f)
                .When(inertia.IsGreaterThan(0.23f));
            ultraFast.Exits()
                .AfterAnimationIsAtLeastAtPercent(1f);*/
            // save max value?*/

            /*var sfxInLayer = sfx.NewLayer("SFX In");
            var randomIn = sfxInLayer.IntParameter("Random In");
            var inIdle = sfxInLayer.NewState("Idle");
            var ins = new[]
            {
                sfxInLayer.NewState("In 0")
                    .WithAnimation(aac.NewClip()
                        .Animating(editClip =>
                            {
                                editClip.Animates(gameObject.transform.Find("Sound/Fast In").GetComponent<AudioSource>(), "m_Enabled")
                                    .WithFixedSeconds(0.032f, 1);
                            })),
                sfxInLayer.NewState("In 1")
                    .WithAnimation(aac.NewClip()
                        .Animating(editClip =>
                        {
                            editClip.Animates(gameObject.transform.Find("Sound/Slow In").GetComponent<AudioSource>(), "m_Enabled")
                                .WithFixedSeconds(0.081f, 1);
                        })),
            };
            inIdle.DrivingRandomizesUnsynced(randomIn, 0, ins.Length - 1);
            for (var i = 0; i < ins.Length; i++)
            {
                inIdle.TransitionsTo(ins[i])
                    .When(randomIn.IsEqualTo(i))
                    .And(inertia.IsGreaterThan(0.15f));
                ins[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1);
            }
            
            var sfxOutLayer = sfx.NewLayer("SFX Out");
            var randomOut = sfxOutLayer.IntParameter("Random Out");
            var outIdle = sfxOutLayer.NewState("Idle");
            var outs = new[]
            {
                sfxOutLayer.NewState("Out 0")
                    .WithAnimation(aac.NewClip()
                        .Animating(editClip =>
                        {
                            editClip.Animates(gameObject.transform.Find("Sound/Fast Out").GetComponent<AudioSource>(), "m_Enabled")
                                .WithFixedSeconds(0.048f, 1);
                        })),
                sfxOutLayer.NewState("Out 1")
                    .WithAnimation(aac.NewClip()
                        .Animating(editClip =>
                        {
                            editClip.Animates(gameObject.transform.Find("Sound/Slow Out").GetComponent<AudioSource>(), "m_Enabled")
                                .WithFixedSeconds(0.262f, 1);
                        }))
            };
            outIdle.DrivingRandomizesUnsynced(randomOut, 0, outs.Length - 1);
            for (var i = 0; i < outs.Length; i++)
            {
                outIdle.TransitionsTo(outs[i])
                    .When(randomOut.IsEqualTo(i))
                    .And(inertia.IsLessThan(-0.03f));
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1);
            }
            
            var sfxClapLayer = sfx.NewLayer("SFX Clap");
            var randomClap = sfxClapLayer.IntParameter("Random Clap");
            var clapIdle = sfxClapLayer.NewState("Idle");
            var claps = new[]
            {
                sfxClapLayer.NewState("Clap 0")
                    .WithAnimation(aac.NewClip()
                        .Animating(editClip =>
                        {
                            editClip.Animates(gameObject.transform.Find("Sound/Fast Clap").GetComponent<AudioSource>(), "m_Enabled")
                                .WithFixedSeconds(0.048f, 1);
                        }))
            };
            clapIdle.DrivingRandomizesUnsynced(randomClap, 0, claps.Length - 1);
            for (var i = 0; i < claps.Length; i++)
            {
                clapIdle.TransitionsTo(claps[i])
                    .When(randomClap.IsEqualTo(i))
                    .And(inertia.IsLessThan(-0.03f));
                claps[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1);
            }*/
            
            var sfxLayer = sfx.NewLayer("SFX");
            var randomIn = sfxLayer.IntParameter("Random In");
            var inIdle = sfxLayer.NewState("Idle In");
            var ins = new List<AacFlState>();
            var inTransform = gameObject.transform.Find("Sound/In");
            for (var i = 0; i < inTransform.childCount; i++)
            {
                var audioSource = inTransform.GetChild(i).GetComponent<AudioSource>();
                if (audioSource.gameObject.activeSelf)
                {
                    var state = sfxLayer.NewState($"In {i}")
                        .WithAnimation(aac.NewClip()
                            .Animating(editClip =>
                            {
                                editClip.Animates(audioSource, "m_Enabled")
                                    .WithFixedSeconds(audioSource.clip.length + 0.1f, 1);
                            }));
                    ins.Add(state);
                }
            }
            inIdle.DrivingRandomizesUnsynced(randomIn, 0, ins.Count - 1);
            
            
            var randomOut = sfxLayer.IntParameter("Random Out");
            var outIdle = sfxLayer.NewState("Idle Out");
            var outs = new List<AacFlState>();
            var outTransform = gameObject.transform.Find("Sound/Out");
            for (var i = 0; i < outTransform.childCount; i++)
            {
                var audioSource = outTransform.GetChild(i).GetComponent<AudioSource>();
                if (audioSource.gameObject.activeSelf)
                {
                    var state = sfxLayer.NewState($"Out {i}")
                        .WithAnimation(aac.NewClip()
                            .Animating(editClip =>
                            {
                                editClip.Animates(audioSource, "m_Enabled")
                                    .WithFixedSeconds(audioSource.clip.length  + 0.1f, 1);
                            }));
                    outs.Add(state);
                }
            }
            outIdle.DrivingRandomizesUnsynced(randomOut, 0, outs.Count - 1);
            
            
            var randomClap = sfxLayer.IntParameter("Random Clap");
            var clapIdle = sfxLayer.NewState("Idle Clap");
            var claps = new List<AacFlState>();
            var clapTransform = gameObject.transform.Find("Sound/Clap");
            for (var i = 0; i < clapTransform.childCount; i++)
            {
                var audioSource = clapTransform.GetChild(i).GetComponent<AudioSource>();
                if (audioSource.gameObject.activeSelf)
                {
                    var state = sfxLayer.NewState($"Clap {i}")
                        .WithAnimation(aac.NewClip()
                            .Animating(editClip =>
                            {
                                editClip.Animates(audioSource, "m_Enabled")
                                    .WithFixedSeconds(audioSource.clip.length + 0.1f, 1);
                            }));
                    claps.Add(state);
                }
            }
            clapIdle.DrivingRandomizesUnsynced(randomClap, 0, claps.Count - 1);
            
            for (var i = 0; i < ins.Count; i++)
            {
                inIdle.TransitionsTo(ins[i])
                    .When(randomIn.IsEqualTo(i))
                    .And(inertia.IsGreaterThan(0.07f));
                //ins[i].TransitionsTo(clapIdle)
                ins[i].TransitionsTo(outIdle)
                    .AfterAnimationIsAtLeastAtPercent(1);
            }
            
            /*for (var i = 0; i < claps.Count; i++)
            {
                clapIdle.TransitionsTo(claps[i])
                    .When(randomClap.IsEqualTo(i))
                    .And(inertia.IsLessThan(0.02f));
                claps[i].TransitionsTo(outIdle)
                    .AfterAnimationIsAtLeastAtPercent(1);
            }*/
            for (var i = 0; i < outs.Count; i++)
            {
                outIdle.TransitionsTo(outs[i])
                    .When(randomOut.IsEqualTo(i))
                    .And(inertia.IsLessThan(-0.03f));
                outs[i].Exits()
                    .AfterAnimationIsAtLeastAtPercent(1);
            }

            var off = sfxLayer.NewState("Off");
            off.TransitionsTo(inIdle)
                .When(sfxOn.IsEqualTo(true));
            sfxLayer.AnyTransitionsTo(off)
                .When(sfxOn.IsEqualTo(false));
            
            
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