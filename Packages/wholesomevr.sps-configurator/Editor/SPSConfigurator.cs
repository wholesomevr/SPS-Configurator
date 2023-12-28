using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using VF.Builder;
using VF.Component;
using VF.Model;
using VF.Model.Feature;
using VRC.SDK3.Avatars.Components;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Wholesome
{
    public class SPSConfigurator : EditorWindow
    {
#pragma warning disable 0219
#pragma warning disable 0414
        private static readonly string BlowjobName = "Blowjob";
        private static readonly string HandjobName = "Handjob";
        private static readonly string HandjobDoubleName = $"Double {HandjobName}";
        private static readonly string HandjobLeftName = $"{HandjobName} Left";
        private static readonly string HandjobRightName = $"{HandjobName} Right";
        private static readonly string PussyName = "Pussy";
        private static readonly string AnalName = "Anal";
        private static readonly string TitjobName = "Titjob";
        private static readonly string AssjobName = "Assjob";
        private static readonly string ThighjobName = "Thighjob";
        private static readonly string SoleName = "Steppies";
        private static readonly string SoleLeftName = "Steppies Left";
        private static readonly string SoleRightName = "Steppies Right";
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
            if (spsMoveMenu != null)
            {
                return spsMoveMenu.toPath;
            }

            return "SPS";
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

        private void Apply()
        {
            var avatarBase = Bases.All[selectedBase].DeepCopy();
            VRCAvatarDescriptor vrcAvatar = SelectedAvatar;
            var avatarGameObject = vrcAvatar.gameObject;
            var armature = new AvatarArmature(avatarGameObject);
            
            float bakedScale;
            if (selectedBase == 0)
            {
                var torsoLength = armature.FindBone(HumanBodyBones.Hips)
                    .InverseTransformPoint(armature.FindBone(HumanBodyBones.Neck).position)
                    .magnitude;
                bakedScale = torsoLength / avatarBase.DefaultTorsoLength;
                
            }
            else
            {
                var hipLength = (armature.FindBone(HumanBodyBones.Spine).position
                                 - armature.FindBone(HumanBodyBones.Hips).position).magnitude;
                bakedScale = hipLength / avatarBase.DefaultHipLength; 
            }

            //var scale = bakedScale / avatarGameObject.transform.lossyScale.y;
            avatarBase.Scale(bakedScale);
            using (var avatarArmature = new AvatarArmature(avatarGameObject))
            {
                var head = avatarArmature.FindBone(HumanBodyBones.Head);
                var mouthOffset = Base.GetMouth(vrcAvatar, head);
                avatarBase.AlignHands(armature);
                
                var socketBuilder = new SocketBuilder(avatarGameObject, avatarArmature);
                if (defaultOn)
                {
                    if (blowjobOn) socketBuilder.Add(BlowjobName, mouthOffset, HumanBodyBones.Head, blendshape: blowjobBlendshape.ToString(), light: VRCFuryHapticSocket.AddLight.Hole, auto: true);
                    if (pussyOn) socketBuilder.Add(PussyName, avatarBase.Pussy, HumanBodyBones.Hips, blendshape: pussyBlendshape.ToString(), light: VRCFuryHapticSocket.AddLight.Hole, auto: true);
                    if (analOn) socketBuilder.Add(AnalName, avatarBase.Anal, HumanBodyBones.Hips, blendshape: analBlendshape.ToString(), light: VRCFuryHapticSocket.AddLight.Hole);
                    if (handjobOn)
                    {
                        if (handjobRightOn)
                            socketBuilder.Add(HandjobRightName, avatarBase.HandRight, HumanBodyBones.RightHand, category: "Handjob", auto: true);
                        if (handjobBothOn)
                            socketBuilder.AddParent(HandjobDoubleName, avatarBase.HandLeft, avatarBase.HandRight,
                                HumanBodyBones.LeftHand, HumanBodyBones.RightHand, "Handjob");
                        if (handjobLeftOn)
                            socketBuilder.Add(HandjobLeftName, avatarBase.HandLeft, HumanBodyBones.LeftHand, category: "Handjob", auto: true);
                        socketBuilder.AddCategoryIconSet("Handjob");
                    }
                }
                if (specialOn)
                {
                    if(titjobOn) socketBuilder.Add(TitjobName, avatarBase.Titjob, HumanBodyBones.Chest, "Special");
                    if(assjobOn) socketBuilder.Add(AssjobName, avatarBase.Assjob, HumanBodyBones.Hips, "Special", light: VRCFuryHapticSocket.AddLight.Ring);
                    if(thighjobOn) socketBuilder.AddParent(ThighjobName, avatarBase.ThighjobLeft, avatarBase.ThighjobRight, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, "Special");
                    socketBuilder.AddCategoryIconSet("Special");
                }

                if (feetOn)
                {
                    if(soleRightOn) socketBuilder.Add(SoleRightName, avatarBase.GetSole(Side.Right, selectedFootType), HumanBodyBones.RightFoot, "Feet", auto: true);
                    if(footjobOn) socketBuilder.AddParent(FootjobName, avatarBase.GetFootjob(Side.Left, selectedFootType), avatarBase.GetFootjob(Side.Right, selectedFootType), HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, "Feet");
                    if(soleLeftOn) socketBuilder.Add(SoleLeftName, avatarBase.GetSole(Side.Left, selectedFootType), HumanBodyBones.LeftFoot, "Feet", auto: true);
                    socketBuilder.AddCategoryIconSet("Feet");
                }
                if (sfxOn)
                {
                    var sfx = new SFX();
                    var pussy = socketBuilder.Get(PussyName);
                    var anal = socketBuilder.Get(AnalName);
                    if(sfxPussyOn && pussy != null) sfx.Apply(pussy);
                    if(sfxAnalOn && anal != null) sfx.Apply(anal);
                    if (sfxPussyOn && pussy != null || sfxAnalOn && anal != null)
                        sfx.AddToggle(socketBuilder.SpsObject);
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
            BeginCategory("Sound FX (Beta)", ref sfxOn, note);
            EditorGUILayout.BeginHorizontal();
            //sfxBlowjobOn = EditorGUILayout.ToggleLeft(BlowjobName, sfxBlowjobOn, GUILayout.Width(94));
            sfxPussyOn = EditorGUILayout.ToggleLeft(PussyName, sfxPussyOn, GUILayout.Width(94));
            sfxAnalOn = EditorGUILayout.ToggleLeft(AnalName, sfxAnalOn, GUILayout.Width(94));
            EditorGUILayout.EndHorizontal();
            EndCategory();
            DrawParameterEstimation(selectedAvatar);
            EditorGUILayout.Space(16);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(selectedAvatar == null))
                {
                    if (GUILayout.Button("Add Sockets", GUILayout.Width(128), GUILayout.Height(32)))
                    {
                        try
                        {
                            Apply();
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
                        using (var avatarArmature = new AvatarArmature(selectedAvatar.gameObject))
                        {
                            var head = avatarArmature.FindBone(HumanBodyBones.Head);
                            var mouthOffset = Base.GetMouth(selectedAvatar, head);
                            var mouthPosition = head.TransformPoint(mouthOffset.Positon) + new Vector3(0, 0, 0.25f);
                            initiatedPrefab.transform.SetPositionAndRotation(mouthPosition,
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
            if (sfxOn && (sfxPussyOn || sfxAnalOn)) spsCost += SFX_SPS_COST;

            if (defaultOn)
            {
                if (blowjobOn) spsCost += SINGLE_SPS_COST;
                if (handjobOn)
                {
                    if (handjobLeftOn) spsCost += SINGLE_SPS_COST;
                    if (handjobRightOn) spsCost += SINGLE_SPS_COST;
                    if (handjobBothOn) spsCost += SINGLE_SPS_COST;
                }
                if (pussyOn) spsCost += SINGLE_SPS_COST;
                if (analOn) spsCost += SINGLE_SPS_COST;
            }

            if (specialOn)
            {
                if (titjobOn) spsCost += SINGLE_SPS_COST;
                if (assjobOn) spsCost += SINGLE_SPS_COST;
                if (thighjobOn) spsCost += SINGLE_SPS_COST;
            }

            if (feetOn)
            {
                if (soleOn)
                {
                    if (soleLeftOn) spsCost += SINGLE_SPS_COST;
                    if (soleRightOn) spsCost += SINGLE_SPS_COST;
                }

                if (footjobOn) spsCost += SINGLE_SPS_COST;
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