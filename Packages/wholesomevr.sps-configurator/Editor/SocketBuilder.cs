using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VF.Component;
using VF.Model;
using VF.Model.Feature;
using VF.Model.StateAction;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Wholesome
{
    public class SocketBuilder
    {
        private GameObject avatarObject;
        private SPSConfigurator.AvatarArmature avatarArmature;
        private string spsPath;
        private Transform spsParent;

        public SocketBuilder(GameObject avatarObject, SPSConfigurator.AvatarArmature avatarArmature)
        {
            if (avatarObject.GetComponent<VRCAvatarDescriptor>() == null)
                throw new ArgumentException("avatarObject doesn't have a VRC Avatar Descriptor");
            this.avatarObject = avatarObject;
            this.avatarArmature = avatarArmature;
        }

        public GameObject SpsObject => spsParent.gameObject;

        public void Add(string name, Base.Offset offset, HumanBodyBones bone, string category = null,
            string blendshape = null, bool auto = false,
            VRCFuryHapticSocket.AddLight light = VRCFuryHapticSocket.AddLight.Auto)
        {
            var socket = CreateSocket(name, category, light, auto, blendshape);
            SetArmatureLinkedOffset(socket.gameObject, bone, offset);
            AddSocketToAvatar(socket.gameObject, category);
            socket.transform.localScale = Vector3.one;
        }

        public void AddParent(string name, Base.Offset offsetLeft, Base.Offset offsetRight, HumanBodyBones boneLeft,
            HumanBodyBones boneRight, string category = null,
            string blendshape = null, bool auto = false,
            VRCFuryHapticSocket.AddLight light = VRCFuryHapticSocket.AddLight.Auto)
        {
            var gameObject = new GameObject(name);
            var socket = CreateSocket(name, category, light, auto, blendshape);
            socket.gameObject.name = $"{name} Socket";
            socket.transform.SetParent(gameObject.transform, true);
            
            var targetLeft = new GameObject($"{name} Target Left");
            SetArmatureLinkedOffset(targetLeft, boneLeft, offsetLeft);
            targetLeft.transform.SetParent(gameObject.transform, true);
            
            var targetRight = new GameObject($"{name} Target Right");
            SetArmatureLinkedOffset(targetRight, boneRight, offsetRight);
            targetRight.transform.SetParent(gameObject.transform, true);

            SetParentConstraint(socket.gameObject, targetLeft.transform, targetRight.transform);
            AddSocketToAvatar(gameObject, category);
            targetRight.transform.localPosition =
                Vector3.Scale(targetRight.transform.localPosition, gameObject.transform.localScale);
            targetLeft.transform.localPosition =
                Vector3.Scale(targetLeft.transform.localPosition, gameObject.transform.localScale);
            gameObject.transform.localScale = Vector3.one;
        }

        public void AddCategoryIconSet(string category)
        {
            var catTransform = spsParent.Find(category);
            if (catTransform == null) return;
            var fury = catTransform.gameObject.AddComponent<VRCFury>();
            fury.Version = 2;
            fury.config.features.Add(new SetIcon()
            {
                path = $"{spsPath ?? "SPS"}/{category}",
            });
        }

        private VRCFuryHapticSocket CreateSocket(string name, string category, VRCFuryHapticSocket.AddLight light,
            bool auto, string blendshape)
        {
            var socket = new GameObject(name);
            VRCFuryHapticSocket socketVrcf = socket.AddComponent<VRCFuryHapticSocket>();
            socketVrcf.Version = 7;
            socketVrcf.addLight = light;

            socketVrcf.name = String.IsNullOrWhiteSpace(category) ? name : $"{category}/{name}";
            socketVrcf.enableAuto = auto;

            if (blendshape == null) return socketVrcf;

            var state = new State();
            state.actions.Add(new BlendShapeAction
            {
                blendShape = blendshape
            });
            socketVrcf.enableDepthAnimations = true;
            socketVrcf.depthActions.Add(new VRCFuryHapticSocket.DepthAction
            {
                state = state,
                enableSelf = true,
                startDistance = 0.05f,
                endDistance = 0,
                smoothingSeconds = 0,
            });
            return socketVrcf;
        }

        private void SetArmatureLinkedOffset(GameObject gameObject, HumanBodyBones bone, Base.Offset offset)
        {
            var vrcf = gameObject.AddComponent<VRCFury>();
            vrcf.Version = 2;
            vrcf.config.features.Add(new ArmatureLink()
            {
                propBone = vrcf.gameObject,
                boneOnAvatar = bone,
                Version = 5
            });
            var transform = avatarArmature.FindBone(bone);
            //gameObject.transform.position = transform.TransformPoint(offset.Positon);
            gameObject.transform.position = transform.position;
            gameObject.transform.rotation = transform.rotation;
            gameObject.transform.Translate(offset.Positon);
            gameObject.transform.Rotate(offset.EulerAngles);
        }

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