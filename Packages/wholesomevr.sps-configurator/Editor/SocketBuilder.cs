using System;
using System.Reflection;
using com.vrcfury.api;
using com.vrcfury.api.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;

namespace Wholesome
{
    public class SocketBuilder
    {
        private static readonly FieldInfo FurySocketComponentField =
            typeof(FurySocket).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameObject avatarObject;
        private Transform spsParent;

        public SocketBuilder(GameObject avatarObject)
        {
            if (avatarObject.GetComponent<VRCAvatarDescriptor>() == null)
                throw new ArgumentException("avatarObject doesn't have a VRC Avatar Descriptor");
            this.avatarObject = avatarObject;
        }

        public GameObject SpsObject => spsParent.gameObject;

        public FurySocket Add(string name, Base.Offset offset, HumanBodyBones bone, string category = null,
            string blendshape = null, bool auto = false,
            FurySocket.Mode mode = FurySocket.Mode.Auto,
            bool useRadiusOffset = false)
        {
            var (obj, socket) = CreateSocket(name, category, mode, auto, blendshape, useRadiusOffset);
            SetArmatureLinkedOffset(obj, bone, offset);
            AddSocketToAvatar(obj, category);
            obj.transform.localScale = Vector3.one;
            return socket;
        }

        public FurySocket Add(string name, Matrix4x4 mat, HumanBodyBones bone, string category = null,
            string blendshape = null, bool auto = false,
            FurySocket.Mode mode = FurySocket.Mode.Auto,
            bool useRadiusOffset = false)
        {
            var (obj, socket) = CreateSocket(name, category, mode, auto, blendshape, useRadiusOffset);
            SetArmatureLinkedOffset(obj, bone, mat);
            AddSocketToAvatar(obj, category);
            obj.transform.localScale = Vector3.one;
            return socket;
        }

        // public void Add(string name, Locator.Pose pose, HumanBodyBones bone, string category = null,
        //     string blendshape = null, bool auto = false,
        //     FurySocket.Mode mode = FurySocket.Mode.Auto)
        // {
        //     var socket = CreateSocket(name, category, light, auto, blendshape);
        //     SetArmatureLinkedOffset(socket.gameObject, bone, pose);
        //     AddSocketToAvatar(socket.gameObject, category);
        //     socket.transform.localScale = Vector3.one;
        // }

        public void AddParent(string name, Base.Offset offsetLeft, Base.Offset offsetRight, HumanBodyBones boneLeft,
            HumanBodyBones boneRight, string category = null,
            string blendshape = null, bool auto = false,
            FurySocket.Mode mode = FurySocket.Mode.Auto)
        {
            var gameObject = new GameObject(name);
            var (obj, socket) = CreateSocket(name, category, mode, auto, blendshape);
            obj.name = $"{name} Socket";
            obj.transform.SetParent(gameObject.transform, true);

            var targetLeft = new GameObject($"{name} Target Left");
            SetArmatureLinkedOffset(targetLeft, boneLeft, offsetLeft);
            targetLeft.transform.SetParent(gameObject.transform, true);

            var targetRight = new GameObject($"{name} Target Right");
            SetArmatureLinkedOffset(targetRight, boneRight, offsetRight);
            targetRight.transform.SetParent(gameObject.transform, true);

            SetParentConstraint(obj, targetLeft.transform, targetRight.transform);
            AddSocketToAvatar(gameObject, category);
            targetRight.transform.localPosition =
                Vector3.Scale(targetRight.transform.localPosition, gameObject.transform.localScale);
            targetLeft.transform.localPosition =
                Vector3.Scale(targetLeft.transform.localPosition, gameObject.transform.localScale);
            gameObject.transform.localScale = Vector3.one;
        }

        public void AddParent(string name, Matrix4x4 matLeft, Matrix4x4 matRight, HumanBodyBones boneLeft,
            HumanBodyBones boneRight, string category = null,
            string blendshape = null, bool auto = false,
            FurySocket.Mode mode = FurySocket.Mode.Auto)
        {
            var gameObject = new GameObject(name);
            var (obj, socket) = CreateSocket(name, category, mode, auto, blendshape);
            obj.name = $"{name} Socket";
            obj.transform.SetParent(gameObject.transform, true);

            var targetLeft = new GameObject($"{name} Target Left");
            SetArmatureLinkedOffset(targetLeft, boneLeft, matLeft);
            targetLeft.transform.SetParent(gameObject.transform, true);

            var targetRight = new GameObject($"{name} Target Right");
            SetArmatureLinkedOffset(targetRight, boneRight, matRight);
            targetRight.transform.SetParent(gameObject.transform, true);

            SetParentConstraint(obj, targetLeft.transform, targetRight.transform);
            AddSocketToAvatar(gameObject, category);
            targetRight.transform.localPosition =
                Vector3.Scale(targetRight.transform.localPosition, gameObject.transform.localScale);
            targetLeft.transform.localPosition =
                Vector3.Scale(targetLeft.transform.localPosition, gameObject.transform.localScale);
            gameObject.transform.localScale = Vector3.one;
        }

        private (GameObject, FurySocket) CreateSocket(string name, string category, FurySocket.Mode mode,
            bool auto, string blendshape, bool useRadiusOffset = false)
        {
            var obj = new GameObject(name);
            var socket = FuryComponents.CreateSocket(obj);
            socket.SetMode(mode);
            socket.SetName(String.IsNullOrWhiteSpace(category) ? name : $"{category}/{name}");
            if (useRadiusOffset) SetRadiusOffset(socket);
            if (!auto) socket.SetAutoOff();
            if (blendshape == null) return (obj, socket);

            var action = socket.AddDepthActions(new Vector2(0.05f, 0), 0, true);
            action.AddBlendshape(blendshape, 100);
            return (obj, socket);
        }

        private static void SetRadiusOffset(FurySocket socket)
        {
            if (FurySocketComponentField == null)
            {
                throw new MissingFieldException(typeof(FurySocket).FullName, "s");
            }

            var socketComponent = FurySocketComponentField.GetValue(socket);
            if (socketComponent == null)
            {
                throw new InvalidOperationException("VRCFury socket wrapper did not contain an internal socket component.");
            }

            var radiusOffsetField = socketComponent.GetType()
                .GetField("useRadiusOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (radiusOffsetField == null)
            {
                throw new MissingFieldException(socketComponent.GetType().FullName, "useRadiusOffset");
            }

            if (radiusOffsetField.FieldType != typeof(bool))
            {
                throw new InvalidOperationException($"{socketComponent.GetType().FullName}.useRadiusOffset is not a bool field.");
            }

            radiusOffsetField.SetValue(socketComponent, true);
            if (socketComponent is UnityEngine.Object unityObject)
            {
                EditorUtility.SetDirty(unityObject);
            }
        }

        private void SetArmatureLinkedOffset(GameObject gameObject, HumanBodyBones bone, Base.Offset offset)
        {
            // var vrcf = gameObject.AddComponent<VRCFury>();
            // vrcf.Version = 2;
            // vrcf.config.features.Add(new ArmatureLink()
            // {
            //     propBone = vrcf.gameObject,
            //     boneOnAvatar = bone,
            //     Version = 5
            // });
            var link = FuryComponents.CreateArmatureLink(gameObject);
            link.LinkTo(bone);
            var transform = FuryUtils.GetBone(avatarObject, bone).transform;
            //gameObject.transform.position = transform.TransformPoint(offset.Positon);
            gameObject.transform.position = transform.position;
            gameObject.transform.rotation = transform.rotation;
            gameObject.transform.Translate(offset.Positon);
            gameObject.transform.Rotate(offset.EulerAngles);
        }

        private void SetArmatureLinkedOffset(GameObject gameObject, HumanBodyBones bone, Matrix4x4 mat)
        {
            // var vrcf = gameObject.AddComponent<VRCFury>();
            // vrcf.Version = 2;
            // vrcf.config.features.Add(new ArmatureLink()
            // {
            //     propBone = vrcf.gameObject,
            //     boneOnAvatar = bone,
            //     Version = 5
            // });
            var link = FuryComponents.CreateArmatureLink(gameObject);
            link.LinkTo(bone);
            var transform = FuryUtils.GetBone(avatarObject, bone).transform;
            //gameObject.transform.position = transform.TransformPoint(offset.Positon);
            gameObject.transform.position = transform.position;
            gameObject.transform.rotation = transform.rotation;
            gameObject.transform.SetPositionAndRotation(mat.GetPosition(), mat.rotation);
        }

        // private void SetArmatureLinkedOffset(GameObject gameObject, HumanBodyBones bone, Locator.Pose pose)
        // {
        //     var link = FuryComponents.CreateArmatureLink(gameObject);
        //     link.LinkTo(bone);
        //     gameObject.transform.position = pose.Position;
        //     gameObject.transform.eulerAngles = pose.EulerAngles;
        // }

        private void SetParentConstraint(GameObject gameObject, Transform targetLeft, Transform targetRight)
        {
            var parent = gameObject.AddComponent<ParentConstraint>();
            parent.AddSource(new ConstraintSource
            {
                sourceTransform = targetLeft.transform,
                weight = 1
            });
            parent.AddSource(new ConstraintSource
            {
                sourceTransform = targetRight.transform,
                weight = 1
            });
            parent.locked = true;
            parent.constraintActive = true;
        }

        private void AddSocketToAvatar(GameObject socket, string category)
        {
            if (spsParent == null)
            {
                var newSps = new GameObject("SPS");
                Undo.RegisterCreatedObjectUndo(newSps, "Add Sockets");
                newSps.transform.SetParent(avatarObject.transform, false);
                spsParent = newSps.transform;
            }

            if (!String.IsNullOrWhiteSpace(category))
            {
                var categoryTranform = spsParent.Find(category);
                if (categoryTranform == null)
                {
                    var newCategory = new GameObject(category);
                    newCategory.transform.SetParent(spsParent, false);
                    categoryTranform = newCategory.transform;
                }

                socket.transform.SetParent(categoryTranform, true);
            }
            else
            {
                socket.transform.SetParent(spsParent, true);
            }
        }

        public Transform Get(string socketName, string category = null)
        {
            return spsParent.Find(category == null ? socketName : $"{category}/{socketName}");
        }
    }
}
