using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
    public class SocketConfig
    {
    }

    public class SocketBuilder
    {
        private GameObject avatarObject;
        private SPSConfigurator.AvatarArmature avatarArmature;
        private Dictionary<string, VRCFuryHapticSocket> existingSockets;
        private Dictionary<string, GuidTexture2d> icons;
        private string spsPath;
        private Transform spsParent;

        public SocketBuilder(GameObject avatarObject, SocketParser.Result parseResult = null)
        {
            if (avatarObject.GetComponent<VRCAvatarDescriptor>() == null)
                throw new ArgumentException("avatarObject doesn't have a VRC Avatar Descriptor");
            this.avatarObject = avatarObject;
            avatarArmature = new SPSConfigurator.AvatarArmature(avatarObject);
            existingSockets = parseResult?.Sockets;
            icons = parseResult?.CategoryIcons ?? new Dictionary<string, GuidTexture2d>();
            spsPath = parseResult?.SpsPath;
        }

        public IEnumerable<VRCFuryHapticSocket> ExistingSockets => existingSockets?.Values;

        public void SetExistingSockets(Dictionary<string, VRCFuryHapticSocket> sockets)
        {
            existingSockets = sockets;
        }

        public void Add(string name, Base.Offset offset, HumanBodyBones bone, string category = null,
            string blendshape = null, bool auto = false,
            VRCFuryHapticSocket.AddLight light = VRCFuryHapticSocket.AddLight.Auto)
        {
            var socket = CreateSocket(name, category, light, auto, blendshape);
            ClearArmatureLink(socket.gameObject); // if existing socket
            SetArmatureLinkedOffset(socket.gameObject, bone, offset);
            AddSocketToAvatar(socket.gameObject, category);
        }

        public void AddParent(string name, Base.Offset offsetLeft, Base.Offset offsetRight, HumanBodyBones boneLeft,
            HumanBodyBones boneRight, string category = null,
            string blendshape = null, bool auto = false,
            VRCFuryHapticSocket.AddLight light = VRCFuryHapticSocket.AddLight.Auto)
        {
            var gameObject = new GameObject(name);
            var socket = CreateSocket(name, category, light, auto, blendshape);
            socket.gameObject.name = $"{name} Socket";
            socket.transform.SetParent(gameObject.transform);
            /*var targetLeft = socket.transform.Find($"{name} Target Left")?.gameObject;
            var targetRight = socket.transform.Find($"{name} Target Right")?.gameObject;
            if (targetLeft == null || targetRight == null)
            {
                targetLeft = new GameObject($"{name} Target Left");
                SetArmatureLinkedOffset(targetLeft, boneLeft, offsetLeft);
                targetLeft.transform.SetParent(gameObject.transform, true);
                targetRight = new GameObject($"{name} Target Right");
                SetArmatureLinkedOffset(targetRight, boneRight, offsetRight);
                targetRight.transform.SetParent(gameObject.transform, true);
            }*/
            var targetLeft = new GameObject($"{name} Target Left");
            SetArmatureLinkedOffset(targetLeft, boneLeft, offsetLeft);
            targetLeft.transform.SetParent(gameObject.transform, true);
            var targetRight = new GameObject($"{name} Target Right");
            SetArmatureLinkedOffset(targetRight, boneRight, offsetRight);
            targetRight.transform.SetParent(gameObject.transform, true);
            if(!ValidateExistingParentConstraint(socket, targetLeft, targetRight))
                SetParentConstraint(socket.gameObject, targetLeft.transform, targetRight.transform);
            AddSocketToAvatar(gameObject, category);
        }

        private bool ValidateExistingParentConstraint(VRCFuryHapticSocket socket, GameObject targetLeft,
            GameObject targetRight)
        {
            var constraints = socket.GetComponents<ParentConstraint>();
            var hasValid = false;
            foreach (var constraint in constraints)
            {
                if (constraint.sourceCount == 2)
                {
                    if (constraint.GetSource(0).sourceTransform == targetLeft.transform &&
                        constraint.GetSource(1).sourceTransform == targetRight.transform)
                    {
                        hasValid = true;
                        break;
                    }
                    else
                    {
                        Object.DestroyImmediate(constraint);
                    }
                }
                else
                {
                    Object.DestroyImmediate(constraint);
                }
            }
            return hasValid;
        }

        private void ClearArmatureLink(GameObject socket)
        {
            var furies = socket.GetComponents<VRCFury>();
            foreach (var fury in furies)
            {
                fury.config.features.RemoveAll(f => f is ArmatureLink);
                if (fury.config.features.Count == 0) Object.DestroyImmediate(fury);
            }
        }

        public void AddCategoryIconSet(string category)
        {
            var catTransform = spsParent.Find(category);
            if (catTransform == null) return;
            var fury = catTransform.gameObject.AddComponent<VRCFury>();
            fury.config.features.Add(new SetIcon()
            {
                path = $"{spsPath ?? "SPS"}/{category}",
                icon = icons.TryGetValue(category, out var icon) ? icon : null
            });
        }

        private VRCFuryHapticSocket CreateSocket(string name, string category, VRCFuryHapticSocket.AddLight light,
            bool auto, string blendshape)
        {
            VRCFuryHapticSocket socketVrcf;
            if (existingSockets != null && existingSockets.TryGetValue(name, out socketVrcf))
            {
                existingSockets.Remove(name);
                socketVrcf.name = String.IsNullOrWhiteSpace(category) ? name : $"{category}/{name}";
                return socketVrcf;
            }

            var socket = new GameObject(name);
            socketVrcf = socket.AddComponent<VRCFuryHapticSocket>();
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
            vrcf.config.features.Add(new ArmatureLink()
            {
                propBone = vrcf.gameObject,
                boneOnAvatar = bone,
                Version = 5
            });
            var transform = avatarArmature.FindBone(bone);
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
                newSps.transform.SetParent(avatarObject.transform, false);
                spsParent = newSps.transform;
            }

            if (!String.IsNullOrWhiteSpace(category))
            {
                var categoryTranform = spsParent.Find(category);
                if (categoryTranform == null)
                {
                    var newCategory = new GameObject(category);
                    newCategory.transform.SetParent(spsParent);
                    categoryTranform = newCategory.transform;
                }

                socket.transform.SetParent(categoryTranform, false);
            }
            else
            {
                socket.transform.SetParent(spsParent, false);
            }
        }

        public Transform Get(string socketName, string category = null)
        {
            return spsParent.Find(category == null ? socketName : $"{category}/{socketName}");
        }

        public void Apply()
        {
        }
    }
}