using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEditor.PackageManager;
using VF;
using VF.Builder;
using VF.Component;
using VF.Inspector;
using VF.Model;
using VF.Model.Feature;
using VF.Model.StateAction;
using VF.Utils;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Action = VF.Model.StateAction.Action;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Wholesome
{
    public class SPSConfigurator : EditorWindow
    {
        #pragma warning disable 0219
        #pragma warning disable 0414
        enum FootType
        {
            Flat,
            Heeled
        }

        private static readonly string BlowjobName = "Blowjob";
        private static readonly string HandjobName = "Handjob";
        private static readonly string PussyName = "Pussy";
        private static readonly string AnalName = "Anal";
        private static readonly string TitjobName = "Titjob";
        private static readonly string AssjobName = "Assjob";
        private static readonly string ThighjobName = "Thighjob";
        private static readonly string SoleName = "Steppies";
        private static readonly string FootjobName = "Footjob";

        private int selectedBase = 0;
        private FootType selectedFootType = FootType.Flat;
        private Texture2D categoryLabelBackground;

        private Texture2D logo;

        private bool defaultOn = true;
        private bool specialOn = true;
        private bool feetOn = true;

        private bool blowjobOn = true;
        private StringBuilder blowjobBlendshape = new StringBuilder("vrc.v_oh");
        private bool handjobOn = true;
        private bool handjobLeftOn = true;
        private bool handjobRightOn = true;
        private bool handjobBothOn = true;
        private bool pussyOn = true;
        private StringBuilder pussyBlendshape = new StringBuilder();
        private bool analOn = true;
        private StringBuilder analBlendshape = new StringBuilder();

        private bool titjobOn = true;
        private bool assjobOn = true;
        private bool thighjobOn = true;

        private bool soleOn = true;
        private bool soleLeftOn = true;
        private bool soleRightOn = true;
        private bool footjobOn = true;
        private SkinnedMeshRenderer[] meshes;
        private string spsMenuPath = "SPS";
        private bool experimentalFoldout = false;
        private bool sfxOn = true;
        private bool sfxBlowjobOn = false;
        private bool sfxPussyOn = true;
        private bool sfxAnalOn = true;
        private Vector2 scrollPosition = new Vector2();

        [MenuItem("Tools/Wholesome/SPS Configurator")]
        public static void Open()
        {
            var window = GetWindow(typeof(SPSConfigurator));
            window.titleContent = new GUIContent("SPS Configurator");
            window.minSize = new Vector2(490, 806);
            window.Show();
        }

        public static Vector3 DetectMouthPosition(SkinnedMeshRenderer head, int ohBlendshapeIndex, Transform headBone)
        {
            var mesh = head.sharedMesh;
            var frames = head.sharedMesh.GetBlendShapeFrameCount(ohBlendshapeIndex);
            var deltaPositions = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            head.sharedMesh.GetBlendShapeFrameVertices(ohBlendshapeIndex, frames - 1, deltaPositions, deltaNormals,
                deltaTangents);
            var magnitudes = deltaPositions.Select(pos => pos.magnitude).ToArray();
            var sum = magnitudes.Sum();
            var weights = magnitudes.Select(mag => mag / sum).ToArray();
            var weightedPos = mesh.vertices.Zip(weights, (pos, weight) => pos * weight)
                .Aggregate(new Vector3(), (wpSum, wp) => wpSum + wp, (wp) => wp);
            Debug.Assert(Mathf.Abs(weightedPos.x) < 0.01, "Blendshape not symmetric");
            weightedPos = head.localToWorldMatrix.MultiplyPoint(weightedPos);
            var weightedPosLocal = headBone.InverseTransformPoint(weightedPos);
            var originLocal = weightedPosLocal + new Vector3(0, 0, 0.1f);
            var origin = headBone.TransformPoint(originLocal);
            //weightedPos = new Vector3(0, weightedPos.z, -weightedPos.y); // TODO: Handle all possible head transformations
            var intersect = typeof(HandleUtility).GetMethod("IntersectRayMesh",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            // TODO: relative to head rotation
            object[] rayParams =
            {
                new Ray(origin, headBone.transform.TransformVector(Vector3.back)), mesh, head.localToWorldMatrix, null
            };
            var result = (bool)intersect.Invoke(null, rayParams);
            RaycastHit hit = (RaycastHit)rayParams[3];
            if (result && Vector3.Distance(weightedPos, hit.point) < 0.03 &&
                headBone.InverseTransformPoint(hit.point).x < 0.01)
            {
                return hit.point;
            }
            else
            {
                return weightedPos;
            }
        }

        private string GetSPSMenuPath(GameObject avatarObject)
        {
            var furies = avatarObject.GetComponents<VRCFury>();
            var allSpsOptions = furies
                .SelectMany(fury =>
                    fury.config.features.OfType<SpsOptions>()).ToList();
            var spsOptions = allSpsOptions.FirstOrDefault();
            if (spsOptions != null)
            {
                return spsOptions.menuPath;
            }
            var moveMenus = furies
                .SelectMany(fury =>
                    fury.config.features.OfType<MoveMenuItem>()).ToList();
            var spsMoveMenu = moveMenus.FirstOrDefault(m => m.fromPath == "SPS");
            if(spsMoveMenu != null)
            {
                return spsMoveMenu.toPath;
            }

            return "SPS";
        }

        private (VRCFuryHapticSocket, VRCFuryHapticSocket) GetPussyAndAnal(Transform hips)
        {
            if (hips == null) return (null, null);
            var pussy = hips.Find("SPS/Pussy");
            VRCFuryHapticSocket pussySocket = null;
            if(pussy != null)
            {
                pussySocket = pussy.GetComponent<VRCFuryHapticSocket>();
            }
        
            var anal = hips.Find("SPS/Anal");
            VRCFuryHapticSocket analSocket = null;
            if(anal != null)
            {
                analSocket = anal.GetComponent<VRCFuryHapticSocket>();
            }
            return (pussySocket, analSocket);
        }

        class ParsedSockets
        {
            public Transform pussy;
        }

        private void ParseSockets(AvatarArmature armature)
        {
            var avatarObject = armature.AvatarObject;

            var furies = avatarObject.GetComponents<VRCFury>();
            var moveMenus = furies
                .SelectMany(fury =>
                    fury.config.features.OfType<MoveMenuItem>()).ToList();
            var toggles = furies
                .SelectMany(fury =>
                    fury.config.features.OfType<Toggle>()).ToList();
            var setIcon = furies
                .SelectMany(fury =>
                    fury.config.features.OfType<SetIcon>()).ToList();

            var hips = armature.FindBone(HumanBodyBones.Hips);
            var spsHips = hips.Find("SPS");
            if (spsHips != null)
            {
                var pussy = spsHips.Find(PussyName);
                VRCFuryHapticSocket socketPussy = null;
                if (pussy != null) pussy.GetComponent<VRCFuryHapticSocket>();
                List<VRCFuryHapticSocket.DepthAction> depthActions = null;
                //if (socketPussy != null) depthActions = socketPussy.depthActions.Where(action => action.).ToList();
                var anal = spsHips.Find(AnalName);
                VRCFuryHapticSocket socketAnal = null;
                if (anal != null) anal.GetComponent<VRCFuryHapticSocket>();
            }

            string spsPath = null;
            var spsOptions = avatarObject.GetComponent<SpsOptions>();
            if (spsOptions != null) spsPath = spsOptions.menuPath;
            if (spsPath == null)
            {
                spsPath = moveMenus.FirstOrDefault(m => m.fromPath == "SPS")?.toPath;
            }

            var soundToggle = toggles.FirstOrDefault(t => t.globalParam == "WH_SFX_On");
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


        private void OnEnable()
        {
            logo = Resources.Load<Texture2D>("SPS Logo");
            categoryLabelBackground = new Texture2D(1, 1);
            categoryLabelBackground.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
            categoryLabelBackground.Apply();
            meshes = SelectedAvatar?.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        public void OnSelectionChange()
        {
            meshes = SelectedAvatar?.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var selectedAvatar = SelectedAvatar;
            if (selectedAvatar != null)
            {
                spsMenuPath = GetSPSMenuPath(SelectedAvatar.gameObject);
            }
            else
            {
                spsMenuPath = "SPS";
            }
            Repaint();
        }

        public void SetBlendshapes()
        {
            var @base = Bases.All[selectedBase];
            pussyBlendshape.Clear();
            pussyBlendshape.Append(@base.PussyBlendshape);
            analBlendshape.Clear();
            analBlendshape.Append(@base.AnalBlendshape);
        }

        private VRCAvatarDescriptor SelectedAvatar
        {
            get
            {
                VRCAvatarDescriptor selectedAvatar = null;
                if (Selection.activeTransform != null)
                {
                    var gameObject = Selection.activeTransform.gameObject;
                    selectedAvatar = gameObject.GetComponent<VRCAvatarDescriptor>();
                    if (selectedAvatar == null)
                    {
                        selectedAvatar = gameObject.GetComponentsInParent<VRCAvatarDescriptor>(true)
                            .FirstOrDefault(); // TODO: use last?
                    }
                }

                return selectedAvatar;
            }
        }

        private static Vector3 FindPrimaryDirection(Vector3 target)
        {
            var spineCoords = new[] { target.x, target.y, target.z }.Select(Math.Abs).ToList();
            var primaryIndex = spineCoords.IndexOf(spineCoords.Max());
            var primarySign = Math.Sign(target[primaryIndex]);
            var primary = Vector3.zero;
            primary[primaryIndex] = 1f * primarySign;
            return primary;
        }

        public static (Quaternion, Quaternion) DetermineArmatureAlignment(AvatarArmature armature)
        {
            var hips = armature.FindBone(HumanBodyBones.Hips);
            Debug.Log(Math.Abs(hips.localPosition.x) < 0.01);
            var upperLegRight = armature.FindBone(HumanBodyBones.RightUpperLeg);
            var upperLegLeft = armature.FindBone(HumanBodyBones.LeftUpperLeg);
            int symmetricIndex = -1;
            for (int i = 0; i < 3; i++)
            {
                var leftCoord = upperLegLeft.localPosition[i];
                var rightCoord = upperLegRight.localPosition[i];
                Debug.Assert(Math.Abs(Math.Abs(leftCoord) - Math.Abs(rightCoord)) < 0.001);
            }

            var primary = FindPrimaryDirection(armature.FindBone(HumanBodyBones.Spine).localPosition);
            var primaryRotation = Quaternion.FromToRotation(hips.transform.up, hips.TransformDirection(primary));
            var secondaryRotation =
                Quaternion.FromToRotation(primaryRotation * hips.transform.right,
                    armature.AvatarObject.transform.right);
            var aligned = new GameObject("Aligned");
            aligned.transform.SetParent(hips, false);
            aligned.transform.localRotation = primaryRotation * secondaryRotation;
            var secondaryLeftArmRotation =
                Quaternion.FromToRotation(armature.FindBone(HumanBodyBones.LeftUpperArm).right,
                    -armature.AvatarObject.transform.forward);
            var aligned2 = new GameObject("Aligned");
            aligned2.transform.SetParent(armature.FindBone(HumanBodyBones.LeftUpperArm), false);
            aligned2.transform.localRotation = primaryRotation * secondaryLeftArmRotation;
            var secondaryRightArmRotation =
                Quaternion.FromToRotation(armature.FindBone(HumanBodyBones.RightUpperArm).right,
                    armature.AvatarObject.transform.forward);
            var aligned3 = new GameObject("Aligned");
            aligned3.transform.SetParent(armature.FindBone(HumanBodyBones.RightUpperArm), false);
            aligned3.transform.localRotation = primaryRotation;
            return (primaryRotation, secondaryRotation);
        }


        // TODO: Use root bone?
        public static Dictionary<string, Transform> BuildSkeleton(SkinnedMeshRenderer[] meshes, HumanBone[] bones)
        {
            var boneToHuman = bones.ToDictionary(bone => bone.boneName, bone => bone.humanName);
            var humanToTransform = new Dictionary<string, Transform>();
            var transforms = new HashSet<Transform>(meshes.SelectMany(mesh => mesh.bones));
            foreach (var bone in transforms)
            {
                if (bone != null)
                {
                    if (boneToHuman.TryGetValue(bone.name, out var humanBoneName))
                    {
                        humanToTransform[humanBoneName] = bone;
                    }
                }
            }

            // Sometimes Unity reports wrong Transform for Hips (Reference to armature object???)
            if (!humanToTransform.ContainsKey("Hips"))
            {
                humanToTransform["Hips"] = humanToTransform["Spine"].parent;
            }

            return humanToTransform;
        }

        public class AvatarArmature : IDisposable
        {
            public readonly GameObject AvatarObject;


            public AvatarArmature(GameObject avatarObject)
            {
                this.AvatarObject = avatarObject;
                VRCFArmatureUtils.WarmupCache(this.AvatarObject);
            }

            public Transform FindBone(HumanBodyBones findBone)
            {
                return VRCFArmatureUtils.FindBoneOnArmatureOrException(AvatarObject, findBone).transform;
            }
            
            public Transform FindBoneOrNull(HumanBodyBones findBone)
            {
                var bone = VRCFArmatureUtils.FindBoneOnArmatureOrNull(AvatarObject, findBone);
                return bone == null ? null : bone.transform;
            }

            public Quaternion CalcAlignmentDelta(HumanBodyBones bone, Vector3 primaryTarget, Vector3 secondaryTarget)
            {
                var transform = FindBone(bone);
                var targetCoords = new[] { primaryTarget.x, primaryTarget.y, primaryTarget.z }.Select(Math.Abs)
                    .ToList();
                var primaryIndex = targetCoords.IndexOf(targetCoords.Max());
                var primarySign = Math.Sign(primaryTarget[primaryIndex]);
                var primary = Vector3.zero;
                primary[primaryIndex] = 1f * primarySign;
                var primaryRotation = Quaternion.FromToRotation(transform.up, transform.TransformDirection(primary));
                var secondaryRotation =
                    Quaternion.FromToRotation((transform.rotation * primaryRotation) * Vector3.right, secondaryTarget);
                var aligned = new GameObject("Aligned");
                aligned.transform.SetParent(transform, false);
                aligned.transform.localRotation = primaryRotation * secondaryRotation;
                return primaryRotation * secondaryRotation;
            }

            public void Dispose()
            {
                VRCFArmatureUtils.ClearCache();
            }
        }

        private void DrawSymmetricToggle(string name, ref bool on, ref bool left, ref bool right)
        {
            using (new GUILayout.HorizontalScope())
            {
                on = EditorGUILayout.ToggleLeft(name, on, GUILayout.Width(64 + 16));
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(!on))
                {
                    left = EditorGUILayout.ToggleLeft("Left", left, GUILayout.Width(64));
                    right = EditorGUILayout.ToggleLeft("Right", right, GUILayout.Width(64));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSymmetricBothToggle(string name, ref bool on, ref bool left, ref bool right, ref bool both)
        {
            using (new GUILayout.HorizontalScope())
            {
                on = EditorGUILayout.ToggleLeft(name, on, GUILayout.Width(64 + 16));
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(!on))
                {
                    left = EditorGUILayout.ToggleLeft("Left", left, GUILayout.Width(64));
                    right = EditorGUILayout.ToggleLeft("Right", right, GUILayout.Width(64));
                    both = EditorGUILayout.ToggleLeft("Double", both, GUILayout.Width(64));
                }

                EditorGUILayout.EndHorizontal();
            }
        }


        private void DrawBlendshapeToggle(IEnumerable<SkinnedMeshRenderer> meshes, string name, ref bool on,
            StringBuilder blendshape)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                on = EditorGUILayout.ToggleLeft(name, on, GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();


                using (new EditorGUI.DisabledScope(!on || meshes == null))
                {
                    if (EditorGUILayout.DropdownButton(
                            new GUIContent(blendshape.Length > 0 ? blendshape.ToString() : "None"), FocusType.Keyboard,
                            GUILayout.Width(64 * 3 + 8)))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("None"),
                            blendshape.Length == 0, () => { blendshape.Clear(); });

                        foreach (var mesh in meshes)
                        {
                            for (int j = 0; j < mesh.sharedMesh.blendShapeCount; j++)
                            {
                                var blendshapeName = mesh.sharedMesh.GetBlendShapeName(j);
                                var menuEntry = $"{mesh.name}/{blendshapeName}";
                                menu.AddItem(new GUIContent(menuEntry),
                                    blendshape.ToString() == blendshapeName,
                                    blendshapeNameObject =>
                                    {
                                        blendshape.Clear();
                                        blendshape.Append(blendshapeNameObject as string);
                                    },
                                    blendshapeName);
                            }
                        }

                        menu.ShowAsContext();
                    }
                }
            }
        }

        private bool HasExistingSockets(GameObject avatarObject)
        {
            string[] names =
            {
                BlowjobName, $"{HandjobName} Right", $"{HandjobName} Left", $"Double {HandjobName}", PussyName,
                AnalName, TitjobName, AssjobName, ThighjobName, $"{SoleName} Left", $"{SoleName} Right",
                FootjobName
            };
            var sockets = avatarObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);
            var socketCount = sockets.Count(socket =>
                names.Contains(socket.gameObject.name) && socket.transform.parent.name == "SPS");
            return socketCount > 0;
        }

        private Dictionary<string, VRCFuryHapticSocket> ReadExistingSockets(GameObject avatarObject)
        {
            string[] names =
            {
                BlowjobName, $"{HandjobName} Right", $"{HandjobName} Left", $"Double {HandjobName}", PussyName,
                AnalName, TitjobName, AssjobName, ThighjobName, $"{SoleName} Left", $"{SoleName} Right",
                FootjobName
            };
            var sockets = avatarObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);
            var filteredSockets = sockets.Where(socket =>
                    names.Contains(socket.gameObject.name) && socket.transform.parent.name == "SPS")
                .ToDictionary(socket => socket.gameObject.name);

            // Delete SFX

            foreach (var socket in filteredSockets.Values)
            {
                socket.transform.SetParent(null, false);
            }

            return filteredSockets;
            /*var socketChildren = new Dictionary<string, List<GameObject>>();
            return filteredSockets
                .Select(socket =>
            {
                //var obj = Object.Instantiate(socket);
                socket
                obj.gameObject.name = socket.gameObject.name;
                return obj;
            })
                .ToDictionary(socket => socket.gameObject.name);*/
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
        
        private bool HasSFX(Transform hips)
        {
            if (hips == null) return false;
            var pussySFX = hips.Find("SPS/Pussy/SFX");
            if (pussySFX != null) return true;
            var analSFX = hips.Find("SPS/Anal/SFX");
            if (pussySFX != null) return true;
            return false;
        }

        private void DeleteSFXFromSocket(VRCFuryHapticSocket socket)
        {
            var sfx = socket.transform.Find("SFX");
            var sfxToggle = socket.activeActions.actions.OfType<ObjectToggleAction>()
                .FirstOrDefault(o => o.obj == sfx.gameObject);
            var sfxDepth = socket.depthActions.OfType<FxFloatAction>().FirstOrDefault(fx => fx.name == "WH_SFX_Depth");
        }

        private void UpdateMenu()
        {
        }

        private Vector3 GetAlignDelta(Transform transform)
        {
            var alignedX = Vector3.Cross(transform.up, Vector3.up);
            var sign = Mathf.Sign(Vector3.Dot(transform.up, Vector3.right));
            return new Vector3(0, sign * Vector3.Angle(transform.right, alignedX), 0);
        }

        private Transform CreateAligned(Transform parent)
        {
            var aligned = new GameObject("Aligned");
            aligned.transform.SetParent(parent, false);
            var alignedX = Vector3.Cross(parent.up, Vector3.up);
            var sign = Mathf.Sign(Vector3.Dot(parent.up, Vector3.right));
            aligned.transform.localEulerAngles =
                new Vector3(0, sign * Vector3.Angle(parent.transform.right, alignedX), 0);
            return aligned.transform;
        }

        private void SetParentLocalPositionEulerAngles(Transform transform, Transform parent, Vector3 position,
            Vector3 rotation)
        {
            transform.SetParent(parent, false);
            transform.localPosition = position;
            transform.localEulerAngles = rotation;
        }

        private void SetSymmetricParent(GameObject gameObject, Transform leftTarget, Transform rightTarget,
            Vector3 offsetPosition, Vector3 offsetRotation, bool signLeft = false)
        {
            var parent = gameObject.AddComponent<ParentConstraint>();
            parent.AddSource(new ConstraintSource
            {
                sourceTransform = leftTarget.transform,
                weight = 1
            });
            parent.AddSource(new ConstraintSource
            {
                sourceTransform = rightTarget.transform,
                weight = 1
            });
            parent.SetTranslationOffset(0, offsetPosition);
            parent.SetRotationOffset(0,
                signLeft ? Vector3.Scale(offsetRotation, new Vector3(1, -1, 1)) : offsetRotation);
            parent.SetTranslationOffset(1, offsetPosition);
            parent.SetRotationOffset(1, offsetRotation);
            parent.locked = true;
            parent.constraintActive = true;
        }

        private void SetSymmetricParent2(GameObject gameObject, Transform leftTarget, Transform rightTarget,
            Vector3 leftOffsetPosition, Vector3 leftOffsetRotation, Vector3 rightOffsetPosition,
            Vector3 rightOffsetRotation)
        {
            var parent = gameObject.AddComponent<ParentConstraint>();
            parent.AddSource(new ConstraintSource
            {
                sourceTransform = leftTarget.transform,
                weight = 1
            });
            parent.AddSource(new ConstraintSource
            {
                sourceTransform = rightTarget.transform,
                weight = 1
            });
            parent.SetTranslationOffset(0, leftOffsetPosition);
            parent.SetRotationOffset(0,
                leftOffsetRotation);
            parent.SetTranslationOffset(1, rightOffsetPosition);
            parent.SetRotationOffset(1, rightOffsetRotation);
            parent.locked = true;
            parent.constraintActive = true;
        }

        public VRCFuryHapticSocket CreateSocket(string name, VRCFuryHapticSocket.AddLight light, bool auto,
            string category = null)
        {
            var gameObject = new GameObject(name);
            var socketVrcf = gameObject.AddComponent<VRCFuryHapticSocket>();
            socketVrcf.Version = 7;
            socketVrcf.addLight = light;

            socketVrcf.name = String.IsNullOrWhiteSpace(category) ? name : $"{category}/{name}";
            socketVrcf.enableAuto = auto;
            return socketVrcf;
        }

        public static void AddBlendshape(VRCFuryHapticSocket hapticSocket, string blendshape)
        {
            var state = new State();
            state.actions.Add(new BlendShapeAction
            {
                blendShape = blendshape
            });
            hapticSocket.enableDepthAnimations = true;
            hapticSocket.depthActions.Add(new VRCFuryHapticSocket.DepthAction
            {
                state = state,
                enableSelf = true,
                startDistance = 0.05f,
                endDistance = 0,
                smoothingSeconds = 0,
            });
        }

        private void apply()
        {
            var @base = Bases.All[selectedBase];
            VRCAvatarDescriptor vrcAvatar = SelectedAvatar;

            var avatarGameObject = vrcAvatar.gameObject;
            var meshes = vrcAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(mesh => mesh.transform.parent == avatarGameObject.transform)
                .ToArray();
            var armature = new AvatarArmature(avatarGameObject);
            var armatureTransform = armature.FindBone(HumanBodyBones.Hips).parent;
            Debug.Assert(armatureTransform.parent == avatarGameObject.transform,
                "Armature is doesn't share parent with mesh");
            var existingSockets = new Dictionary<string, VRCFuryHapticSocket>();
            var keep = false;
            if (HasExistingSockets(avatarGameObject))
            {
                var result = EditorUtility.DisplayDialogComplex("Warning",
                    "Found existing sockets. Do you want to keep or clear them?", "Keep", "Cancel", "Clear");
                switch (result)
                {
                    case 0: // Keep
                        keep = true;
                        existingSockets = ReadExistingSockets(avatarGameObject);
                        break;
                    case 1: // Cancel
                        return;
                    case 2: // Clear
                        keep = false;
                        Clear(avatarGameObject);
                        break;
                }
            }
            // TODO: Handle no visemes

            var armatureScale = armatureTransform.localScale;
            var inverseArmatureScale = new Vector3(1 / armatureScale.x, 1 / armatureScale.y,
                1 / armatureScale.z);
            var avatarScale = vrcAvatar.transform.localScale;
            var inverseAvatarScale = new Vector3(1 / avatarScale.x, 1 / avatarScale.y,
                1 / avatarScale.z);
            var hipLength = Vector3.Scale(
                armature.FindBone(HumanBodyBones.Spine)
                    .position - armature.FindBone(HumanBodyBones.Hips).position,
                inverseAvatarScale).magnitude;

            // var hipLength = Vector3.Scale(humanToTransform["Spine"].localPosition, armatureScale).magnitude;
            var bakedScale = hipLength / @base.DefaultHipLength;
            if (selectedBase == 0) // Generic
            {
                var torsoLength = armature.FindBone(HumanBodyBones.Hips)
                    .InverseTransformPoint(armature.FindBone(HumanBodyBones.Neck).position)
                    .magnitude;
                bakedScale = torsoLength / @base.DefaultTorsoLength;
            }

            var (sfxPrefab, sfxBJPrefab) = CopyAssets();
            var createdSockets = new List<VRCFuryHapticSocket>();
            if (defaultOn)
            {
                if (blowjobOn)
                {
                    if (keep && existingSockets.TryGetValue(BlowjobName, out var existingSocket))
                    {
                        existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Head), false);
                        if (existingSocket.depthActions.Count == 0)
                        {
                            AddBlendshape(existingSocket, blowjobBlendshape.ToString());
                        }

                        createdSockets.Add(existingSocket);
                        if (sfxOn && sfxBlowjobOn)
                        {
                            // TODO: Is this enough? Object toggle is null if SFX object is missing. No need to delete a None object toggle
                            var existingSFX = existingSocket.transform.Find("SFX BJ");
                            Transform sfx;
                            if (existingSFX != null)
                            {
                                var version = GetSFXVersion(existingSocket);
                                if (version != null && version >= CurrentVersion)
                                {
                                    sfx = existingSFX;
                                }
                                else
                                {
                                    sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxBJPrefab,
                                        existingSocket.transform)).transform;
                                    Object.DestroyImmediate(existingSFX.gameObject);
                                }
                            }
                            else
                            {
                                sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxBJPrefab,
                                    existingSocket.transform)).transform;
                            }

                            existingSocket.enableDepthAnimations = true;
                            if (!existingSocket.depthActions.Any(action =>
                                    action.state.actions.Any(action2 =>
                                        action2 is FxFloatAction fx && fx.name == "WH_SFX_Depth_Blowjob")))
                            {
                                var fxState = new State();
                                fxState.actions.Add(new FxFloatAction()
                                {
                                    name = "WH_SFX_Depth_Blowjob"
                                });
                                existingSocket.depthActions.Add(new VRCFuryHapticSocket.DepthAction
                                {
                                    state = fxState,
                                    enableSelf = true,
                                    startDistance = 0,
                                    endDistance = -0.5f,
                                    smoothingSeconds = 0,
                                });
                            }

                            existingSocket.enableActiveAnimation = true;
                            if (existingSocket.activeActions.actions.OfType<ObjectToggleAction>()
                                .All(o => o.obj != sfx.gameObject))
                            {
                                if (existingSocket.activeActions?.actions == null)
                                {
                                    existingSocket.activeActions = new State();
                                }

                                existingSocket.activeActions.actions.Add(new ObjectToggleAction
                                {
                                    obj = sfx.gameObject
                                });
                                existingSocket.activeActions.actions.RemoveAll(action =>
                                {
                                    if (action is ObjectToggleAction o)
                                    {
                                        return o.obj == null;
                                    }

                                    return false;
                                });
                            }
                        }
                    }
                    else
                    {
                        var mouthPosition = new Vector3(0, 0.01f, 0.075f);
                        if (vrcAvatar.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                        {
                            var visemOhBlendshapeName =
                                vrcAvatar.VisemeBlendShapes[(int)VRC_AvatarDescriptor.Viseme.oh];
                            if (!string.IsNullOrEmpty(visemOhBlendshapeName))
                            {
                                mouthPosition = armature.FindBone(HumanBodyBones.Head).InverseTransformPoint(
                                    DetectMouthPosition(
                                        vrcAvatar.VisemeSkinnedMesh,
                                        vrcAvatar.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(
                                            visemOhBlendshapeName), armature.FindBone(HumanBodyBones.Head)));
                            }
                        }

                        var socket = CreateSocket(BlowjobName, VRCFuryHapticSocket.AddLight.Hole, true);
                        SetParentLocalPositionEulerAngles(socket.transform, armature.FindBone(HumanBodyBones.Head),
                            mouthPosition,
                            Vector3.zero);
                        AddBlendshape(socket, blowjobBlendshape.ToString());
                        createdSockets.Add(socket);
                        if (sfxOn && sfxBlowjobOn)
                        {
                            var sfx = PrefabUtility.InstantiatePrefab(sfxBJPrefab,
                                socket.transform) as GameObject;
                            socket.enableActiveAnimation = true;
                            socket.activeActions = new State();
                            socket.activeActions.actions.Add(new ObjectToggleAction
                            {
                                obj = sfx
                            });
                            var fxState = new State();
                            fxState.actions.Add(new FxFloatAction()
                            {
                                name = "WH_SFX_Depth_Blowjob"
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
                    }
                }

                if (handjobOn)
                {
                    var leftAlignDelta = GetAlignDelta(armature.FindBone(HumanBodyBones.LeftHand));
                    var rightAlignDelta = GetAlignDelta(armature.FindBone(HumanBodyBones.RightHand));
                    if (handjobLeftOn)
                    {
                        if (keep && existingSockets.TryGetValue($"{HandjobName} Left", out var existingSocket))
                        {
                            existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.LeftHand), false);
                            createdSockets.Add(existingSocket);
                        }
                        else
                        {
                            var socket = CreateSocket($"{HandjobName} Left", VRCFuryHapticSocket.AddLight.Ring, true,
                                "Handjob");
                            SetParentLocalPositionEulerAngles(socket.transform,
                                armature.FindBone(HumanBodyBones.LeftHand),
                                Vector3.Scale(@base.Hand.Positon, inverseArmatureScale) * bakedScale,
                                Vector3.Scale(@base.Hand.EulerAngles, new Vector3(1, -1, 1)) + leftAlignDelta);
                            createdSockets.Add(socket);
                        }
                    }

                    if (handjobRightOn)
                    {
                        if (keep && existingSockets.TryGetValue($"{HandjobName} Right", out var existingSocket))
                        {
                            existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.RightHand), false);
                            createdSockets.Add(existingSocket);
                        }
                        else
                        {
                            var socket = CreateSocket($"{HandjobName} Right", VRCFuryHapticSocket.AddLight.Ring, true,
                                "Handjob");
                            SetParentLocalPositionEulerAngles(socket.transform,
                                armature.FindBone(HumanBodyBones.RightHand),
                                Vector3.Scale(@base.Hand.Positon, inverseArmatureScale) * bakedScale,
                                @base.Hand.EulerAngles + rightAlignDelta);
                            createdSockets.Add(socket);
                        }
                    }

                    if (handjobBothOn)
                    {
                        if (keep && existingSockets.TryGetValue($"Double {HandjobName}", out var existingSocket))
                        {
                            existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                            createdSockets.Add(existingSocket);
                        }
                        else
                        {
                            var socket = CreateSocket($"Double {HandjobName}", VRCFuryHapticSocket.AddLight.Ring, false,
                                "Handjob");
                            socket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                            SetSymmetricParent2(socket.gameObject, armature.FindBone(HumanBodyBones.LeftHand),
                                armature.FindBone(HumanBodyBones.RightHand),
                                Vector3.Scale(@base.Hand.Positon, avatarScale) * bakedScale, // World Scale
                                Vector3.Scale(@base.Hand.EulerAngles, new Vector3(1, -1, 1)) + leftAlignDelta,
                                Vector3.Scale(@base.Hand.Positon, avatarScale) * bakedScale, // World Scale
                                @base.Hand.EulerAngles + rightAlignDelta);
                            createdSockets.Add(socket);
                        }
                    }
                }

                if (pussyOn)
                {
                    if (keep && existingSockets.TryGetValue(PussyName, out var existingSocket))
                    {
                        existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                        createdSockets.Add(existingSocket);
                        if (sfxOn && sfxPussyOn)
                        {
                            // TODO: Is this enough? Object toggle is null if SFX object is missing. No need to delete a None object toggle
                            var existingSFX = existingSocket.transform.Find("SFX");
                            Transform sfx;
                            if (existingSFX != null)
                            {
                                var version = GetSFXVersion(existingSocket);
                                if (version != null && version >= CurrentVersion)
                                {
                                    sfx = existingSFX;
                                }
                                else
                                {
                                    sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxPrefab,
                                        existingSocket.transform)).transform;
                                    Object.DestroyImmediate(existingSFX.gameObject);
                                }
                            }
                            else
                            {
                                sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxPrefab,
                                    existingSocket.transform)).transform;
                            }

                            existingSocket.enableDepthAnimations = true;
                            if (!existingSocket.depthActions.Any(action =>
                                    action.state.actions.Any(action2 =>
                                        action2 is FxFloatAction fx && fx.name == "WH_SFX_Depth")))
                            {
                                var fxState = new State();
                                fxState.actions.Add(new FxFloatAction()
                                {
                                    name = "WH_SFX_Depth"
                                });
                                existingSocket.depthActions.Add(new VRCFuryHapticSocket.DepthAction
                                {
                                    state = fxState,
                                    enableSelf = true,
                                    startDistance = 0,
                                    endDistance = -0.5f,
                                    smoothingSeconds = 0,
                                });
                            }

                            existingSocket.enableActiveAnimation = true;
                            if (existingSocket.activeActions.actions.OfType<ObjectToggleAction>()
                                .All(o => o.obj != sfx.gameObject))
                            {
                                if (existingSocket.activeActions?.actions == null)
                                {
                                    existingSocket.activeActions = new State();
                                }

                                existingSocket.activeActions.actions.Add(new ObjectToggleAction
                                {
                                    obj = sfx.gameObject
                                });
                                existingSocket.activeActions.actions.RemoveAll(action =>
                                {
                                    if (action is ObjectToggleAction o)
                                    {
                                        return o.obj == null;
                                    }

                                    return false;
                                });
                            }
                        }
                    }
                    else
                    {
                        var socket = CreateSocket(PussyName, VRCFuryHapticSocket.AddLight.Hole, true);
                        SetParentLocalPositionEulerAngles(socket.transform, armature.FindBone(HumanBodyBones.Hips),
                            Vector3.Scale(@base.Pussy.Positon, inverseArmatureScale) * bakedScale,
                            @base.Pussy.EulerAngles);
                        AddBlendshape(socket, pussyBlendshape.ToString());
                        createdSockets.Add(socket);
                        if (sfxOn && sfxPussyOn)
                        {
                            var sfx = PrefabUtility.InstantiatePrefab(sfxPrefab,
                                socket.transform) as GameObject;
                            socket.enableActiveAnimation = true;
                            socket.activeActions = new State();
                            socket.activeActions.actions.Add(new ObjectToggleAction
                            {
                                obj = sfx
                            });
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
                    }
                }

                if (analOn)
                {
                    if (keep && existingSockets.TryGetValue(AnalName, out var existingSocket))
                    {
                        existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                        createdSockets.Add(existingSocket);
                        if (sfxOn && sfxAnalOn)
                        {
                            // TODO: Is this enough? Object toggle is null if SFX object is missing. No need to delete a None object toggle
                            var existingSFX = existingSocket.transform.Find("SFX");
                            Transform sfx;
                            if (existingSFX != null)
                            {
                                var version = GetSFXVersion(existingSocket);
                                if (version != null && version >= CurrentVersion)
                                {
                                    sfx = existingSFX;
                                }
                                else
                                {
                                    sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxPrefab,
                                        existingSocket.transform)).transform;
                                    Object.DestroyImmediate(existingSFX.gameObject);
                                }
                            }
                            else
                            {
                                sfx = ((GameObject)PrefabUtility.InstantiatePrefab(sfxPrefab,
                                    existingSocket.transform)).transform;
                            }

                            existingSocket.enableDepthAnimations = true;
                            if (!existingSocket.depthActions.Any(action =>
                                    action.state.actions.Any(action2 =>
                                        action2 is FxFloatAction fx && fx.name == "WH_SFX_Depth")))
                            {
                                var fxState = new State();
                                fxState.actions.Add(new FxFloatAction()
                                {
                                    name = "WH_SFX_Depth"
                                });
                                existingSocket.depthActions.Add(new VRCFuryHapticSocket.DepthAction
                                {
                                    state = fxState,
                                    enableSelf = true,
                                    startDistance = 0,
                                    endDistance = -0.5f,
                                    smoothingSeconds = 0,
                                });
                            }

                            existingSocket.enableActiveAnimation = true;
                            if (existingSocket.activeActions.actions.OfType<ObjectToggleAction>()
                                .All(o => o.obj != sfx.gameObject))
                            {
                                if (existingSocket.activeActions?.actions == null)
                                {
                                    existingSocket.activeActions = new State();
                                }

                                existingSocket.activeActions.actions.Add(new ObjectToggleAction
                                {
                                    obj = sfx.gameObject
                                });
                                existingSocket.activeActions.actions.RemoveAll(action =>
                                {
                                    if (action is ObjectToggleAction o)
                                    {
                                        return o.obj == null;
                                    }

                                    return false;
                                });
                            }
                        }
                    }
                    else
                    {
                        var socket = CreateSocket(AnalName, VRCFuryHapticSocket.AddLight.Hole, false);
                        SetParentLocalPositionEulerAngles(socket.transform, armature.FindBone(HumanBodyBones.Hips),
                            Vector3.Scale(@base.Anal.Positon, inverseArmatureScale) * bakedScale,
                            @base.Anal.EulerAngles);
                        AddBlendshape(socket, analBlendshape.ToString());
                        createdSockets.Add(socket);
                        if (sfxOn && sfxAnalOn)
                        {
                            var sfx = PrefabUtility.InstantiatePrefab(sfxPrefab,
                                socket.transform) as GameObject;
                            socket.enableActiveAnimation = true;
                            socket.activeActions = new State();
                            socket.activeActions.actions.Add(new ObjectToggleAction
                            {
                                obj = sfx
                            });
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
                    }
                }
            }

            if (specialOn)
            {
                if (titjobOn)
                {
                    if (keep && existingSockets.TryGetValue(TitjobName, out var existingSocket))
                    {
                        existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Chest), false);
                        createdSockets.Add(existingSocket);
                    }
                    else
                    {
                        var socket = CreateSocket(TitjobName, VRCFuryHapticSocket.AddLight.Ring, false, "Special");
                        SetParentLocalPositionEulerAngles(socket.transform, armature.FindBone(HumanBodyBones.Chest),
                            Vector3.Scale(@base.Titjob.Positon, inverseArmatureScale) * bakedScale,
                            @base.Titjob.EulerAngles);
                        createdSockets.Add(socket);
                    }
                }

                if (assjobOn)
                {
                    if (keep && existingSockets.TryGetValue(AssjobName, out var existingSocket))
                    {
                        existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                        createdSockets.Add(existingSocket);
                    }
                    else
                    {
                        var socket = CreateSocket(AssjobName, VRCFuryHapticSocket.AddLight.Ring, false, "Special");
                        SetParentLocalPositionEulerAngles(socket.transform, armature.FindBone(HumanBodyBones.Hips),
                            Vector3.Scale(@base.Assjob.Positon, inverseArmatureScale) * bakedScale,
                            @base.Assjob.EulerAngles);
                        createdSockets.Add(socket);
                    }
                }

                if (thighjobOn)
                {
                    if (keep && existingSockets.TryGetValue(ThighjobName, out var existingSocket))
                    {
                        existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                        createdSockets.Add(existingSocket);
                    }
                    else
                    {
                        var socket = CreateSocket(ThighjobName, VRCFuryHapticSocket.AddLight.Ring, false, "Special");
                        socket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                        SetSymmetricParent(socket.gameObject, armature.FindBone(HumanBodyBones.LeftUpperLeg),
                            armature.FindBone(HumanBodyBones.RightUpperLeg),
                            Vector3.Scale(@base.Thighjob.Positon, avatarScale) * bakedScale, // World Scale
                            @base.Thighjob.EulerAngles);
                        createdSockets.Add(socket);
                    }
                }
            }

            if (feetOn)
            {
                if (soleOn)
                {
                    Vector3 solePosition;
                    Vector3 soleRotation;
                    if (selectedFootType == FootType.Flat)
                    {
                        solePosition = @base.SoleFlat.Positon;
                        soleRotation = @base.SoleFlat.EulerAngles;
                    }
                    else
                    {
                        solePosition = @base.SoleHeeled.Positon;
                        soleRotation = @base.SoleHeeled.EulerAngles;
                    }

                    if (soleLeftOn)
                    {
                        if (keep && existingSockets.TryGetValue($"{SoleName} Left", out var existingSocket))
                        {
                            existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.LeftFoot), false);
                            createdSockets.Add(existingSocket);
                        }
                        else
                        {
                            var socket = CreateSocket($"{SoleName} Left", VRCFuryHapticSocket.AddLight.Ring, true,
                                "Feet");
                            SetParentLocalPositionEulerAngles(socket.transform,
                                armature.FindBone(HumanBodyBones.LeftFoot),
                                Vector3.Scale(solePosition, inverseArmatureScale) * bakedScale,
                                soleRotation);
                            createdSockets.Add(socket);
                        }
                    }

                    if (soleRightOn)
                    {
                        if (keep && existingSockets.TryGetValue($"{SoleName} Right", out var existingSocket))
                        {
                            existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.RightFoot), false);
                            createdSockets.Add(existingSocket);
                        }
                        else
                        {
                            var socket = CreateSocket($"{SoleName} Right", VRCFuryHapticSocket.AddLight.Ring, true,
                                "Feet");
                            SetParentLocalPositionEulerAngles(socket.transform,
                                armature.FindBone(HumanBodyBones.RightFoot),
                                Vector3.Scale(solePosition, inverseArmatureScale) * bakedScale,
                                soleRotation);
                            createdSockets.Add(socket);
                        }
                    }
                }

                if (footjobOn)
                {
                    Vector3 footjobPosition;
                    Vector3 footjobRotation;
                    if (selectedFootType == FootType.Flat)
                    {
                        footjobPosition = @base.FootjobFlat.Positon;
                        footjobRotation = @base.FootjobHeeled.EulerAngles;
                    }
                    else
                    {
                        footjobPosition = @base.FootjobHeeled.Positon;
                        footjobRotation = @base.FootjobHeeled.EulerAngles;
                    }

                    if (keep && existingSockets.TryGetValue(FootjobName, out var existingSocket))
                    {
                        existingSocket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                        createdSockets.Add(existingSocket);
                    }
                    else
                    {
                        var socket = CreateSocket($"{FootjobName}", VRCFuryHapticSocket.AddLight.Ring, false, "Feet");
                        socket.transform.SetParent(armature.FindBone(HumanBodyBones.Hips), false);
                        SetSymmetricParent(socket.gameObject, armature.FindBone(HumanBodyBones.LeftFoot),
                            armature.FindBone(HumanBodyBones.RightFoot),
                            Vector3.Scale(footjobPosition, avatarScale) * bakedScale, footjobRotation); // World Scale
                        createdSockets.Add(socket);
                    }
                }
            }

            foreach (var socket in createdSockets)
            {
                Transform transform;
                if (socket.transform.parent.name == "Aligned")
                {
                    transform = socket.transform.parent;
                }
                else
                {
                    transform = socket.transform;
                }

                if (transform.parent.name != "SPS")
                {
                    if (transform.parent.Find("SPS") == null)
                    {
                        var sps = new GameObject("SPS");
                        sps.transform.SetParent(transform.parent, false);
                        transform.SetParent(sps.transform, true);
                    }
                    else
                    {
                        transform.SetParent(transform.parent.Find("SPS"), true);
                    }
                }
            }

            if (keep)
            {
                if (sfxOn && (sfxPussyOn || sfxAnalOn))
                {
                    var vrcFurys = avatarGameObject.GetComponents<VRCFury>();
                    var hasSfxToggle = vrcFurys.Any(vrcFury =>
                        vrcFury.config.features.Where(feature => feature is Toggle).Any(feature =>
                            (feature as Toggle).globalParam == "WH_SFX_On"));
                    if (!hasSfxToggle)
                    {
                        var vrcFury = vrcFurys.Length > 0 ? vrcFurys[0] : avatarGameObject.AddComponent<VRCFury>();
                        vrcFury.config.features.Add(new Toggle()
                        {
                            name = "SPS/Options/Sound FX",
                            saved = true,
                            defaultOn = true,
                            useGlobalParam = true,
                            globalParam = "WH_SFX_On"
                        });
                    }
                }
            }
            else
            {
                var vrcFury = avatarGameObject.AddComponent<VRCFury>();
                vrcFury.config.features.Add(new SpsOptions()
                {
                    //menuIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.vrcfury.vrcfury/VrcfResources/sps_icon.png"),
                    menuPath = spsMenuPath
                });
                vrcFury.config.features.Add(new SetIcon()
                {
                    path = $"{spsMenuPath}/Handjob"
                });
                vrcFury.config.features.Add(new SetIcon()
                {
                    path = $"{spsMenuPath}/Special"
                });
                vrcFury.config.features.Add(new SetIcon()
                {
                    path = $"{spsMenuPath}/Feet"
                });
                // Reorder
                vrcFury.config.features.Add(new MoveMenuItem()
                {
                    fromPath = $"{spsMenuPath}/Handjob",
                    toPath = $"{spsMenuPath}/Handjob"
                });
                vrcFury.config.features.Add(new MoveMenuItem()
                {
                    fromPath = $"{spsMenuPath}/Special",
                    toPath = $"{spsMenuPath}/Special"
                });
                vrcFury.config.features.Add(new MoveMenuItem()
                {
                    fromPath = $"{spsMenuPath}/Feet",
                    toPath = $"{spsMenuPath}/Feet"
                });
                if (sfxOn && (sfxPussyOn || sfxAnalOn))
                {
                    vrcFury.config.features.Add(new Toggle()
                    {
                        name = "SPS/Options/Sound FX",
                        saved = true,
                        defaultOn = true,
                        useGlobalParam = true,
                        globalParam = "WH_SFX_On"
                    });
                }
            }

            armature.Dispose();
        }

        public static void Clear2(GameObject avatarGameObject)
        {
            string[] socketNames =
            {
                BlowjobName, $"Handjob/{HandjobName} Right", $"Handjob/{HandjobName} Left",
                $"Handjob/Double {HandjobName}", PussyName,
                AnalName, $"Special/{TitjobName}", $"Special/{AssjobName}", $"Special/{ThighjobName}",
                $"Feet/{SoleName} Left", $"Feet/{SoleName} Right",
                $"Feet/{FootjobName}"
            };
            var sockets = avatarGameObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);

            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    if (socketNames.Contains(socket.name))
                    {
                        Transform parent = socket.transform.parent;
                        Object.DestroyImmediate(socket.gameObject);
                        if (parent.name == "SPS")
                        {
                            if (parent.childCount == 0)
                            {
                                Object.DestroyImmediate(parent.gameObject);
                            }
                        }
                    }
                }
            }

            var furies = avatarGameObject.GetComponents<VRCFury>();
            string[] possiblePaths =
            {
                "Sockets/Handjob", "Sockets/Special", "Sockets/Feet", "SPS/Handjob", "SPS/Special", "SPS/Feet"
            };
            var possibleIcons = socketNames.SelectMany(name =>
                new[]
                {
                    $"SPS/{name}", $"Sockets/{name}"
                }).Concat(possiblePaths).ToList();
            foreach (var vrcFury in furies)
            {
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && (m.fromPath == "SPS" || m.fromPath == "Sockets"));
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                vrcFury.config.features.RemoveAll(feature =>
                    feature is SetIcon i && (possibleIcons.Contains(i.path) || i.path == "SPS" || i.path == "Sockets"));
                if (vrcFury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(vrcFury);
                }
            }
        }

        // TODO: Get Socket names from Marker Component to clear VRCFury component
        public static void Clear(GameObject avatarGameObject)
        {
            Clear2(avatarGameObject);
            Clear3(avatarGameObject);
            string[] socketNames =
            {
                BlowjobName, $"{HandjobName} Right", $"{HandjobName} Left", $"Double {HandjobName}", PussyName,
                AnalName, TitjobName, AssjobName, ThighjobName, $"{SoleName} Left", $"{SoleName} Right",
                FootjobName
            };
            var sockets = avatarGameObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);

            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    if (socketNames.Contains(socket.name))
                    {
                        Transform parent = socket.transform.parent;
                        Object.DestroyImmediate(socket.gameObject);
                        if (parent.name == "SPS")
                        {
                            if (parent.childCount == 0)
                            {
                                Object.DestroyImmediate(parent.gameObject);
                            }
                        }
                    }
                }
            }

            var furies = avatarGameObject.GetComponents<VRCFury>();
            var possiblePaths = socketNames.SelectMany(name =>
                new[]
                {
                    $"SPS/{name}", $"Sockets/{name}"
                }).ToList();
            foreach (var vrcFury in furies)
            {
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && (m.fromPath == "SPS" || m.fromPath == "Sockets"));
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                if (vrcFury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(vrcFury);
                }
            }
        }

        public static void Clear3(GameObject avatarGameObject)
        {
            string[] socketNames =
            {
                BlowjobName, $"Handjob/{HandjobName} Right", $"Handjob/{HandjobName} Left",
                $"Handjob/Double {HandjobName}", PussyName,
                AnalName, $"Special/{TitjobName}", $"Special/{AssjobName}", $"Special/{ThighjobName}",
                $"Feet/{SoleName} Left", $"Feet/{SoleName} Right",
                $"Feet/{FootjobName}"
            };
            var sockets = avatarGameObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);

            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    if (socketNames.Contains(socket.name))
                    {
                        Transform parent = socket.transform.parent;
                        Object.DestroyImmediate(socket.gameObject);
                        if (parent.name == "SPS")
                        {
                            if (parent.childCount == 0)
                            {
                                Object.DestroyImmediate(parent.gameObject);
                            }
                        }
                    }
                }
            }

            var furies = avatarGameObject.GetComponents<VRCFury>();

            foreach (var vrcFury in furies)
            {
                var spsPaths = vrcFury.config.features.Where(feature => feature is SpsOptions)
                    .Select(feature => (feature as SpsOptions).menuPath).ToList();
                foreach (var spsPath in spsPaths)
                {
                    string[] possiblePaths =
                    {
                        $"{spsPath}/Handjob", $"{spsPath}/Special", $"{spsPath}/Feet",
                    };
                    vrcFury.config.features.RemoveAll(feature =>
                        feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                    vrcFury.config.features.RemoveAll(feature =>
                        feature is SetIcon i && possiblePaths.Contains(i.path));
                }

                vrcFury.config.features.RemoveAll(feature =>
                    feature is Toggle t && t.globalParam == "WH_SFX_On");

                vrcFury.config.features.RemoveAll(feature => feature is SpsOptions sps);
                if (vrcFury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(vrcFury);
                }
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.Space(16);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, new GUIStyle { fixedWidth = 340, fixedHeight = 89 });
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            if (Resources.FindObjectsOfTypeAll<VRCAvatarDescriptor>().Length == 0)
            {
                EditorGUILayout.HelpBox("No Avatar with a VRC Avatar Descriptor found on the active scene.",
                    MessageType.Warning);
            }

            var selectedAvatar = SelectedAvatar;

            using (var scope = new EditorGUILayout.HorizontalScope())
            {
                string avatarLabel;
                if (selectedAvatar != null)
                {
                    avatarLabel = selectedAvatar.name;
                }
                else
                {
                    avatarLabel = "No Avatar selected. Select an Avatar in the Hierarchy Window.";
                }

                var labelStyle = new GUIStyle(GUI.skin.box);
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.font = EditorStyles.boldLabel.font;
                labelStyle.padding = new RectOffset(8, 8, 8, 8);
                labelStyle.normal.background = labelStyle.normal.scaledBackgrounds[0];
                labelStyle.normal.textColor = EditorStyles.boldLabel.normal.textColor;
                var prefixStyle = new GUIStyle(GUI.skin.label);
                prefixStyle.padding = new RectOffset(8, 8, 8, 8);
                GUILayout.Label("Selected Avatar:", prefixStyle, GUILayout.ExpandWidth(false));
                GUILayout.Space(8);
                GUILayout.Label(avatarLabel, labelStyle);
            }

            EditorGUILayout.Space(8);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                selectedBase =
                    GUILayout.Toolbar(selectedBase, Bases.All.Select(@base => @base.Name).ToArray(),
                        GUILayout.Height(32));
                if (check.changed)
                {
                    SetBlendshapes();
                    if (selectedAvatar != null)
                    {
                        meshes = selectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    }
                    else
                    {
                        meshes = null;
                    }
                }
            }

            selectedFootType = (FootType)GUILayout.Toolbar((int)selectedFootType, new[] { "Flat", "Heeled" });

            EditorGUILayout.Space();
            var pathStyle = new GUIStyle(GUI.skin.textField)
            {
                margin = new RectOffset(16, 16, 8, 8)
            };
            spsMenuPath = EditorGUILayout.TextField("SPS Menu Path:", spsMenuPath, pathStyle);
            EditorGUILayout.BeginVertical();
            BeginCategory("Default", ref defaultOn);
            DrawLabels();
            DrawBlendshapeToggle(meshes, BlowjobName, ref blowjobOn, blowjobBlendshape);
            DrawSymmetricBothToggle(HandjobName, ref handjobOn, ref handjobLeftOn, ref handjobRightOn,
                ref handjobBothOn);
            DrawBlendshapeToggle(meshes, PussyName, ref pussyOn, pussyBlendshape);
            DrawBlendshapeToggle(meshes, AnalName, ref analOn, analBlendshape);
            EndCategory();

            EditorGUILayout.BeginHorizontal();
            BeginCategory("Special", ref specialOn);
            titjobOn = EditorGUILayout.ToggleLeft(TitjobName, titjobOn);
            assjobOn = EditorGUILayout.ToggleLeft(AssjobName, assjobOn);
            thighjobOn = EditorGUILayout.ToggleLeft(ThighjobName, thighjobOn);
            EndCategory();
            BeginCategory("Feet", ref feetOn);
            DrawSymmetricToggle(SoleName, ref soleOn, ref soleLeftOn, ref soleRightOn);
            footjobOn = EditorGUILayout.ToggleLeft(FootjobName, footjobOn);
            EndCategory();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            string note = null;
            if (selectedAvatar != null)
            {
                using (var armature = new AvatarArmature(selectedAvatar.gameObject))
                {
                    var hips = armature.FindBoneOrNull(HumanBodyBones.Hips);
                    if (hips != null)
                    {
                        var sockets = GetPussyAndAnal(hips);
                        if (sockets.Item1 != null)
                        {
                            if (HasSFX(hips))
                            {
                                var sfxVer = GetSFXVersion(sockets.Item1);
                                if (sfxVer == null || (sfxVer != null && sfxVer < CurrentVersion))
                                {
                                    note = "Outdated SFX. Reapply to update!";
                                }   
                            }
                        }
                        if (sockets.Item2 != null)
                        {
                            if (HasSFX(hips))
                            {
                                var sfxVer = GetSFXVersion(sockets.Item2);
                                if (sfxVer == null || (sfxVer != null && sfxVer < CurrentVersion))
                                {
                                    note = "Outdated SFX. Reapply to update!";
                                }   
                            }
                        }
                    }
                }
            }
            BeginCategory("Sound FX (Beta)", ref sfxOn, note);
            EditorGUILayout.BeginHorizontal();
            //sfxBlowjobOn = EditorGUILayout.ToggleLeft(BlowjobName, sfxBlowjobOn, GUILayout.Width(94));
            sfxPussyOn = EditorGUILayout.ToggleLeft(PussyName, sfxPussyOn, GUILayout.Width(94));
            sfxAnalOn = EditorGUILayout.ToggleLeft(AnalName, sfxAnalOn, GUILayout.Width(94));
            EditorGUILayout.EndHorizontal();
            EndCategory();
            DrawParameterEstimation(selectedAvatar);
            /*
            if ((experimentalFoldout = BeginExperimental(experimentalFoldout)))
            {
                EditorGUILayout.ToggleLeft("Sound FX", false);
            }
            EndExperimental();
            */
            EditorGUILayout.Space(16);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(selectedAvatar == null))
                {
                    if (GUILayout.Button("Apply", GUILayout.Width(128), GUILayout.Height(32)))
                    {
                        try
                        {
                            apply();
                        }
                        catch (Exception e)
                        {
                            EditorUtility.DisplayDialog("Error",
                                $"An error occured: {e.Message}\n\nCheck the Unity console for further information.\nIt is most likely a bug. Please report the issue on my Discord.",
                                "Ok");
                            throw;
                        }
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(128), GUILayout.Height(32)))
                    {
                        Clear(selectedAvatar.gameObject);
                    }
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            var linkStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(16, 16, 8, 8)
            };
            using (new EditorGUI.DisabledScope(selectedAvatar == null))
            {
                if (GUILayout.Button("Add test penetrator to Avatar", linkStyle, GUILayout.ExpandWidth(false)))
                {
                    try
                    {
                        var testPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                            "Packages/wholesomevr.sps-configurator/Assets/Actual Wholesome Lollipop.prefab");
                        var initiatedPrefab =
                            PrefabUtility.InstantiatePrefab(testPrefab, selectedAvatar.transform) as GameObject;
                        var animator = selectedAvatar.gameObject.GetComponent<Animator>();
                        Debug.Assert(animator != null, "No animator on the avatar");
                        var unityAvatar = animator.avatar;
                        var avatarMeshes = selectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                            .Where(mesh => mesh.transform.parent == selectedAvatar.gameObject.transform)
                            .ToArray();
                        var armature = new AvatarArmature(selectedAvatar.gameObject);
                        if (SelectedAvatar.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                        {
                            var visemOhBlendshapeName =
                                SelectedAvatar.VisemeBlendShapes[(int)VRC_AvatarDescriptor.Viseme.oh];
                            var mouthPosition = DetectMouthPosition(SelectedAvatar.VisemeSkinnedMesh,
                                SelectedAvatar.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(
                                    visemOhBlendshapeName), armature.FindBone(HumanBodyBones.Head));
                            var position = mouthPosition + new Vector3(0, 0, 0.3f);
                            initiatedPrefab.transform.SetPositionAndRotation(position,
                                Quaternion.AngleAxis(-180, Vector3.left));
                        }
                        else
                        {
                            var mouthPosition = armature.FindBone(HumanBodyBones.Head)
                                .TransformPoint(new Vector3(0, 0.01f, 0.075f));
                            var position = mouthPosition + new Vector3(0, 0, 0.3f);
                            initiatedPrefab.transform.SetPositionAndRotation(position,
                                Quaternion.AngleAxis(-180, Vector3.left));
                        }

                        armature.Dispose();
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error",
                            $"An error occured: {e.Message}\n\nCheck the Unity console for further information.\nIt is most likely a bug. Please report the issue on my Discord.",
                            "Ok");
                        throw;
                    }
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Discord", linkStyle, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("https://discord.gg/Rtp3wvJu8s");
            }

            if (GUILayout.Button("Gumroad", linkStyle, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("https://wholesomevr.gumroad.com/?referrer=SPS");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void DrawLabels()
        {
            EditorGUILayout.BeginHorizontal();
            var toggleLabelStyle = new GUIStyle(GUI.skin.label);
            toggleLabelStyle.margin = new RectOffset(2, 4, 0, 8);
            toggleLabelStyle.normal.textColor = Color.gray;
            GUILayout.Label("Toggle", toggleLabelStyle);
            GUILayout.FlexibleSpace();
            var blendshapeLabelStyle = new GUIStyle(GUI.skin.label);
            blendshapeLabelStyle.margin = new RectOffset(4, 16, 0, 8);
            blendshapeLabelStyle.normal.textColor = Color.gray;
            GUILayout.Label("Blendshape/Symmetric Toggles", blendshapeLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void BeginCategory(string categoryName, ref bool on, string note = null)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = style.normal.scaledBackgrounds[0];
            EditorGUILayout.BeginHorizontal(style);
            on = EditorGUILayout.ToggleLeft(categoryName, on, EditorStyles.boldLabel);
            if (note != null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(note);
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = on;
            var togglesStyle = new GUIStyle(GUI.skin.label);
            togglesStyle.padding = new RectOffset(8, 8, 8, 8);
            EditorGUILayout.BeginHorizontal(togglesStyle);
            EditorGUILayout.BeginVertical();
        }

        private void EndCategory()
        {
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical(); // box
        }

        private bool BeginExperimental(bool foldout)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = style.normal.scaledBackgrounds[0];
            EditorGUILayout.BeginVertical(style);
            foldout = EditorGUILayout.Foldout(foldout, "Experimental");
            EditorGUILayout.EndVertical();
            var togglesStyle = new GUIStyle(GUI.skin.label);
            togglesStyle.padding = new RectOffset(8, 8, 8, 8);
            EditorGUILayout.BeginHorizontal(togglesStyle);
            EditorGUILayout.BeginVertical();
            return foldout;
        }

        private void EndExperimental()
        {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical(); // box
        }


        private const int GENERAL_SPS_COST = 1;
        private const int SINGLE_SPS_COST = 1;
        private const int SFX_SPS_COST = 1;

        private void DrawParameterEstimation(VRCAvatarDescriptor avatar)
        {
            var avatarCost = avatar?.expressionParameters?.CalcTotalCost() ?? 0;
            var spsCost = GENERAL_SPS_COST;
            if (sfxOn && (sfxPussyOn || sfxAnalOn))
            {
                spsCost += SFX_SPS_COST;
            }

            if (defaultOn)
            {
                if (blowjobOn)
                {
                    spsCost += SINGLE_SPS_COST;
                }

                if (handjobOn)
                {
                    if (handjobLeftOn)
                    {
                        spsCost += SINGLE_SPS_COST;
                    }

                    if (handjobRightOn)
                    {
                        spsCost += SINGLE_SPS_COST;
                    }

                    if (handjobBothOn)
                    {
                        spsCost += SINGLE_SPS_COST;
                    }
                }

                if (pussyOn)
                {
                    spsCost += SINGLE_SPS_COST;
                }

                if (analOn)
                {
                    spsCost += SINGLE_SPS_COST;
                }
            }

            if (specialOn)
            {
                if (titjobOn)
                {
                    spsCost += SINGLE_SPS_COST;
                }

                if (assjobOn)
                {
                    spsCost += SINGLE_SPS_COST;
                }

                if (thighjobOn)
                {
                    spsCost += SINGLE_SPS_COST;
                }
            }

            if (feetOn)
            {
                if (soleOn)
                {
                    if (soleLeftOn)
                    {
                        spsCost += SINGLE_SPS_COST;
                    }

                    if (soleRightOn)
                    {
                        spsCost += SINGLE_SPS_COST;
                    }
                }

                if (footjobOn)
                {
                    spsCost += SINGLE_SPS_COST;
                }
            }

            var resultCost = avatarCost + spsCost;
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = style.normal.scaledBackgrounds[0];
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var labelStyle = new GUIStyle(EditorStyles.boldLabel) { margin = new RectOffset(8, 8, 8, 8) };
            GUILayout.Label("Paramaters", labelStyle);
            var parametersStyle = new GUIStyle(GUI.skin.box)
                { margin = new RectOffset(8, 8, 8, 8), padding = new RectOffset(8, 8, 8, 8) };
            EditorGUILayout.BeginVertical(parametersStyle);
            EditorGUILayout.LabelField("Avatar:", avatarCost.ToString());
            EditorGUILayout.LabelField("SPS:", spsCost.ToString());
            var totalStyle = new GUIStyle(GUI.skin.label) { richText = true };
            var color = resultCost > 256 ? "red" : "lime";
            EditorGUILayout.LabelField("Total Memory:", $"<color={color}>{resultCost}</color>/256", totalStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }
#pragma warning restore 0219
#pragma warning restore 0414
    }
}