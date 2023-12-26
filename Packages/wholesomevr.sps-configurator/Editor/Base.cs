using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Wholesome
{
    public enum FootType
    {
        Flat,
        Heeled
    }

    public enum Side
    {
        Left,
        Right
    }

    public static class Names
    {
        public static readonly string BlowjobName = "Blowjob";
        public static readonly string HandjobName = "Handjob";
        public static readonly string HandjobDoubleName = $"Double {HandjobName}";
        public static readonly string HandjobLeftName = $"{HandjobName} Left";
        public static readonly string HandjobRightName = $"{HandjobName} Right";
        public static readonly string PussyName = "Pussy";
        public static readonly string AnalName = "Anal";
        public static readonly string TitjobName = "Titjob";
        public static readonly string AssjobName = "Assjob";
        public static readonly string ThighjobName = "Thighjob";
        public static readonly string SoleName = "Steppies";
        public static readonly string SoleLeftName = $"{SoleName} Left";
        public static readonly string SoleRightName = $"{SoleName} Right";
        public static readonly string FootjobName = "Footjob";

        public static string[] All => new[]
        {
            BlowjobName, HandjobDoubleName, HandjobLeftName, HandjobRightName, PussyName, AnalName, TitjobName,
            AssjobName, ThighjobName, SoleLeftName, SoleRightName, FootjobName
        };
    }

    public class Base
    {
        public string Name;
        public Offset Hand;
        public Offset Pussy;
        public Offset Anal;
        public Offset Titjob;
        public Offset Assjob;
        public Offset Thighjob;
        public Offset SoleFlat;
        public Offset SoleHeeled;
        public Offset FootjobFlat;

        public Offset FootjobHeeled;
        //TODO: Armpit? Pussyrub

        public string PussyBlendshape;
        public string AnalBlendshape;

        public float DefaultHipLength;
        public float DefaultTorsoLength;
        
        public Offset AlignedHandLeft = null;

        public class Offset
        {
            public Vector3 Positon;
            public Vector3 EulerAngles;

            public Offset(Vector3 positon, Vector3 eulerAngles)
            {
                Positon = positon;
                EulerAngles = eulerAngles;
            }

            public void Scale(float scale)
            {
                Positon.Scale(new Vector3(scale, scale, scale));
            }
        }

        public Offset HandLeft => AlignedHandLeft;
        public Offset HandRight => Hand;
        public Offset ThighjobLeft => Thighjob;
        public Offset ThighjobRight => Thighjob;

        public Offset GetFootjob(Side side, FootType footType)
        {
            switch (footType)
            {
                case FootType.Flat:
                    return FootjobFlat;
                case FootType.Heeled:
                    return FootjobHeeled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(footType), footType, null);
            }
        }

        public Offset GetSole(Side side, FootType footType)
        {
            switch (footType)
            {
                case FootType.Flat:
                    return SoleFlat;
                case FootType.Heeled:
                    return SoleHeeled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(footType), footType, null);
            }
        }

        public Base DeepCopy()
        {
            return new Base
            {
                Name = String.Copy(Name),
                Hand =  new Offset(Hand.Positon, Hand.EulerAngles),
                Pussy = new Offset(Pussy.Positon, Pussy.EulerAngles),
                Anal = new Offset(Anal.Positon, Anal.EulerAngles),
                Titjob = new Offset(Titjob.Positon, Titjob.EulerAngles),
                Assjob = new Offset(Assjob.Positon, Assjob.EulerAngles),
                Thighjob = new Offset(Thighjob.Positon, Thighjob.EulerAngles),
                SoleFlat = new Offset(SoleFlat.Positon, SoleFlat.EulerAngles),
                SoleHeeled = new Offset(SoleHeeled.Positon, SoleHeeled.EulerAngles),
                FootjobFlat = new Offset(FootjobFlat.Positon, FootjobFlat.EulerAngles),
                FootjobHeeled = new Offset(FootjobHeeled.Positon, FootjobHeeled.EulerAngles),
                PussyBlendshape = PussyBlendshape == null ? null : String.Copy(PussyBlendshape),
                AnalBlendshape = AnalBlendshape == null ? null : String.Copy(AnalBlendshape),
                DefaultHipLength = DefaultHipLength,
                DefaultTorsoLength = DefaultTorsoLength
            };
        }

        private IEnumerable<Offset> All() =>
            new[]
            {
                Hand, Pussy, Anal, Titjob, Assjob, Thighjob, SoleFlat, SoleHeeled,
                FootjobFlat, FootjobHeeled
            };

        public void Scale(float scale)
        {
            foreach (var offset in All())
            {
                offset.Scale(scale);
            }
        }

        public Offset GetMouth(VRCAvatarDescriptor avatarDescriptor, Transform head)
        {
            var defaultPos = new Offset(
                new Vector3(0, 0.01f, 0.075f),
                new Vector3());
            if (avatarDescriptor.lipSync != VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape) return defaultPos;
            if (avatarDescriptor.VisemeBlendShapes == null) return defaultPos;
            if (avatarDescriptor.VisemeBlendShapes.Length <= (int)VRC_AvatarDescriptor.Viseme.oh) return defaultPos;
            var blendshapeName = avatarDescriptor.VisemeBlendShapes[(int)VRC_AvatarDescriptor.Viseme.oh];
            if (string.IsNullOrEmpty(blendshapeName)) return defaultPos;
            if (avatarDescriptor.VisemeSkinnedMesh == null) return defaultPos;
            var blendshapeIndex = avatarDescriptor.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(blendshapeName);
            var mouthPosition = DetectMouthPosition(avatarDescriptor.VisemeSkinnedMesh, blendshapeIndex, head);
            return new Offset(
                head.InverseTransformPoint(mouthPosition),
                new Vector3());
        }
        
        private Vector3 DetectMouthPosition(SkinnedMeshRenderer head, int ohBlendshapeIndex, Transform headBone)
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

        public void AlignHands(SPSConfigurator.AvatarArmature armature)
        {
            Vector3 Align(Transform transform)
            {
                var alignedX = Vector3.Cross(transform.up, Vector3.up);
                var sign = Mathf.Sign(Vector3.Dot(transform.up, Vector3.right));
                var delta = new Vector3(0, sign * Vector3.Angle(transform.right, alignedX), 0);
                return delta;
            }
            
            var rightHandTransform = armature.FindBone(HumanBodyBones.RightHand);
            var deltaRightHand = Align(rightHandTransform);
            var leftHandTransform = armature.FindBone(HumanBodyBones.LeftHand);
            var deltaLeftHand = Align(leftHandTransform);
            AlignedHandLeft = new Offset(Hand.Positon, Vector3.Scale(Hand.EulerAngles, new Vector3(1, -1, 1)) + deltaLeftHand);
            Hand.EulerAngles += deltaRightHand;
        }
    }
}