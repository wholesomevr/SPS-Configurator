using System;
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
        private GameObject avatarObject;
        private SPSConfigurator.AvatarArmature avatarArmature;

        public SFX(GameObject avatarObject, SPSConfigurator.AvatarArmature avatarArmature)
        {
            this.avatarObject = avatarObject;
            this.avatarArmature = avatarArmature;
        }

        private Version CurrentVersion
        {
            get
            {
                var packageInfo =
                    PackageInfo.FindForAssetPath("Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab");
                return new Version(packageInfo.version);
            }
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

                var vrcf = dstPrefab.GetComponent<VRCFury>();
                var fullCtr = (vrcf.config.features.Find(ft => ft is FullController) as FullController).controllers[0];
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

        public void Apply(Transform socketTransform)
        {
            var (sfxPrefab, sfxBJPrefab) = CopyAssets();
            var socket = socketTransform.GetComponent<VRCFuryHapticSocket>();
            var existingSFX = socketTransform.Find("SFX");
            Transform sfx;
            if (existingSFX != null)
            {
                var version = GetSFXVersion(socket);
                if (version != null && version >= CurrentVersion)
                {
                    sfx = existingSFX;
                }
                else
                {
                    sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxPrefab, socketTransform)).transform;
                    Object.DestroyImmediate(existingSFX.gameObject);
                }
            }
            else
            {
                sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxPrefab, socketTransform)).transform;
            }

            socket.enableDepthAnimations = true;
            if (!socket.depthActions.Any(action =>
                    action.state.actions.Any(action2 =>
                        action2 is FxFloatAction fx && fx.name == "WH_SFX_Depth")))
            {
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
            }

            socket.enableActiveAnimation = true;
            if (socket.activeActions == null || socket.activeActions.actions.OfType<ObjectToggleAction>().All(o => o.obj != sfx.gameObject) )
            {
                if (socket.activeActions?.actions == null)
                {
                    socket.activeActions = new State();
                }

                socket.activeActions.actions.Add(new ObjectToggleAction
                {
                    obj = sfx.gameObject
                });
                socket.activeActions.actions.RemoveAll(action =>
                {
                    if (action is ObjectToggleAction o)
                    {
                        return o.obj == null;
                    }

                    return false;
                });
            }
        }

        private Version GetSFXVersion(VRCFuryHapticSocket socket)
        {
            if (socket == null) return null;
            var sfx = socket.transform.Find("SFX");
            if (sfx == null) return null;
            var vrcFury = sfx.GetComponent<VRCFury>();
            if (vrcFury == null) return null;
            var ctr = vrcFury.config.features.OfType<FullController>().FirstOrDefault();
            if (ctr == null) return null;
            if (ctr.controllers.Count == 0) return null;
            var animCtr = ctr.controllers[0].controller.Get();
            if (animCtr == null) return null;
            var ctrPath = AssetDatabase.GetAssetPath(animCtr);
            if (!ctrPath.StartsWith("Assets/!Wholesome/SPS Configurator")) return null;
            var pathSplit = ctrPath.Split('/');
            if (pathSplit.Length <= 3) return null;
            var version = pathSplit[3];
            try
            {
                return new Version(version);
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't parse SFX version: {e.Message}");
                return null;
            }
        }

        public void Clean()
        {
        }

        private bool HasSFX(Transform hips)
        {
            if (hips == null) return false;
            var pussySFX = hips.Find("SPS/Pussy/SFX");
            if (pussySFX != null) return true;
            var analSFX = hips.Find("SPS/Anal/SFX");
            if (pussySFX != null) return true;
            return false;
        }
    }
}