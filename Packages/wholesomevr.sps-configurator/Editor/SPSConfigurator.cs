using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.vrcfury.api;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using com.vrcfury.api.Components;

namespace Wholesome
{
    public class SPSConfigurator : EditorWindow
    {
        private const float SOCKET_OFFSET = 0.015f;
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

        // private int selectedBase = 0;
        // private FootType selectedFootType = FootType.Flat;
        private Texture2D categoryLabelBackground;

        private Texture2D logo;

        private bool defaultOn = true;
        private bool specialOn = true;
        private bool feetOn = true;

        private bool blowjobOn = true;
        private StringBuilder blowjobBlendshape = new StringBuilder();
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
        private bool sfxOn = true;
        private bool sfxPussyOn = true;
        private bool sfxAnalOn = true;
        private Vector2 scrollPosition = new Vector2();
        private SkinnedMeshRenderer bodyRenderer = null;
        private SkinnedMeshRenderer headRenderer = null;

        [MenuItem("Tools/Wholesome/SPS Configurator")]
        public static void Open()
        {
            var window = GetWindow(typeof(SPSConfigurator));
            window.titleContent = new GUIContent("SPS Configurator");
            window.minSize = new Vector2(490, 806);
            window.Show();
        }

        private void OnEnable()
        {
            logo = Resources.Load<Texture2D>("SPS Logo");
            categoryLabelBackground = new Texture2D(1, 1);
            categoryLabelBackground.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
            categoryLabelBackground.Apply();
            meshes = SelectedAvatar?.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        // public void OnSelectionChange()
        // {
        //     meshes = SelectedAvatar?.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        //     Repaint();
        // }

        // public void SetBlendshapes()
        // {
        //     var @base = Bases.All[selectedBase];
        //     pussyBlendshape.Clear();
        //     pussyBlendshape.Append(@base.PussyBlendshape);
        //     analBlendshape.Clear();
        //     analBlendshape.Append(@base.AnalBlendshape);
        // }

        private VRCAvatarDescriptor SelectedAvatar
        {
            get
            {
                VRCAvatarDescriptor selectedAvatar = null;
                // if (Selection.activeTransform != null)
                if (bodyRenderer != null)
                {
                    selectedAvatar = bodyRenderer.GetComponentsInParent<VRCAvatarDescriptor>(true).FirstOrDefault(); // TODO: use last?
                }

                return selectedAvatar;
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


        private void DrawBlendshapeToggle(SkinnedMeshRenderer renderer, string name, ref bool on,
            StringBuilder blendshape)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                on = EditorGUILayout.ToggleLeft(name, on, GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();


                // using (new EditorGUI.DisabledScope(!on || meshes == null))
                using (new EditorGUI.DisabledScope(!on || renderer == null))
                {
                    if (EditorGUILayout.DropdownButton(
                            new GUIContent(blendshape.Length > 0 ? blendshape.ToString() : "None"), FocusType.Keyboard,
                            GUILayout.Width(64 * 3 + 8)))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("None"),
                            blendshape.Length == 0, () => { blendshape.Clear(); });

                        // foreach (var mesh in meshes)
                        // {
                        //     for (int j = 0; j < mesh.sharedMesh.blendShapeCount; j++)
                        //     {
                        //         var blendshapeName = mesh.sharedMesh.GetBlendShapeName(j);
                        //         var menuEntry = $"{mesh.name}/{blendshapeName}";
                        //         menu.AddItem(new GUIContent(menuEntry),
                        //             blendshape.ToString() == blendshapeName,
                        //             blendshapeNameObject =>
                        //             {
                        //                 blendshape.Clear();
                        //                 blendshape.Append(blendshapeNameObject as string);
                        //             },
                        //             blendshapeName);
                        //     }
                        // }
                        for (int j = 0; j < renderer.sharedMesh.blendShapeCount; j++)
                        {
                            var blendshapeName = renderer.sharedMesh.GetBlendShapeName(j);
                            var menuEntry = $"{blendshapeName}";
                            menu.AddItem(new GUIContent(menuEntry),
                                blendshape.ToString() == blendshapeName,
                                blendshapeNameObject =>
                                {
                                    blendshape.Clear();
                                    blendshape.Append(blendshapeNameObject as string);
                                },
                                blendshapeName);
                        }

                        menu.ShowAsContext();
                    }
                }
            }
        }

        private void Apply()
        {
            Debug.Assert(bodyRenderer != null);

            // var avatarBase = Bases.All[selectedBase].DeepCopy();
            VRCAvatarDescriptor vrcAvatar = SelectedAvatar;
            var avatarGameObject = vrcAvatar.gameObject;


            // float bakedScale;
            // if (selectedBase == 0)
            // {
            //     var torsoLength = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.Hips)
            //         .transform
            //         .InverseTransformPoint(FuryUtils.GetBone(avatarGameObject, HumanBodyBones.Neck).transform.position)
            //         .magnitude;
            //     bakedScale = torsoLength / avatarBase.DefaultTorsoLength;

            // }
            // else
            // {
            //     var hipLength = (FuryUtils.GetBone(avatarGameObject, HumanBodyBones.Spine).transform.position
            //                      - FuryUtils.GetBone(avatarGameObject, HumanBodyBones.Hips).transform.position).magnitude;
            //     bakedScale = hipLength / avatarBase.DefaultHipLength;
            // }

            // //var scale = bakedScale / avatarGameObject.transform.lossyScale.y;
            // avatarBase.Scale(bakedScale);

            // var head = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.Head).transform;
            // var mouthOffset = Base.GetMouth(vrcAvatar, head);
            // avatarBase.AlignHands(avatarGameObject);
            var locator = new Locator(avatarGameObject, bodyRenderer);

            var socketBuilder = new SocketBuilder(avatarGameObject);
            if (defaultOn)
            {
                // if (blowjobOn) socketBuilder.Add(BlowjobName, mouthOffset, HumanBodyBones.Head, blendshape: blowjobBlendshape.ToString(), mode: FurySocket.Mode.Hole, auto: true);
                if (blowjobOn)
                {
                    Matrix4x4 mat;
                    var pos = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.Head).transform.position;
                    var fallbackMat = Matrix4x4.TRS(pos + new Vector3(0, 0.05f, 0.1f), Quaternion.identity, new Vector3(1, 1, 1));
                    if (headRenderer && !string.IsNullOrEmpty(blowjobBlendshape.ToString()))
                    {
                        var headLocator = new Locator(avatarGameObject, headRenderer);
                        var res = headLocator.RaycastToBlendshape(blowjobBlendshape.ToString(), Vector3.forward, 0.1f, out Matrix4x4 matBs);
                        if (res) mat = matBs;
                        else mat = fallbackMat;
                    }
                    else
                    {
                        // TODO: Fallback
                        mat = fallbackMat;
                    }
                    socketBuilder.Add(BlowjobName, mat, HumanBodyBones.Head, blendshape: blowjobBlendshape.ToString(), mode: FurySocket.Mode.Hole, auto: true);

                }
                if (pussyOn)
                {
                    Matrix4x4 mat;
                    if (!string.IsNullOrEmpty(pussyBlendshape.ToString()))
                    {
                        var yPos = locator.GetMaxPosInAxis(HumanBodyBones.Hips, Locator.Axis.Y, -1, weightThreshold: 0.95f);
                        // var hit = locator.RaycastToBlendshape(pussyBlendshape.ToString(), Vector3.down, 0.2f, out Vector3 pos);
                        var hit = locator.RaycastAroundToBlendshape(pussyBlendshape.ToString(), Vector3.down, 0.1f, Locator.Axis.X, out Vector3 pos);
                        // pos.y = yPos.y;
                        if (!hit) mat = locator.GetLastPosInDir(HumanBodyBones.Hips, Vector3.up);
                        else mat = Matrix4x4.TRS(pos, Quaternion.AngleAxis(90, Vector3.right), Vector3.one);
                    }
                    else
                    {
                        mat = locator.GetLastPosInDir(HumanBodyBones.Hips, Vector3.up);
                    }

                    FurySocket socket = socketBuilder.Add(PussyName, mat, HumanBodyBones.Hips, blendshape: pussyBlendshape.ToString(), mode: FurySocket.Mode.Hole, auto: true);
                    if (sfxOn && sfxPussyOn)
                    {
                        var tf = socketBuilder.Get(PussyName);
                        SFX.Apply(tf.gameObject, socket);
                    }
                }

                if (analOn)
                {

                    Matrix4x4 mat;
                    if (!string.IsNullOrEmpty(analBlendshape.ToString()))
                    {
                        // var hit = locator.RaycastToBlendshape(analBlendshape.ToString(), Quaternion.AngleAxis(1, Vector3.left) * Vector3.down, 0.2f, out mat);
                        // if (!hit)
                        // {
                        //     mat = locator.GetLastPosInDir(HumanBodyBones.Hips, Vector3.up);
                        //     mat[2, 3] -= 0.02f;
                        //     mat[2, 2] += 0.01f;
                        // }
                        var hit = locator.RaycastAroundToBlendshape(analBlendshape.ToString(), Quaternion.AngleAxis(5, Vector3.right) * Vector3.down, 0.05f, Locator.Axis.Z, out Vector3 pos);
                        // pos.y = yPos.y;
                        if (!hit) mat = locator.GetLastPosInDir(HumanBodyBones.Hips, Vector3.up);
                        else mat = Matrix4x4.TRS(pos, Quaternion.AngleAxis(95, Vector3.right), Vector3.one);

                    }
                    else
                    {
                        mat = locator.GetLastPosInDir(HumanBodyBones.Hips, Vector3.up);
                        mat[2, 3] -= 0.02f;
                        mat[2, 2] += 0.01f;
                    }

                    FurySocket socket = socketBuilder.Add(AnalName, mat, HumanBodyBones.Hips, blendshape: analBlendshape.ToString(), mode: FurySocket.Mode.Hole, auto: true);
                    if (sfxOn && sfxAnalOn)
                    {
                        var tf = socketBuilder.Get(AnalName);
                        SFX.Apply(tf.gameObject, socket);
                    }
                }
                if (handjobOn)
                {
                    Matrix4x4 GetHandMat(Vector3 origin, Vector3 target, Matrix4x4 originWorld)
                    {
                        // var wrist = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightHand);
                        // var finger = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightMiddleProximal);
                        var dir = target - origin;
                        var center = origin + dir * 0.7f;
                        var worldMat = originWorld;
                        // var primaryAxis = Locator.GetDominantAxisInDir(dir, worldMat);
                        // var zAxis = Locator.GetDominantAxisInDir(Vector3.back, worldMat);
                        var secAxis = Locator.GetDominantAxisInDir(dir * -1, worldMat);

                        // var zCol = worldMat.GetColumn((int)zAxis.Item1) * zAxis.Item2;
                        var zCol = (Vector4)Vector3.back;
                        var xCol = worldMat.GetColumn((int)secAxis.Item1) * secAxis.Item2;
                        var yCol = (Vector4)Vector3.Cross((Vector3)zCol, (Vector3)xCol);
                        // TODO: Check if basis is perpedicualr

                        var ySign = Vector3.Dot((Vector3)yCol, Vector3.up) > 0 ? 1 : -1;
                        // Math.Sign()
                        var trans = new Vector4(center.x, center.y, center.z, 1);
                        var mat = new Matrix4x4(xCol, yCol, zCol, trans);

                        var raycastMat = mat * Matrix4x4.Translate(new Vector3(0, -0.1f * ySign, 0));

                        var pos = (Vector3)locator.RaycastTarget(center, raycastMat.GetPosition());
                        if (pos == null) pos = center;
                        var transPos = new Vector4(pos.x, pos.y, pos.z, 1);
                        mat.SetColumn(3, transPos);
                        var mat1 = mat * Matrix4x4.Translate(new Vector3(0, -SOCKET_OFFSET * ySign, 0));

                        return mat1;
                    }

                    var wristRight = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightHand).transform;
                    var fingerRight = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightMiddleProximal).transform;
                    var matRight = GetHandMat(wristRight.position, fingerRight.position, wristRight.localToWorldMatrix);
                    var wristLeft = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftHand).transform;
                    var fingerLeft = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftMiddleProximal).transform;
                    var matLeft = GetHandMat(wristLeft.position, fingerLeft.position, wristLeft.localToWorldMatrix);


                    if (handjobRightOn)
                    {
                        // // socketBuilder.Add(HandjobRightName, avatarBase.HandRight, HumanBodyBones.RightHand, category: "Handjob", auto: true);
                        // var wrist = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightHand);
                        // var finger = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightMiddleProximal);
                        // var dir = finger.transform.position - wrist.transform.position;
                        // var center = wrist.transform.position + dir * 0.7f;
                        // var worldMat = wrist.transform.localToWorldMatrix;
                        // // var primaryAxis = Locator.GetDominantAxisInDir(dir, worldMat);
                        // // var zAxis = Locator.GetDominantAxisInDir(Vector3.back, worldMat);
                        // var secAxis = Locator.GetDominantAxisInDir(dir * -1, worldMat);

                        // // var zCol = worldMat.GetColumn((int)zAxis.Item1) * zAxis.Item2;
                        // var zCol = (Vector4)Vector3.back;
                        // var xCol = worldMat.GetColumn((int)secAxis.Item1) * secAxis.Item2;
                        // var yCol = (Vector4)Vector3.Cross((Vector3)zCol, (Vector3)xCol);
                        // // TODO: Check if basis is perpedicualr

                        // var ySign = Vector3.Dot((Vector3)yCol, Vector3.up) > 0 ? 1 : -1;
                        // // Math.Sign()
                        // var trans = new Vector4(center.x, center.y, center.z, 1);
                        // var mat = new Matrix4x4(xCol, yCol, zCol, trans);

                        // var raycastMat = mat * Matrix4x4.Translate(new Vector3(0, -0.1f * ySign, 0));

                        // var pos = (Vector3)locator.RaycastTarget(center, raycastMat.GetPosition());
                        // if (pos == null) pos = center;
                        // var transPos = new Vector4(pos.x, pos.y, pos.z, 1);
                        // mat.SetColumn(3, transPos);
                        // var mat1 = mat * Matrix4x4.Translate(new Vector3(0, -SOCKET_OFFSET * ySign, 0));

                        // socketBuilder.Add(HandjobRightName, mat1, HumanBodyBones.RightHand, category: "Handjob", auto: true);
                        // var wrist = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightHand);
                        // var finger = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightMiddleProximal);
                        // var mat = GetHandMat(wrist.transform.position, finger.transform.position, wrist.transform.localToWorldMatrix);
                        socketBuilder.Add(HandjobRightName, matRight, HumanBodyBones.RightHand, category: "Handjob", auto: true);

                    }
                    if (handjobBothOn)
                        socketBuilder.AddParent(HandjobDoubleName, matLeft, matRight,
                            HumanBodyBones.LeftHand, HumanBodyBones.RightHand, "Handjob");
                    if (handjobLeftOn)
                    {
                        // var wrist = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftHand);
                        // var finger = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftMiddleProximal);
                        // var dir = finger.transform.position - wrist.transform.position;
                        // var center = wrist.transform.position + dir / 2;
                        // var worldMat = wrist.transform.localToWorldMatrix;
                        // var zAxis = Locator.GetDominantAxisInDir(Vector3.back, worldMat);
                        // var secAxis = Locator.GetDominantAxisInDir(dir * -1, worldMat);

                        // var zCol = worldMat.GetColumn((int)zAxis.Item1) * zAxis.Item2;
                        // var xCol = worldMat.GetColumn((int)secAxis.Item1) * secAxis.Item2;
                        // var yCol = (Vector4)Vector3.Cross((Vector3)zCol, (Vector3)xCol);

                        // var trans = new Vector4(center.x, center.y, center.z, 1);
                        // var mat = new Matrix4x4(xCol, yCol, zCol, trans);

                        // var ySign = Vector3.Dot((Vector3)yCol, Vector3.up) > 0 ? 1 : -1;
                        // var mat1 = mat * Matrix4x4.Translate(new Vector3(0, -SOCKET_OFFSET * ySign, 0));
                        // var wrist = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftHand);
                        // var finger = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftMiddleProximal);
                        // var mat = GetHandMat(wrist.transform.position, finger.transform.position, wrist.transform.localToWorldMatrix);

                        socketBuilder.Add(HandjobLeftName, matLeft, HumanBodyBones.LeftHand, category: "Handjob", auto: true);
                    }
                    // socketBuilder.Add(HandjobLeftName, avatarBase.HandLeft, HumanBodyBones.LeftHand, category: "Handjob", auto: true);
                    // socketBuilder.AddCategoryIconSet("Handjob");
                }
            }
            if (specialOn)
            {
                if (titjobOn)
                {
                    var nipRight = locator.GetMaxPosInAxis(HumanBodyBones.Chest, Locator.Axis.Z, 1, 1);
                    var nipLeft = locator.GetMaxPosInAxis(HumanBodyBones.Chest, Locator.Axis.Z, 1, -1);
                    var mid = (nipRight - nipLeft) / 2 + nipLeft;
                    var q = Quaternion.AngleAxis(90, Vector3.right);
                    var pos = locator.RaycastTarget(mid, mid + Vector3.forward);
                    if (pos == null) pos = mid;
                    var mat = Matrix4x4.TRS((Vector3)pos, q, Vector3.one) * Matrix4x4.Translate(new Vector3(0, SOCKET_OFFSET, 0));
                    socketBuilder.Add(TitjobName, mat, HumanBodyBones.Chest, "Special");
                }
                if (assjobOn)
                {
                    var cheekRight = locator.GetMaxPosInAxis(HumanBodyBones.Hips, Locator.Axis.Z, -1, 1);
                    var cheekLeft = locator.GetMaxPosInAxis(HumanBodyBones.Hips, Locator.Axis.Z, -1, -1);
                    var mid = (cheekRight - cheekLeft) / 2 + cheekLeft;
                    var q = Quaternion.AngleAxis(90, Vector3.right);

                    var pos = locator.RaycastTarget(mid, mid + Vector3.back);
                    if (pos == null) pos = mid;

                    var mat = Matrix4x4.Translate(new Vector3(0, 0, -SOCKET_OFFSET)) * Matrix4x4.TRS((Vector3)pos, q, Vector3.one);
                    socketBuilder.Add(AssjobName, mat, HumanBodyBones.Hips, "Special", mode: FurySocket.Mode.Ring);
                }
                if (thighjobOn)
                {
                    var upperLegLeft = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftUpperLeg).transform.position;
                    var lowerLegLeft = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.LeftLowerLeg).transform.position;
                    var upperLegRight = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightUpperLeg).transform.position;
                    var lowerLegRight = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightLowerLeg).transform.position;

                    var posLeft = (lowerLegLeft - upperLegLeft) * 0.3f + upperLegLeft;
                    var posRight = (lowerLegRight - upperLegRight) * 0.3f + upperLegRight;

                    var q = Quaternion.AngleAxis(180, Vector3.right);

                    var matLeft = Matrix4x4.TRS(posLeft, q, Vector3.one);
                    var matRight = Matrix4x4.TRS(posRight, q, Vector3.one);

                    socketBuilder.AddParent(ThighjobName, matLeft, matRight, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, "Special");
                }
                // socketBuilder.AddCategoryIconSet("Special");
            }

            if (feetOn)
            {

                Matrix4x4 GetSoleMat(HumanBodyBones human)
                {
                    var bone = FuryUtils.GetBone(avatarGameObject, human);
                    var bonePos = bone.transform.position;

                    var backPos = locator.GetMaxPosInAxis(human, Locator.Axis.Z, -1, 0, 0.5f);
                    var frontPos = locator.GetMaxPosInAxis(human, Locator.Axis.Z, 1, 0, 0.5f);
                    var mid = (frontPos - backPos) * 0.65f + backPos;
                    mid.x = bonePos.x;


                    var dirZ = frontPos - backPos;
                    dirZ.x = 0;
                    dirZ = dirZ.normalized;
                    var dirY = Vector3.Cross(Vector3.right, dirZ);

                    var hit = (RaycastHit)locator.Raycast(mid, mid + dirY);

                    var leftPos = locator.GetMaxPosInAxis(human, Locator.Axis.X, -1, 0, 0.5f);
                    var rightPos = locator.GetMaxPosInAxis(human, Locator.Axis.X, 1, 0, 0.5f);
                    var width = rightPos.x - leftPos.x;
                    var normal = locator.GetAvgNormalCloseToPoint(hit.point, hit.normal, 30, width / 4);
                    var dirZNew = Vector3.Cross(Vector3.right, normal);
                    var normalP = Vector3.Cross(dirZNew, Vector3.right);

                    var colTrans = new Vector4(hit.point.x, hit.point.y, hit.point.z, 1);

                    var mat = new Matrix4x4((Vector4)Vector3.right, (Vector4)normalP, (Vector4)dirZNew, colTrans) * Matrix4x4.Translate(new Vector3(0, 0.015f, 0));
                    return mat;
                }

                var rightMat = GetSoleMat(HumanBodyBones.RightFoot);
                var leftMat = GetSoleMat(HumanBodyBones.LeftFoot);
                if (soleRightOn)
                {
                    // var backPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.Z, -1, 1, 0.5f);
                    // var frontPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.Z, 1, 1, 0.5f);
                    // var midZ = (frontPos - backPos) / 2 + backPos;
                    // var dirZ = (frontPos - backPos).normalized;
                    // var leftPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.X, -1, 1, 0.5f);
                    // var rightPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.X, 1, 1, 0.5f);
                    // var midX = (rightPos - leftPos) / 2 + leftPos;

                    // var dirY = Vector3.Cross(dirZ, Vector3.right);
                    // var colTrans = new Vector4(midX.x, midZ.y, midZ.z, 1);
                    // var mat = new Matrix4x4((Vector4)Vector3.right, (Vector4)dirY, (Vector4)dirZ, colTrans);
                    // var bone = FuryUtils.GetBone(avatarGameObject, HumanBodyBones.RightFoot);
                    // var bonePos = bone.transform.position;

                    // var backPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.Z, -1, 1, 0.5f);
                    // var frontPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.Z, 1, 1, 0.5f);
                    // var mid = (frontPos - backPos) * 0.65f + backPos;
                    // mid.x = bonePos.x;


                    // var dirZ = frontPos - backPos;
                    // dirZ.x = 0;
                    // dirZ = dirZ.normalized;
                    // var dirY = Vector3.Cross(Vector3.right, dirZ);

                    // var hit = (RaycastHit)locator.Raycast(mid, mid + dirY);

                    // var leftPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.X, -1, 1, 0.5f);
                    // var rightPos = locator.GetMaxPosInAxis(HumanBodyBones.RightFoot, Locator.Axis.X, 1, 1, 0.5f);
                    // var width = rightPos.x - leftPos.x;
                    // var normal = locator.GetAvgNormalCloseToPoint(hit.point, hit.normal, 30, width / 4);
                    // var dirZNew = Vector3.Cross(Vector3.right, normal);
                    // var normalP = Vector3.Cross(dirZNew, Vector3.right);

                    // var colTrans = new Vector4(hit.point.x, hit.point.y, hit.point.z, 1);

                    // var mat = new Matrix4x4((Vector4)Vector3.right, (Vector4)normalP, (Vector4)dirZNew, colTrans) * Matrix4x4.Translate(new Vector3(0, 0.015f, 0));

                    // var (mid, dir) = locator.GetCenterInNormalDirection(HumanBodyBones.RightFoot, normal, 15);
                    // dir.x = 0;
                    // dir = dir.normalized;
                    // var dirY = Vector3.Cross(Vector3.right, dir);

                    // var (mid, dirY) = locator.GetCenterInNormalDirection(HumanBodyBones.RightFoot, normal, 15);
                    // dirY.x = 0;
                    // dirY = dirY.normalized;
                    // var dirZNormal = Vector3.Cross(Vector3.right, dirY);

                    // var colTrans = new Vector4(mid.x, mid.y, mid.z, 1);
                    // var mat = new Matrix4x4((Vector4)Vector3.right, (Vector4)dirY, (Vector4)dirZNormal, colTrans);
                    socketBuilder.Add(SoleRightName, rightMat, HumanBodyBones.RightFoot, "Feet", auto: true);
                }
                if (footjobOn)
                {
                    var centerRight = locator.GetAABBCenter(HumanBodyBones.RightFoot);
                    var centerLeft = locator.GetAABBCenter(HumanBodyBones.LeftFoot);
                    var qRight = Quaternion.Euler(new Vector3(-90, 0, 0)) * rightMat.rotation;
                    var qLeft = Quaternion.Euler(new Vector3(-90, 0, 0)) * leftMat.rotation;

                    var fjMatLeft = Matrix4x4.TRS(centerLeft, qLeft, Vector3.one);
                    var fjMatRight = Matrix4x4.TRS(centerRight, qRight, Vector3.one);

                    socketBuilder.AddParent(FootjobName, fjMatLeft, fjMatRight, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, "Feet");
                }
                if (soleLeftOn)
                {
                    socketBuilder.Add(SoleLeftName, leftMat, HumanBodyBones.LeftFoot, "Feet", auto: true);
                }
                // socketBuilder.AddCategoryIconSet("Feet");
            }
            if (sfxOn) SFX.AddToggle(socketBuilder.SpsObject);

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

            VRCAvatarDescriptor selectedAvatar = null;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                bodyRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Body Mesh", bodyRenderer, typeof(SkinnedMeshRenderer), allowSceneObjects: true);
                if (bodyRenderer)
                {
                    var gameObject = bodyRenderer.gameObject;
                    selectedAvatar = gameObject.GetComponent<VRCAvatarDescriptor>();
                    if (selectedAvatar == null)
                    {
                        selectedAvatar = gameObject.GetComponentsInParent<VRCAvatarDescriptor>(true).FirstOrDefault(); // TODO: use last?
                    }
                }

                if (check.changed)
                {
                    blowjobBlendshape.Clear();
                    pussyBlendshape.Clear();
                    analBlendshape.Clear();
                    headRenderer = null;
                }
                if (check.changed && selectedAvatar)
                {
                    if (selectedAvatar.lipSync == VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape && selectedAvatar.VisemeSkinnedMesh != null)
                    {
                        headRenderer = selectedAvatar.VisemeSkinnedMesh;
                        if (headRenderer != null && selectedAvatar.VisemeBlendShapes != null && selectedAvatar.VisemeBlendShapes.Length > (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.oh)
                        {
                            var blendshapeName = selectedAvatar.VisemeBlendShapes[(int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.oh];
                            if (!string.IsNullOrEmpty(blendshapeName))
                            {
                                blowjobBlendshape.Clear();
                                blowjobBlendshape.Append(blendshapeName);
                            }
                        }
                    }
                }


            }

            // var selectedAvatar = SelectedAvatar;

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

            // using (var check = new EditorGUI.ChangeCheckScope())
            // {
            //     selectedBase =
            //         GUILayout.Toolbar(selectedBase, Bases.All.Select(@base => @base.Name).ToArray(),
            //             GUILayout.Height(32));
            //     if (check.changed)
            //     {
            //         SetBlendshapes();
            //         if (selectedAvatar != null)
            //         {
            //             meshes = selectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            //         }
            //         else
            //         {
            //             meshes = null;
            //         }
            //     }
            // }

            // selectedFootType = (FootType)GUILayout.Toolbar((int)selectedFootType, new[] { "Flat", "Heeled" });

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            BeginCategory("Default", ref defaultOn);
            DrawLabels();
            DrawBlendshapeToggle(headRenderer, BlowjobName, ref blowjobOn, blowjobBlendshape);
            DrawSymmetricBothToggle(HandjobName, ref handjobOn, ref handjobLeftOn, ref handjobRightOn,
                ref handjobBothOn);
            DrawBlendshapeToggle(bodyRenderer, PussyName, ref pussyOn, pussyBlendshape);
            DrawBlendshapeToggle(bodyRenderer, AnalName, ref analOn, analBlendshape);
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
                using (new EditorGUI.DisabledScope(selectedAvatar == null || bodyRenderer == null))
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
                if (GUILayout.Button("Add test plug to Avatar", linkStyle, GUILayout.ExpandWidth(false)))
                {
                    try
                    {
                        var testPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                            "Packages/wholesomevr.sps-configurator/Assets/Actual Wholesome Lollipop.prefab");
                        var initiatedPrefab =
                            PrefabUtility.InstantiatePrefab(testPrefab, selectedAvatar.transform) as GameObject;
                        var head = FuryUtils.GetBone(selectedAvatar.gameObject, HumanBodyBones.Head).transform;
                        var mouthOffset = Base.GetMouth(selectedAvatar, head);
                        var mouthPosition = head.TransformPoint(mouthOffset.Positon) + new Vector3(0, 0, 0.25f);
                        initiatedPrefab.transform.SetPositionAndRotation(mouthPosition,
                            Quaternion.AngleAxis(-180, Vector3.left));
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
    }
}