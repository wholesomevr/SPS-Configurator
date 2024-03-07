using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor.PackageManager;
using VF;
using VF.Component;
using VF.Model;
using VF.Model.Feature;
using VF.Model.StateAction;
using VF.Utils;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Wholesome
{
    public class SFX
    {
        public void Apply(Transform socketTransform)
        {
            var (sfxPrefab, sfxBJPrefab) = CopyAssets();
            var socket = socketTransform.GetComponent<VRCFuryHapticSocket>();
            var sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxPrefab, socketTransform)).transform;
            socket.enableDepthAnimations = true;
            var fxState = new State();
            fxState.actions.Add(new FxFloatAction()
            {
                name = "WH_SFX_Depth"
            });
            socket.depthActions.Add(new VRCFuryHapticSocket.DepthAction
            {
                state = fxState,
                enableSelf = true,
                startDistance = 0,
                endDistance = -0.5f,
                smoothingSeconds = 0,
            });

            socket.enableActiveAnimation = true;
            socket.activeActions = new State();
            socket.activeActions.actions.Add(new ObjectToggleAction
            {
                obj = sfx.gameObject
            });
        }

        public void AddToggle(GameObject spsObject)
        {
            var fury = spsObject.AddComponent<VRCFury>();
            fury.Version = 2;
            fury.config.features.Add(new Toggle()
            {
                name = "SPS/Options/Sound FX",
                saved = true,
                defaultOn = true,
                useGlobalParam = true,
                globalParam = "WH_SFX_On"
            });
        }
        
        private (Object, Object) CopyAssets()
        {
            var packageInfo =
                PackageInfo.FindForAssetPath("Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab");
            var dest = $"Assets/!Wholesome/SPS Configurator/{packageInfo.version}/SFX";
            var sfxSrcSuffix = "Packages/wholesomevr.sps-configurator/Assets/SFX/";

            string SrcToDst(string path)
            {
                var file = path.Substring(sfxSrcSuffix.Length);
                var dst = $"{dest}/{file}";
                return dst;
            }

            void ReplaceAssets(string file)
            {
                var dstPrefab = AssetDatabase.LoadMainAssetAtPath($"{dest}/{file}") as GameObject;
                var audios = dstPrefab.GetComponentsInChildren<AudioSource>(true);
                foreach (var audioSource in audios)
                {
                    var clipSrcPath = AssetDatabase.GetAssetPath(audioSource.clip);
                    if (clipSrcPath.StartsWith(sfxSrcSuffix))
                    {
                        audioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SrcToDst(clipSrcPath));
                    }
                }

                var content = typeof(VRCFury).GetField("content");
                IEnumerable<FeatureModel> features;
                if (content != null)
                {
                    features = dstPrefab.GetComponents<VRCFury>()
                        .Select(vrcf => content.GetValue(vrcf) as FeatureModel);
                }
                else
                {
                    features = dstPrefab.GetComponent<VRCFury>().config.features;
                }

                var vrcfFullCtr = features.OfType<FullController>().FirstOrDefault();
                if (vrcfFullCtr == null)
                    throw new Exception("SFX assets are corrupted. " +
                                        "Delete !Wholesome/SPS Configurator directory and re-add sockets.");
                var fullCtr = vrcfFullCtr.controllers[0];
                var ctrSrc = fullCtr.controller.Get();
                var ctrSrcPath = AssetDatabase.GetAssetPath(ctrSrc);
                if (ctrSrcPath.StartsWith(sfxSrcSuffix))
                {
                    var ctrDstPath = SrcToDst(ctrSrcPath);
                    var ctrDst = AssetDatabase.LoadAllAssetsAtPath(ctrDstPath).ToList()
                        .Find(asset =>
                            asset is AnimatorController ctr && ctr.name == ctrSrc.name) as AnimatorController;
                    fullCtr.controller = ctrDst;
                    fullCtr.controller.id = VrcfObjectId.ObjectToId(ctrDst);
                    fullCtr.controller.objRef = ctrDst;
                    dstPrefab.GetComponent<Animator>().runtimeAnimatorController = ctrDst;
                }

                PrefabUtility.SavePrefabAsset(dstPrefab);
            }

            var sfxPath = "Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab";
            var sfxBJPath = "Packages/wholesomevr.sps-configurator/Assets/SFX/SFX BJ.prefab";
            var sfxDependencies = AssetDatabase.GetDependencies(sfxPath)
                .Where(path => path.StartsWith("Packages/wholesomevr.sps-configurator/Assets/SFX/")).ToList();
            var sfxBJDependencies = AssetDatabase.GetDependencies(sfxBJPath)
                .Where(path => path.StartsWith("Packages/wholesomevr.sps-configurator/Assets/SFX/")).ToList();
            foreach (var srcDependency in sfxDependencies)
            {
                var dstDependency = SrcToDst(srcDependency);
                if (!File.Exists(dstDependency))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dstDependency));
                    AssetDatabase.CopyAsset(srcDependency, dstDependency);
                }
            }

            ReplaceAssets("SFX.prefab");
            foreach (var srcDependency in sfxBJDependencies)
            {
                var dstDependency = SrcToDst(srcDependency);
                if (!File.Exists(dstDependency))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dstDependency));
                    AssetDatabase.CopyAsset(srcDependency, dstDependency);
                }
            }

            ReplaceAssets("SFX BJ.prefab");

            return (AssetDatabase.LoadAssetAtPath<Object>($"{dest}/SFX.prefab"),
                AssetDatabase.LoadAssetAtPath<Object>($"{dest}/SFX BJ.prefab"));
        }
    }
}