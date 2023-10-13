using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using VF.Component;
using VF.Inspector;
using VF.Model;
using VF.Model.Feature;
using VF.Model.StateAction;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Action = VF.Model.StateAction.Action;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Wholesome
{
    public class SPSConfigurator : EditorWindow
    {
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
        private bool sfxOn = false;
        private bool sfxPussyOn = true;
        private bool sfxAnalOn = true;

        [MenuItem("Tools/Wholesome/SPS Configurator")]
        public static void Open()
        {
            var window = GetWindow(typeof(SPSConfigurator));
            window.titleContent = new GUIContent("SPS Configurator");
            window.minSize = new Vector2(490, 790);
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


        private void OnEnable()
        {
            logo = Resources.Load<Texture2D>("SPS Logo");
            categoryLabelBackground = new Texture2D(1, 1);
            categoryLabelBackground.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
            categoryLabelBackground.Apply();
            meshes = SelectedAvatar?.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        public void OnSelectionChange()
        {
            meshes = SelectedAvatar?.GetComponentsInChildren<SkinnedMeshRenderer>();
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
            var animator = avatarGameObject.GetComponent<Animator>();
            Debug.Assert(animator != null, "No animator on the avatar");
            var unityAvatar = animator.avatar;
            Debug.Assert(unityAvatar != null, "No Unity Avatar on the avatar");
            var meshes = vrcAvatar.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Where(mesh => mesh.transform.parent == avatarGameObject.transform)
                .ToArray();
            var humanToTransform = BuildSkeleton(meshes, unityAvatar.humanDescription.human);
            var armature = humanToTransform["Hips"].parent; // TODO: Missing key issue
            Debug.Assert(armature.parent == avatarGameObject.transform,
                "Armature is doesn't share parent with mesh");
            Clear(avatarGameObject);
            // TODO: Handle no visemes

            var armatureScale = armature.localScale;
            var inverseArmatureScale = new Vector3(1 / armatureScale.x, 1 / armatureScale.y,
                1 / armatureScale.z);
            var avatarScale = vrcAvatar.transform.localScale;
            var inverseAvatarScale = new Vector3(1 / avatarScale.x, 1 / avatarScale.y,
                1 / avatarScale.z);
            var hipLength = Vector3.Scale(humanToTransform["Spine"].position - humanToTransform["Hips"].position,
                inverseAvatarScale).magnitude;

            // var hipLength = Vector3.Scale(humanToTransform["Spine"].localPosition, armatureScale).magnitude;
            var bakedScale = hipLength / @base.DefaultHipLength;
            if (selectedBase == 0) // Generic
            {
                var torsoLength = humanToTransform["Hips"].InverseTransformPoint(humanToTransform["Neck"].position)
                    .magnitude;
                bakedScale = torsoLength / @base.DefaultTorsoLength;
            }

            var icons = new List<SetIcon>();
            var createdSockets = new List<VRCFuryHapticSocket>();
            if (defaultOn)
            {
                if (blowjobOn)
                {
                    var mouthPosition = new Vector3(0, 0.01f, 0.075f);
                    if (vrcAvatar.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                    {
                        var visemOhBlendshapeName = vrcAvatar.VisemeBlendShapes[(int)VRC_AvatarDescriptor.Viseme.oh];
                        if (!string.IsNullOrEmpty(visemOhBlendshapeName))
                        {
                            mouthPosition = humanToTransform["Head"].InverseTransformPoint(DetectMouthPosition(
                                vrcAvatar.VisemeSkinnedMesh,
                                vrcAvatar.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(
                                    visemOhBlendshapeName), humanToTransform["Head"]));
                        }
                    }

                    var socket = CreateSocket(BlowjobName, VRCFuryHapticSocket.AddLight.Hole, true);
                    SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["Head"],
                        mouthPosition,
                        Vector3.zero);
                    AddBlendshape(socket, blowjobBlendshape.ToString());
                    createdSockets.Add(socket);
                    icons.Add(new SetIcon()
                    {
                        path = $"Sockets/{socket.name}"
                    });
                }

                if (handjobOn)
                {
                    var leftAlignDelta = GetAlignDelta(humanToTransform["LeftHand"]);
                    var rightAlignDelta = GetAlignDelta(humanToTransform["RightHand"]);
                    if (handjobLeftOn)
                    {
                        var socket = CreateSocket($"{HandjobName} Left", VRCFuryHapticSocket.AddLight.Ring, true,
                            "Handjob");
                        SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["LeftHand"],
                            Vector3.Scale(@base.Hand.Positon, inverseArmatureScale) * bakedScale,
                            Vector3.Scale(@base.Hand.EulerAngles, new Vector3(1, -1, 1)) + leftAlignDelta);
                        createdSockets.Add(socket);
                        icons.Add(new SetIcon()
                        {
                            path = $"Sockets/{socket.name}"
                        });
                    }

                    if (handjobRightOn)
                    {
                        var socket = CreateSocket($"{HandjobName} Right", VRCFuryHapticSocket.AddLight.Ring, true,
                            "Handjob");
                        SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["RightHand"],
                            Vector3.Scale(@base.Hand.Positon, inverseArmatureScale) * bakedScale,
                            @base.Hand.EulerAngles + rightAlignDelta);
                        createdSockets.Add(socket);
                        icons.Add(new SetIcon()
                        {
                            path = $"Sockets/{socket.name}"
                        });
                    }

                    if (handjobBothOn)
                    {
                        var socket = CreateSocket($"Double {HandjobName}", VRCFuryHapticSocket.AddLight.Ring, false,
                            "Handjob");
                        socket.transform.SetParent(humanToTransform["Hips"], false);
                        SetSymmetricParent2(socket.gameObject, humanToTransform["LeftHand"],
                            humanToTransform["RightHand"],
                            Vector3.Scale(@base.Hand.Positon, avatarScale) * bakedScale, // World Scale
                            Vector3.Scale(@base.Hand.EulerAngles, new Vector3(1, -1, 1)) + leftAlignDelta,
                            Vector3.Scale(@base.Hand.Positon, avatarScale) * bakedScale, // World Scale
                            @base.Hand.EulerAngles + rightAlignDelta);
                        createdSockets.Add(socket);
                        icons.Add(new SetIcon()
                        {
                            path = $"Sockets/{socket.name}"
                        });
                    }
                }

                if (pussyOn)
                {
                    var socket = CreateSocket(PussyName, VRCFuryHapticSocket.AddLight.Hole, true);
                    SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["Hips"],
                        Vector3.Scale(@base.Pussy.Positon, inverseArmatureScale) * bakedScale,
                        @base.Pussy.EulerAngles);
                    AddBlendshape(socket, pussyBlendshape.ToString());
                    createdSockets.Add(socket);
                    icons.Add(new SetIcon()
                    {
                        path = $"Sockets/{socket.name}"
                    });
                    if (sfxOn && sfxPussyOn)
                    {
                        var sfx = PrefabUtility.InstantiatePrefab(
                            AssetDatabase.LoadAssetAtPath<GameObject>(
                                "Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab"),
                            socket.transform) as GameObject;
                        socket.enableActiveAnimation = true;
                        socket.activeActions = new State();
                        socket.activeActions.actions.Add(new ObjectToggleAction
                        {
                            obj = sfx
                        });
                    }
                }

                if (analOn)
                {
                    var socket = CreateSocket(AnalName, VRCFuryHapticSocket.AddLight.Hole, false);
                    SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["Hips"],
                        Vector3.Scale(@base.Anal.Positon, inverseArmatureScale) * bakedScale,
                        @base.Anal.EulerAngles);
                    AddBlendshape(socket, analBlendshape.ToString());
                    createdSockets.Add(socket);
                    icons.Add(new SetIcon()
                    {
                        path = $"Sockets/{socket.name}"
                    });
                    if (sfxOn && sfxAnalOn)
                    {
                        var sfx = PrefabUtility.InstantiatePrefab(
                            AssetDatabase.LoadAssetAtPath<GameObject>(
                                "Packages/wholesomevr.sps-configurator/Assets/SFX/SFX.prefab"),
                            socket.transform) as GameObject;
                        socket.enableActiveAnimation = true;
                        socket.activeActions = new State();
                        socket.activeActions.actions.Add(new ObjectToggleAction
                        {
                            obj = sfx
                        });
                    }
                }
            }

            if (specialOn)
            {
                if (titjobOn)
                {
                    var socket = CreateSocket(TitjobName, VRCFuryHapticSocket.AddLight.Ring, false, "Special");
                    SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["Chest"],
                        Vector3.Scale(@base.Titjob.Positon, inverseArmatureScale) * bakedScale,
                        @base.Titjob.EulerAngles);
                    createdSockets.Add(socket);
                    icons.Add(new SetIcon()
                    {
                        path = $"Sockets/{socket.name}"
                    });
                }

                if (assjobOn)
                {
                    var socket = CreateSocket(AssjobName, VRCFuryHapticSocket.AddLight.Ring, false, "Special");
                    SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["Hips"],
                        Vector3.Scale(@base.Assjob.Positon, inverseArmatureScale) * bakedScale,
                        @base.Assjob.EulerAngles);
                    createdSockets.Add(socket);
                    icons.Add(new SetIcon()
                    {
                        path = $"Sockets/{socket.name}"
                    });
                }

                if (thighjobOn)
                {
                    var socket = CreateSocket(ThighjobName, VRCFuryHapticSocket.AddLight.Ring, false, "Special");
                    socket.transform.SetParent(humanToTransform["Hips"], false);
                    SetSymmetricParent(socket.gameObject, humanToTransform["LeftUpperLeg"],
                        humanToTransform["RightUpperLeg"],
                        Vector3.Scale(@base.Thighjob.Positon, avatarScale) * bakedScale, // World Scale
                        @base.Thighjob.EulerAngles);
                    createdSockets.Add(socket);
                    icons.Add(new SetIcon()
                    {
                        path = $"Sockets/{socket.name}"
                    });
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
                        var socket = CreateSocket($"{SoleName} Left", VRCFuryHapticSocket.AddLight.Ring, true, "Feet");
                        SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["LeftFoot"],
                            Vector3.Scale(solePosition, inverseArmatureScale) * bakedScale,
                            soleRotation);
                        createdSockets.Add(socket);
                        icons.Add(new SetIcon()
                        {
                            path = $"Sockets/{socket.name}"
                        });
                    }

                    if (soleRightOn)
                    {
                        var socket = CreateSocket($"{SoleName} Right", VRCFuryHapticSocket.AddLight.Ring, true, "Feet");
                        SetParentLocalPositionEulerAngles(socket.transform, humanToTransform["RightFoot"],
                            Vector3.Scale(solePosition, inverseArmatureScale) * bakedScale,
                            soleRotation);
                        createdSockets.Add(socket);
                        icons.Add(new SetIcon()
                        {
                            path = $"Sockets/{socket.name}"
                        });
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

                    var socket = CreateSocket($"{FootjobName}", VRCFuryHapticSocket.AddLight.Ring, false, "Feet");
                    socket.transform.SetParent(humanToTransform["Hips"], false);
                    SetSymmetricParent(socket.gameObject, humanToTransform["LeftFoot"], humanToTransform["RightFoot"],
                        Vector3.Scale(footjobPosition, avatarScale) * bakedScale, footjobRotation); // World Scale
                    createdSockets.Add(socket);
                    icons.Add(new SetIcon()
                    {
                        path = $"Sockets/{socket.name}"
                    });
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
                    name = $"{spsMenuPath}/Options/Sound FX",
                    saved = true,
                    defaultOn = true,
                    useGlobalParam = true,
                    globalParam = "WH_SFX_On"
                });
            }
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
                    vrcFury.config.features.RemoveAll(feature =>
                        feature is Toggle t && t.name == $"{spsPath}/Options/Sound FX");
                }

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
                        meshes = selectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
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
            BeginCategory("Sound FX (Experimental)", ref sfxOn);
            EditorGUILayout.BeginHorizontal();
            sfxPussyOn = EditorGUILayout.ToggleLeft(PussyName, sfxPussyOn);
            sfxAnalOn = EditorGUILayout.ToggleLeft(AnalName, sfxAnalOn);
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
                        var avatarMeshes = selectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>()
                            .Where(mesh => mesh.transform.parent == selectedAvatar.gameObject.transform)
                            .ToArray();
                        var humanToTransform = BuildSkeleton(avatarMeshes, unityAvatar.humanDescription.human);
                        if (SelectedAvatar.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                        {
                            var visemOhBlendshapeName =
                                SelectedAvatar.VisemeBlendShapes[(int)VRC_AvatarDescriptor.Viseme.oh];
                            var mouthPosition = DetectMouthPosition(SelectedAvatar.VisemeSkinnedMesh,
                                SelectedAvatar.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(
                                    visemOhBlendshapeName), humanToTransform["Head"]);
                            var position = mouthPosition + new Vector3(0, 0, 0.3f);
                            initiatedPrefab.transform.SetPositionAndRotation(position,
                                Quaternion.AngleAxis(-180, Vector3.left));
                        }
                        else
                        {
                            var mouthPosition = humanToTransform["Head"].transform
                                .TransformPoint(new Vector3(0, 0.01f, 0.075f));
                            var position = mouthPosition + new Vector3(0, 0, 0.3f);
                            initiatedPrefab.transform.SetPositionAndRotation(position,
                                Quaternion.AngleAxis(-180, Vector3.left));
                        }
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

        private void BeginCategory(string categoryName, ref bool on)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = style.normal.scaledBackgrounds[0];
            EditorGUILayout.BeginHorizontal(style);
            on = EditorGUILayout.ToggleLeft(categoryName, on, EditorStyles.boldLabel);
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

        private void DrawParameterEstimation(VRCAvatarDescriptor avatar)
        {
            var avatarCost = avatar?.expressionParameters?.CalcTotalCost() ?? 0;
            var spsCost = GENERAL_SPS_COST;
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
    }
}