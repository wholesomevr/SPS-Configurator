using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.vrcfury.api;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace Wholesome
{
    public class Locator
    {
        private GameObject avatarObject;
        public SkinnedMeshRenderer renderer;
        private Dictionary<HumanBodyBones, Transform> humanToBone;

        public Locator(GameObject avatarObject, SkinnedMeshRenderer renderer)
        {
            this.avatarObject = avatarObject;
            this.renderer = renderer;
            this.humanToBone = new Dictionary<HumanBodyBones, Transform>();
            var humans = Enum.GetValues(typeof(HumanBodyBones));
            foreach (HumanBodyBones human in humans)
            {
                try
                {
                    var bone = FuryUtils.GetBone(avatarObject, human);
                    humanToBone.Add(human, bone.transform);
                }
                catch (Exception e)
                {

                }
            }
        }

        public struct Pose
        {
            public Vector3 Position;
            public Vector3 EulerAngles;
        }

        public static RaycastHit? Raycast(Ray ray, Mesh mesh, Matrix4x4 matrix)
        {
            var intersect = typeof(HandleUtility).GetMethod("IntersectRayMesh",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object[] rayParams =
            {
                ray, mesh, matrix, null
            };
            var result = (bool)intersect.Invoke(null, rayParams);
            if (result) return (RaycastHit)rayParams[3];
            return null;
        }

        public RaycastHit? Raycast(Vector3 origin, Vector3 target)
        {
            return Raycast(new Ray(origin, target - origin), renderer.sharedMesh, renderer.localToWorldMatrix);
        }

        public Vector3? RaycastTarget(Vector3 target, Vector3 offset)
        {
            var origin = offset;
            var direction = target - origin;
            var g1 = new GameObject("origin");
            var g2 = new GameObject("target");
            g1.transform.position = origin;
            g2.transform.position = target;
            var res = MeshRaycaster.Raycast(renderer.sharedMesh, renderer.localToWorldMatrix, new Ray(origin, direction), out var hitPoint, out var hitNormal, out var hitDistance);
            return hitPoint;
            // var hit = Raycast(new Ray(origin, direction), renderer.sharedMesh, renderer.localToWorldMatrix);
            // return hit?.point;
        }

        public static Vector3 GetCenter(params Vector3[] vectors)
        {
            var sum = Vector3.zero;
            foreach (var v in vectors)
            {
                sum += v;
            }
            return sum / vectors.Length;
        }

        public Vector3 GetBlendshapeCenter(string blendshape)
        {
            var mesh = renderer.sharedMesh;
            var deltaPositions = new Vector3[mesh.vertexCount];
            var blendshapeIdx = mesh.GetBlendShapeIndex(blendshape);
            mesh.GetBlendShapeFrameVertices(blendshapeIdx, 0, deltaPositions, null, null);
            var magnitudes = deltaPositions.Select(pos => pos.magnitude).ToArray();
            var sum = magnitudes.Sum();
            var weights = magnitudes.Select(mag => mag / sum).ToArray();
            var weightedPos = mesh.vertices.Zip(weights, (pos, weight) => pos * weight)
                .Aggregate(new Vector3(), (wpSum, wp) => wpSum + wp, (wp) => wp);
            // Debug.Assert(Mathf.Abs(weightedPos.x) < 0.01, "Blendshape not symmetric");
            var weightedPosWorld = renderer.localToWorldMatrix.MultiplyPoint(weightedPos);
            return weightedPosWorld;
        }

        public bool RaycastToBlendshape(string blendshape, Vector3 normal, float distance, out Matrix4x4 mat)
        {
            var tgt = GetBlendshapeCenter(blendshape);
            tgt.z += 0.005f;
            var offset = tgt + normal.normalized * distance;
            var hit = RaycastTarget(tgt, offset);
            mat = Matrix4x4.identity;
            if (hit == null) return false;
            mat = Matrix4x4.TRS((Vector3)hit, Quaternion.FromToRotation(Vector3.forward, normal), Vector3.one);
            return true;
        }

        public bool RaycastToBlendshape(string blendshape, Vector3 normal, float distance, out Vector3 pos)
        {
            pos = Vector3.zero;
            var tgt = GetBlendshapeCenter(blendshape);
            // tgt.z += 0.005f;
            var offset = tgt + normal.normalized * distance;
            var hit = RaycastTarget(tgt, offset);
            if (hit == null) return false;
            pos = (Vector3)hit;
            return true;
        }

        const float RAYCAST_OFFSET = 0.003f;
        public bool RaycastAroundToBlendshape(string blendshape, Vector3 normal, float distance, Axis axis, out Vector3 pos)
        {
            pos = Vector3.zero;
            var tgt = GetBlendshapeCenter(blendshape);
            // tgt.z += 0.005f;
            var offset = tgt + normal.normalized * distance;
            tgt[(int)axis] += RAYCAST_OFFSET;
            offset[(int)axis] += RAYCAST_OFFSET;
            
            var hit1 = RaycastTarget(tgt, offset);
            tgt[(int)axis] -= 2*RAYCAST_OFFSET;
            offset[(int)axis] -= 2*RAYCAST_OFFSET;
            var hit2 = RaycastTarget(tgt, offset);

            if (hit1 == null || hit2 == null) return false;
            pos = ((Vector3)hit1 + (Vector3)hit2) / 2;
            return true;
        }

        public Matrix4x4 GetLastPosInDir(HumanBodyBones humanBone, Vector3 dir)
        {
            var bone = FuryUtils.GetBone(avatarObject, humanBone);
            var bonePosition = bone.transform.position;
            var boneIdx = Array.IndexOf(renderer.bones, bone.transform);
            var mesh = renderer.sharedMesh;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneWeights = mesh.GetAllBoneWeights();
            var verts = mesh.vertices;

            var vIdx = 0;
            var boneArrayIdx = 0;
            var minY = float.MaxValue;
            foreach (var bonesCount in bonesPerVertex)
            {
                for (var i = 0; i < bonesCount; i++)
                {
                    var bw = boneWeights[boneArrayIdx];
                    if (bw.boneIndex == boneIdx && bw.weight > 0.7)
                    {
                        var pos = verts[vIdx];
                        var posWorld = renderer.localToWorldMatrix.MultiplyPoint(pos);
                        var diff = bonePosition - posWorld;
                        if (posWorld.y < minY && Math.Abs(diff.x) < 0.01 && Math.Abs(diff.z) < 0.01) minY = posWorld.y;
                    }
                    boneArrayIdx++;
                }
                vIdx++;
            }

            return Matrix4x4.TRS(new Vector3(bonePosition.x, minY, bonePosition.z), Quaternion.LookRotation(Vector3.down), Vector3.one);
            // return Matrix4x4.TRS(p, q, Vector3.one);
        }

        public Vector3 GetMaxPosInAxis(HumanBodyBones humanBone, Axis axis, float sign, float xSide = 0, float weightThreshold = 0.5f)
        {
            var bone = FuryUtils.GetBone(avatarObject, humanBone);
            var bonePosition = bone.transform.position;

            var allButCurrentHumanBones = new List<Transform>();
            foreach (var (human, t) in this.humanToBone)
            {
                if (human == humanBone) continue;
                allButCurrentHumanBones.Add(t);
            }

            var boneIdces = new List<int>();
            void traverse(Transform transform)
            {
                if (allButCurrentHumanBones.Contains(transform)) return;
                var boneIdx = Array.IndexOf(renderer.bones, transform);

                boneIdces.Add(boneIdx);
                foreach (Transform t in transform)
                {
                    traverse(t);
                }
            }
            traverse(bone.transform);

            var mesh = renderer.sharedMesh;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneWeights = mesh.GetAllBoneWeights();
            var verts = mesh.vertices;

            var maxPos = Vector3.zero;
            maxPos[(int)axis] = sign > 0 ? float.MinValue : float.MaxValue;
            //TODO: SET MAX POS CORRECTLY

            var vIdx = 0;
            var boneArrayIdx = 0;
            foreach (var bonesCount in bonesPerVertex)
            {
                var pos = verts[vIdx];
                var posWorld = renderer.localToWorldMatrix.MultiplyPoint(pos);

                vIdx++;

                var totalWeight = 0f;

                for (var i = 0; i < bonesCount; i++)
                {
                    var bw = boneWeights[boneArrayIdx];
                    boneArrayIdx++;
                    if (xSide == 1 && posWorld.x < 0) continue;
                    if (xSide == -1 && posWorld.x > 0) continue;
                    if (!boneIdces.Contains(bw.boneIndex)) continue;
                    totalWeight += bw.weight;

                }
                if (totalWeight < weightThreshold) continue;
                if (sign > 0 ? posWorld[(int)axis] > maxPos[(int)axis] : posWorld[(int)axis] < maxPos[(int)axis]) maxPos = posWorld;
            }

            return maxPos;
        }

        // public Vector3 GetMaxPosInDirAroundRadius(Matrix4x4 mat, Axis axis, float sign, float maxDistance)
        // {
        //     var bone = FuryUtils.GetBone(avatarObject, humanBone);
        //     var bonePosition = bone.transform.position;

        //     var allButCurrentHumanBones = new List<Transform>();
        //     foreach (var (human, t) in this.humanToBone)
        //     {
        //         if (human == humanBone) continue;
        //         allButCurrentHumanBones.Add(t);
        //     }

        //     var boneIdces = new List<int>();
        //     void traverse(Transform transform)
        //     {
        //         if (allButCurrentHumanBones.Contains(transform)) return;
        //         var boneIdx = Array.IndexOf(renderer.bones, transform);

        //         boneIdces.Add(boneIdx);
        //         foreach (Transform t in transform)
        //         {
        //             traverse(t);
        //         }
        //     }
        //     traverse(bone.transform);

        //     var mesh = renderer.sharedMesh;
        //     var bonesPerVertex = mesh.GetBonesPerVertex();
        //     var boneWeights = mesh.GetAllBoneWeights();
        //     var verts = mesh.vertices;

        //     var maxPos = Vector3.zero;
        //     maxPos[(int)axis] = sign > 0 ? float.MinValue : float.MaxValue;
        //     //TODO: SET MAX POS CORRECTLY

        //     var vIdx = 0;
        //     var boneArrayIdx = 0;
        //     foreach (var bonesCount in bonesPerVertex)
        //     {
        //         var pos = verts[vIdx];
        //         var posWorld = renderer.localToWorldMatrix.MultiplyPoint(pos);

        //         vIdx++;

        //         var totalWeight = 0f;

        //         for (var i = 0; i < bonesCount; i++)
        //         {
        //             var bw = boneWeights[boneArrayIdx];
        //             boneArrayIdx++;
        //             if (xSide == 1 && posWorld.x < 0) continue;
        //             if (xSide == -1 && posWorld.x > 0) continue;
        //             if (!boneIdces.Contains(bw.boneIndex)) continue;
        //             totalWeight += bw.weight;

        //         }
        //         if (totalWeight < weightThreshold) continue;
        //         if (sign > 0 ? posWorld[(int)axis] > maxPos[(int)axis] : posWorld[(int)axis] < maxPos[(int)axis]) maxPos = posWorld;
        //     }

        //     return maxPos;
        // }

        public Vector3? GetVagina()
        {
            var hip = FuryUtils.GetBone(avatarObject, HumanBodyBones.Hips).transform;
            var origin = hip.position + new Vector3(0, -0.05f, 0);
            var direction = Vector3.up;
            var hit = Raycast(new Ray(origin, direction), renderer.sharedMesh, renderer.localToWorldMatrix);
            return hit?.point;
        }

        public enum Axis
        {
            X = 0, Y = 1, Z = 2
        }

        public static (Axis, float) GetDominantAxisInDir(Vector3 axis, Matrix4x4 worldMat)
        {
            var sec = -1;
            var diff = float.MaxValue;
            var res = 0f;
            for (var i = 0; i < 3; i++)
            {
                var row = worldMat.GetColumn(i);
                var res1 = Vector3.Dot(axis, new Vector3(row.x, row.y, row.z));
                var diff1 = 1 - Math.Abs(res1);
                if (diff1 < diff)
                {
                    sec = i;
                    diff = diff1;
                    res = res1;
                }
            }
            var sign = res > 0 ? 1 : -1;
            return ((Axis)sec, sign);
            // return sec;
        }

        // TODO: get avg. normal dir
        public (Vector3, Vector3) GetCenterInNormalDirection(HumanBodyBones humanBone, Vector3 normal, float angleThreshold)
        {
            var bone = FuryUtils.GetBone(avatarObject, humanBone);
            var bonePosition = bone.transform.position;

            var allButCurrentHumanBones = new List<Transform>();
            foreach (var (human, t) in this.humanToBone)
            {
                if (human == humanBone) continue;
                allButCurrentHumanBones.Add(t);
            }

            var boneIdces = new List<int>();
            void traverse(Transform transform)
            {
                if (allButCurrentHumanBones.Contains(transform)) return;
                var boneIdx = Array.IndexOf(renderer.bones, transform);

                boneIdces.Add(boneIdx);
                foreach (Transform t in transform)
                {
                    traverse(t);
                }
            }
            traverse(bone.transform);

            var mesh = renderer.sharedMesh;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneWeights = mesh.GetAllBoneWeights();
            var verts = mesh.vertices;
            var normals = mesh.normals;


            var vList = new List<Vector3>();
            var vNormalList = new List<Vector3>();

            var vIdx = 0;
            var boneArrayIdx = 0;
            foreach (var bonesCount in bonesPerVertex)
            {
                for (var i = 0; i < bonesCount; i++)
                {
                    var bw = boneWeights[boneArrayIdx];
                    boneArrayIdx++;
                    if (boneIdces.Contains(bw.boneIndex) && bw.weight > 0.5)
                    {
                        var pos = verts[vIdx];
                        var vNormal = normals[vIdx];
                        var posWorld = renderer.localToWorldMatrix.MultiplyPoint(pos);
                        var vNormalWorld = renderer.localToWorldMatrix.MultiplyVector(vNormal);
                        var rad = Math.Acos(Vector3.Dot(vNormalWorld, normal));
                        var deg = rad * (180 / Math.PI);
                        if (deg < angleThreshold)
                        {
                            vList.Add(posWorld);
                            vNormalList.Add(vNormalWorld);
                        }
                    }
                }
                vIdx++;
            }

            var normalSum = Vector3.zero;
            foreach (var n in vNormalList)
            {
                normalSum += n;
            }
            var normalAvg = (normalSum / vNormalList.Count).normalized;

            // var xMax = vList.Max((pos) => pos.x);
            // var xMin = vList.Min((pos) => pos.x);
            // var zMax = vList.Max((pos) => pos.z);
            // var zMin = vList.Min((pos) => pos.z);

            // var xMid = (xMax - zMin) / 2 + xMin;
            // var zMid = (zMax - zMin) / 2 + zMin;

            // var midY = (xMid.y - zMid.y) / 2 + zMid.y;
            // --- component‑wise min / max ---
            float xMin = vList.Min(v => v.x);
            float xMax = vList.Max(v => v.x);

            float yMin = vList.Min(v => v.y);
            float yMax = vList.Max(v => v.y);

            float zMin = vList.Min(v => v.z);
            float zMax = vList.Max(v => v.z);

            // --- midpoint of the AABB that encloses all selected vertices ---
            float midX = (xMin + xMax) * 0.5f;
            float midY = (yMin + yMax) * 0.5f;
            float midZ = (zMin + zMax) * 0.5f;

            var zMinPos = vList.OrderBy(v => v.z).First();
            var zMaxPos = vList.OrderByDescending(v => v.z).First();
            return (new Vector3(midX, midY, midZ), (zMaxPos - zMinPos).normalized);

            // return new Vector3(xMid.x, midY, zMid.z);
        }

        public Vector3 GetAABBCenter(HumanBodyBones human)
        {
            var xMin = GetMaxPosInAxis(human, Axis.X, -1);
            var xMax = GetMaxPosInAxis(human, Axis.X, 1);
            var yMin = GetMaxPosInAxis(human, Axis.Y, -1);
            var yMax = GetMaxPosInAxis(human, Axis.Y, 1);
            var zMin = GetMaxPosInAxis(human, Axis.Z, -1);
            var zMax = GetMaxPosInAxis(human, Axis.Z, 1);
            float midX = (xMin.x + xMax.x) * 0.5f;
            float midY = (yMin.y + yMax.y) * 0.5f;
            float midZ = (zMin.z + zMax.z) * 0.5f;
            return new Vector3(midX, midY, midZ);
        }

        public Vector3 GetAvgNormalCloseToPoint(Vector3 point, Vector3 normal, float angleThreshold, float maxDistance)
        {
            var verts = renderer.sharedMesh.vertices;
            var normals = renderer.sharedMesh.normals;
            var normalSum = Vector3.zero;
            var count = 0;
            for (var i = 0; i < renderer.sharedMesh.vertexCount; i++)
            {
                var pos = verts[i];
                var posWorld = renderer.localToWorldMatrix.MultiplyPoint(pos);
                var n = normals[i];
                var nWorld = renderer.localToWorldMatrix.MultiplyVector(n);
                var dist = Vector3.Distance(posWorld, point);
                if (dist > maxDistance) continue;
                var deg = Vector3.Angle(nWorld, normal);
                if (deg > angleThreshold) continue;
                normalSum += nWorld;
                count++;
            }
            var normalAvg = normalSum / count;
            return normalAvg.normalized;
        }
    }


}