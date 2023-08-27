using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using VF.Component;
using VF.Model;
using VF.Model.Feature;
using VF.Model.StateAction;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace Wholesome
{
    public class SPSConfigurator : EditorWindow
    {
        private int selectedBase = 0;
        private Base.FootType selectedFootType = Base.FootType.Flat;
        private Texture2D categoryLabelBackground;

        private Texture2D logo;

        private Dictionary<Base.Category, bool> categoryToggles;
        private Dictionary<string, Toggle> toggles;

        private string[] autoOn = {
            "Blowjob", "Handjob Right", "Handjob Left", "Pussy", "Steppies Right", "Steppies Left"
        };

        private enum Mode
        {
            Simple = 0,
            Advanced = 1
        }


        [MenuItem("Tools/Wholesome/SPS Configurator")]
        public static void Open()
        {
            var window = GetWindow(typeof(SPSConfigurator));
            window.titleContent = new GUIContent("SPS Configurator");
            window.minSize = new Vector2(490, 530);
            window.Show();
        }

        public static Vector3 DetectMouthPosition(SkinnedMeshRenderer head, int ohBlendshapeIndex)
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
            //weightedPos = new Vector3(0, weightedPos.z, -weightedPos.y); // TODO: Handle all possible head transformations
            return weightedPos;
        }
        

        private void OnEnable()
        {

            var enums = Enum.GetValues(typeof(Wholesome.Base.Category)) as Wholesome.Base.Category[];
            categoryToggles = enums.ToDictionary(category => category, category => true);
            toggles = new Dictionary<string, Toggle>
            {
                {"Mouth", new BlendshapeToggle("Blowjob")
                {
                    SelectedBlendshape = "vrc.v_oh"
                }}
            };
            foreach (var socket in Base.SocketInfos)
            {
                Toggle toggle;
                if (socket.Blendshape)
                {
                    toggle = new BlendshapeToggle(socket.DisplayName);
                }

                else if (socket.Symmetric)
                {
                    toggle = new SymmetricToggle(socket.DisplayName, socket.Both);
                }
                else
                {
                    toggle = new Toggle(socket.DisplayName);
                }
                toggles.Add(socket.Name, toggle);
                if (socket.Both)
                {
                    toggles.Add(socket.BothName, new Toggle(socket.BothName));
                }
            }
            logo = Resources.Load<Texture2D>("SPS Logo");
            categoryLabelBackground = new Texture2D(1, 1);
            categoryLabelBackground.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
            categoryLabelBackground.Apply();
            //GuessBase();
        }

        private void GuessBase()
        {
            var selected = SelectedAvatar;
            if (selected != null)
            {
                var meshes = selected.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var blendshapes = meshes.SelectMany(mesh => Enumerable.Range(0, mesh.sharedMesh.blendShapeCount)
                    .Select(j => mesh.sharedMesh.GetBlendShapeName(j))).ToImmutableHashSet();
                for (var i = 0; i < Bases.All.Length; i++)
                {
                    var baseBlendshapes = new HashSet<string>(Bases.All[i].Blendshapes);
                    if (baseBlendshapes.Count > 0)
                    {
                        baseBlendshapes.IntersectWith(blendshapes);
                        if (Bases.All[i].Blendshapes.Count() == baseBlendshapes.Count)
                        {
                            selectedBase = i;
                            break;
                        }
                    }
                }
            }
        }

        public void OnSelectionChange()
        {
            //GuessBase();
            Repaint();
        }

        public static (int, int) GuessBodyMesh(SkinnedMeshRenderer[] meshes)
        {
            for (var i = 0; i < Bases.All.Length; i++)
            {
                var blendshapes = Bases.All[i].Blendshapes;
                var classifiedMeshIndices = meshes
                    .Select((mesh, j) => (mesh, j))
                    .Where(meshI =>
                    {
                        var mesh = meshI.mesh;
                        var blendshapeNames = Enumerable.Range(0, mesh.sharedMesh.blendShapeCount)
                            .Select(j => mesh.sharedMesh.GetBlendShapeName(j))
                            .ToImmutableHashSet();
                        var intersection = blendshapeNames.Intersect(blendshapes);
                        return intersection.Count == blendshapes.Count();
                    })
                    .Select(meshI => meshI.j).ToArray();
                if (classifiedMeshIndices.Length == 1)
                {
                    return (classifiedMeshIndices.First(), i);
                }
                else
                {
                    Debug.LogWarning($"Matches: {classifiedMeshIndices.Length}");
                }
            }

            return (-1, -1);
        }

        public void SetBlendshapes()
        {
            var @base = Bases.All[selectedBase];
            toggles["Pussy"].SelectedBlendshape = @base.PussyBlendshape;
            toggles["Anal"].SelectedBlendshape = @base.AnalBlendshape;
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
                        selectedAvatar = gameObject.GetComponentsInParent<VRCAvatarDescriptor>(true).FirstOrDefault();
                    }
                }
                return selectedAvatar;
            }
        }

        public static Dictionary<string, Transform> BuildSkeleton(SkinnedMeshRenderer[] meshes, HumanBone[] bones)
        {
            var boneToHuman = bones.ToDictionary(bone => bone.boneName, bone => bone.humanName);
            var humanToTransform = new Dictionary<string, Transform>();
            var transforms = new HashSet<Transform>(meshes.SelectMany(mesh => mesh.bones));
            foreach (var bone in transforms)
            {
                if (boneToHuman.TryGetValue(bone.name, out var humanBoneName))
                {
                    humanToTransform[humanBoneName] = bone;
                }
            }

            return humanToTransform;
        }

        // TODO: Get Socket names from Marker Component to clear VRCFury component
        public static void Clear(GameObject avatarGameObject)
        {
            var socketNames = Base.SocketInfos.SelectMany(info =>
            {
                if (info.Symmetric)
                {
                    if (info.Both)
                    {
                        
                        return new[]
                        {
                            $"{info.DisplayName} Left", $"{info.DisplayName} Right", $"Double {info.DisplayName}",
                        };
                    }
                    else
                    {
                        return new[]
                        {
                            $"{info.DisplayName} Left", $"{info.DisplayName} Right",
                        };
                    }
                }
                else
                {
                    return new[]
                    {
                        $"{info.DisplayName}",
                    };
                }
            }).ToList();
            socketNames.Add("Blowjob");
            var sockets = avatarGameObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);
            
            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    var parent = socket.transform.parent;
                    if (parent.name == "SPS" && socketNames.Contains(socket.name))
                    {
                        Object.DestroyImmediate(socket.gameObject);
                        if (parent.childCount == 0)
                        {
                            Object.DestroyImmediate(parent.gameObject);
                        }
                    }
                }
            }

            var furies = avatarGameObject.GetComponents<VRCFury>();
            var hasMenuMove = false;
            var possiblePaths = socketNames.Select(name => $"Sockets/{name}").ToList();
            var possibleIcons = Wholesome.Base.Categories.Select(category => $"Sockets/{category}").ToArray();
            foreach (var vrcFury in furies)
            {
                vrcFury.config.features.RemoveAll(feature => feature is MoveMenuItem m && m.fromPath == "Sockets");
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                vrcFury.config.features.RemoveAll(feature => feature is SetIcon i && possibleIcons.Contains(i.path));
                if (vrcFury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(vrcFury);
                }
            }
        }

        private class Toggle
        {
            public bool On = true;
            public bool Left = true;
            public bool Right = true;
            public bool Both = true;
            public string SelectedBlendshape = null;
            protected string Name;

            public Toggle(string name)
            {
                Name = name;
            }

            public virtual void Draw(SkinnedMeshRenderer[] meshes)
            {
                On = EditorGUILayout.ToggleLeft(Name, On);
            }
        }

        private class SymmetricToggle : Toggle
        {
            private bool showBoth;

            public SymmetricToggle(string name, bool showBoth = false) : base(name)
            {
                this.showBoth = showBoth;
            }

            public override void Draw(SkinnedMeshRenderer[] meshes)
            {
                using (new GUILayout.HorizontalScope())
                {
                    On = EditorGUILayout.ToggleLeft(Name, On, GUILayout.Width(64 + 16));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    using (new EditorGUI.DisabledScope(!On))
                    {
                        Left = EditorGUILayout.ToggleLeft("Left", Left, GUILayout.Width(64));
                        Right = EditorGUILayout.ToggleLeft("Right", Right, GUILayout.Width(64));
                        if (showBoth)
                        {
                            Both = EditorGUILayout.ToggleLeft("Double", Both, GUILayout.Width(64));
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private class BlendshapeToggle : Toggle
        {
            public BlendshapeToggle(string name) : base(name)
            {
            }

            public override void Draw(SkinnedMeshRenderer[] meshes)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    On = EditorGUILayout.ToggleLeft(Name, On, GUILayout.ExpandWidth(false));
                    GUILayout.FlexibleSpace();


                    using (new EditorGUI.DisabledScope(!On || meshes == null))
                    {
                        if (EditorGUILayout.DropdownButton(new GUIContent(SelectedBlendshape ?? "None"), FocusType.Keyboard, GUILayout.Width(64*3 + 8)))
                        {
                            var menu = new GenericMenu();
                            menu.AddItem(new GUIContent($"None"),
                                SelectedBlendshape == null, () => { SelectedBlendshape = null; });
                            for (int i = 0; i < meshes.Length; i++)
                            {
                                for (int j = 0; j < meshes[i].sharedMesh.blendShapeCount; j++)
                                {
                                    var blendshapeName = meshes[i].sharedMesh.GetBlendShapeName(j);
                                    var menuEntry = $"{meshes[i].name}/{blendshapeName}";
                                    menu.AddItem(new GUIContent(menuEntry),
                                        SelectedBlendshape == blendshapeName,
                                        blendshapeNameObject => { SelectedBlendshape = blendshapeNameObject as string; }, blendshapeName);
                                }
                            }
                            menu.ShowAsContext();
                        }
                    }
                }
            }
        }

        // TODO: Check for optional bone "Chest"


        public struct SocketCreate
        {
            public string Name;
            public VRCFuryHapticSocket.AddLight AddLight;
            public Vector3 Position;
            public Vector3 EulerAngles;
        }

        public static void CreateSocket(SocketCreate socketCreate)
        {
        }

        public VRCFuryHapticSocket CreateSocket(Base.Socket socket, string name, string boneName,
            Dictionary<string, Transform> armature,
            Vector3 inverseArmatureScale, List<MoveMenuItem> menuMoves, float bakedScale, bool alignBone = false)
        {
            var gameObject = new GameObject(name);
            var socketVrcf = gameObject.AddComponent<VRCFuryHapticSocket>();
            socketVrcf.Version = 7;
            switch (socket.Info.Type)
            {
                case Base.SocketType.Hole:
                    socketVrcf.addLight = VRCFuryHapticSocket.AddLight.Hole;
                    break;
                case Base.SocketType.Ring:
                    socketVrcf.addLight = VRCFuryHapticSocket.AddLight.Ring;
                    break;
            }

            socketVrcf.name = name;
            socketVrcf.enableAuto = autoOn.Contains(name);
            var boneTransform = armature[boneName];
            var sps = boneTransform.Find("SPS")?.gameObject;
            if (sps == null)
            {
                sps = new GameObject("SPS");
                sps.transform.SetParent(boneTransform, false);
            }
            sps.transform.localPosition = Vector3.zero;
            sps.transform.localEulerAngles = Vector3.zero;

            if (alignBone)
            {
                var alignedX = Vector3.Cross(sps.transform.up, Vector3.up);
                var sign = Mathf.Sign(Vector3.Dot(sps.transform.up, Vector3.right));
                sps.transform.localEulerAngles =
                    new Vector3(0, sign * Vector3.Angle(sps.transform.right, alignedX), 0);
                gameObject.transform.SetParent(sps.transform, false);
                gameObject.transform.localPosition =
                    Vector3.Scale(socket.Location.Positon, inverseArmatureScale) * bakedScale;
                gameObject.transform.localEulerAngles = sign * socket.Location.EulerAngles;
            }
            else
            {
                gameObject.transform.SetParent(sps.transform, false);
                gameObject.transform.localPosition =
                    Vector3.Scale(socket.Location.Positon, inverseArmatureScale) * bakedScale;
                gameObject.transform.localEulerAngles = socket.Location.EulerAngles;
            }

            if (socket.Info.Category != Base.Category.Default)
            {
                menuMoves.Add(new MoveMenuItem
                {
                    fromPath = $"Sockets/{socketVrcf.name}",
                    toPath = $"Sockets/{socket.Info.Category.ToString()}/{socketVrcf.name}"
                });
            }

            return socketVrcf;

            /*
            var vrcFurySetIcon = gameObject.AddComponent<VRCFury>();
            vrcFurySetIcon.config.features.Add(new SetIcon
            {
                path = $"Sockets/{socketVrcf.name}",
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/com.vrchat.avatars/Samples/AV3 Demo Assets/Expressions Menu/Icons/hand_normal.png")
            });
            switch (socket.Info.Category)
            {
                case Base.Category.Special:
                    menuMoves.Add(new MoveMenuItem
                    {
                        fromPath = $"Sockets/{socketVrcf.name}",
                        toPath = $"Sockets/Special/{socketVrcf.name}"
                    });
                    break;
                case Base.Category.Feet:
                    menuMoves.Add(new MoveMenuItem
                    {
                        fromPath = $"Sockets/{socketVrcf.name}",
                        toPath = $"Sockets/Feet/{socketVrcf.name}"
                    });
                    break;
            }
            */
        }

        public static void AddBlendshape(VRCFuryHapticSocket hapticSocket, SkinnedMeshRenderer mesh, string blendshape)
        {
            var state = new State();
            state.actions.Add(new BlendShapeAction
            {
                blendShape = blendshape
            });
            hapticSocket.depthActions.Add(new VRCFuryHapticSocket.DepthAction
            {
                state = state,
                enableSelf = true,
                startDistance = 0.05f,
                endDistance = 0,
                smoothingSeconds = 0,
            });
        }

        private bool HasBlendshapes(VRCAvatarDescriptor avatar, string blendshape)
        {
            var meshes = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var blendshapes = meshes.SelectMany(mesh => Enumerable.Range(0, mesh.sharedMesh.blendShapeCount)
                .Select(j => mesh.sharedMesh.GetBlendShapeName(j))).ToList();
            return blendshapes.Contains(blendshape);
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
            var armature = humanToTransform["Hips"].parent;
            Debug.Assert(armature.parent == avatarGameObject.transform,
                "Armature is doesn't share parent with mesh");
            Clear(avatarGameObject);
            // TODO: Handle no visemes


            var armatureScale = armature.localScale;
            var inverseArmatureScale = new Vector3(1 / armatureScale.x, 1 / armatureScale.y,
                1 / armatureScale.z);
            var hipLength = (humanToTransform["Spine"].position - humanToTransform["Hips"].position).magnitude;
            var bakedScale = hipLength/@base.DefaultHipLength;
            var menuMoves = new List<MoveMenuItem>();
            foreach (var socket in @base.GetSocketsForFootType(selectedFootType))
            {
                if (!toggles[socket.Info.Name].On || !categoryToggles[socket.Info.Category]) // Skip socket when in advanced mode and toggled off
                {
                    continue;
                }

                if (socket.Info.Symmetric)
                {
                    var align = socket.Info.Bone == Base.Bone.Hand;
                    var socketVrcfLeft = CreateSocket(socket, $"{socket.Info.DisplayName} Left",
                        $"Left{socket.Info.Bone.ToString()}", humanToTransform, inverseArmatureScale, menuMoves, bakedScale, align);
                    var socketVrcfRight = CreateSocket(socket, $"{socket.Info.DisplayName} Right",
                        $"Right{socket.Info.Bone.ToString()}", humanToTransform, inverseArmatureScale, menuMoves, bakedScale, align);
                    if (socket.Info.Both)
                    {
                        var socketVrcf = CreateSocket(socket, $"Double {socket.Info.DisplayName}", "Hips",
                            humanToTransform, inverseArmatureScale, menuMoves, bakedScale);
                        var parent = socketVrcf.gameObject.AddComponent<ParentConstraint>();
                        parent.AddSource(new ConstraintSource
                        {
                            sourceTransform = socketVrcfLeft.transform,
                            weight = 1
                        });
                        parent.AddSource(new ConstraintSource
                        {
                            sourceTransform = socketVrcfRight.transform,
                            weight = 1
                        });
                        parent.locked = true;
                        parent.constraintActive = true;
                    }
                }
                else if (socket.Info.Parent)
                {
                    var socketVrcf = CreateSocket(socket, socket.Info.DisplayName, "Hips",
                        humanToTransform, inverseArmatureScale, menuMoves, bakedScale);
                    var parent = socketVrcf.gameObject.AddComponent<ParentConstraint>();
                    parent.AddSource(new ConstraintSource
                    {
                        sourceTransform = humanToTransform[Base.BoneName(socket.Info.Bone, Base.Direction.Left)],
                        weight = 1
                    });
                    parent.SetTranslationOffset(0, socket.Location.Positon);
                    parent.SetRotationOffset(0, socket.Location.EulerAngles);
                    parent.AddSource(new ConstraintSource
                    {
                        sourceTransform = humanToTransform[Base.BoneName(socket.Info.Bone, Base.Direction.Right)],
                        weight = 1
                    });
                    parent.SetTranslationOffset(1, socket.Location.Positon);
                    parent.SetRotationOffset(1, socket.Location.EulerAngles);
                    parent.locked = true;
                    parent.constraintActive = true;
                }
                else
                {
                    // TODO: Handle bone not set in Human Avatar
                    var socketVrcf = CreateSocket(socket, socket.Info.DisplayName, socket.Info.Bone.ToString(),
                        humanToTransform, inverseArmatureScale, menuMoves, bakedScale);
                    if (socket.Info.Blendshape)
                    {
                        var blendshape = toggles[socket.Info.Name].SelectedBlendshape;
                        AddBlendshape(socketVrcf, null, blendshape);
                        socketVrcf.enableDepthAnimations = true;
                    }
                }
            }

            // TODO: Refactor??

            if (toggles["Mouth"].On && categoryToggles[Base.Category.Default])
            {
                var gameObjectMouth = new GameObject("Blowjob");
                var socketVrcfMouth = gameObjectMouth.AddComponent<VRCFuryHapticSocket>();
                socketVrcfMouth.Version = 7;
                socketVrcfMouth.addLight = VRCFuryHapticSocket.AddLight.Hole;
                socketVrcfMouth.name = "Blowjob";
                var boneTransformMouth = humanToTransform["Head"];
                var sps = boneTransformMouth.Find("SPS")?.gameObject;
                if (sps == null)
                {
                    sps = new GameObject("SPS");
                    sps.transform.SetParent(boneTransformMouth, false);
                }
                sps.transform.localPosition = Vector3.zero;
                sps.transform.localEulerAngles = Vector3.zero;
                gameObjectMouth.transform.SetParent(sps.transform, false);

                var mouthBlendshape = toggles["Mouth"].SelectedBlendshape;
                AddBlendshape(socketVrcfMouth, null, mouthBlendshape);
                socketVrcfMouth.enableDepthAnimations = true;
                if (vrcAvatar.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                {
                    var visemOhBlendshapeName = vrcAvatar.VisemeBlendShapes[(int)VRC_AvatarDescriptor.Viseme.oh];
                    var mouthPosition = DetectMouthPosition(vrcAvatar.VisemeSkinnedMesh,
                        vrcAvatar.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(
                            visemOhBlendshapeName));
                    gameObjectMouth.transform.position = mouthPosition;
                }
                else
                {
                    var mouthPosition = new Vector3(0, 0.01f, 0.075f);
                    gameObjectMouth.transform.localPosition =
                        Vector3.Scale(mouthPosition, inverseArmatureScale) * bakedScale;
                }
            }

            var vrcFury = avatarGameObject.AddComponent<VRCFury>();
            vrcFury.config.features.AddRange(menuMoves);
            vrcFury.config.features.Add(new MoveMenuItem
            {
                fromPath = "Sockets",
                toPath = "NSFW/SPS"
            });
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
                EditorGUILayout.HelpBox("No Avatar with a VRC Avatar Descriptor found on the active scene.", MessageType.Warning);
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
                }
            }
            
            selectedFootType = (Base.FootType)GUILayout.Toolbar((int)selectedFootType, new[] { "Flat", "Heeled" });

            EditorGUILayout.Space();

            var meshes = selectedAvatar?.GetComponentsInChildren<SkinnedMeshRenderer>();
            EditorGUILayout.BeginVertical();
            drawCategory(Base.Category.Default, meshes);
            EditorGUILayout.BeginHorizontal();
            drawCategory(Base.Category.Special, meshes);
            drawCategory(Base.Category.Feet, meshes);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(16);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(selectedAvatar == null))
                {
                    if (GUILayout.Button("Apply", GUILayout.Width(128), GUILayout.Height(32)))
                    {
                        apply();
                    }
                    if (GUILayout.Button("Clear", GUILayout.Width(128), GUILayout.Height(32)))
                    {
                        Clear(selectedAvatar.gameObject);
                    }
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
        }

        private void drawCategory(Base.Category category, SkinnedMeshRenderer[] meshes)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = style.normal.scaledBackgrounds[0];
            EditorGUILayout.BeginHorizontal(style);
            categoryToggles[category] =
                EditorGUILayout.ToggleLeft(category.ToString(), categoryToggles[category], EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            using (new EditorGUI.DisabledScope(!categoryToggles[category]))
            {
                var togglesStyle = new GUIStyle(GUI.skin.label);
                togglesStyle.padding = new RectOffset(8, 8, 8, 8);
                using (new EditorGUILayout.HorizontalScope(togglesStyle))
                {

                    //EditorGUILayout.Space(16);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        if (category == Base.Category.Default)
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
                            toggles["Mouth"].Draw(meshes);
                        }
                        foreach (var socket in Wholesome.Base.SocketsInCategory(category))
                        {
                            if (socket.FootType == selectedFootType || socket.FootType == Base.FootType.Both)
                            {
                                toggles[socket.Name].Draw(meshes);
                            }


                        }
                        /*
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Select All"))
                            {
                                foreach (var socket in Wholesome.Base.SocketsInCategory(category))
                                {
                                    toggles[socket.Name].On = true;
                                }
                            }

                            if (GUILayout.Button("Deselect All"))
                            {
                                foreach (var socket in Wholesome.Base.SocketsInCategory(category))
                                {
                                    toggles[socket.Name].On = false;
                                }
                            }
                        }*/
                    }
                    //EditorGUILayout.Space(16);
                }
            }

            EditorGUILayout.EndVertical();
            //EditorGUILayout.Space(8);
        }
    }
}