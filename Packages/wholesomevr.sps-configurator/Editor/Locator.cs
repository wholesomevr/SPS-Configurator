using System;
using System.Collections.Generic;
using System.Linq;
using com.vrcfury.api;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wholesome
{
    public class Locator : IDisposable
    {
        internal const float RaycastProxyWeldDistance = 0.004f;
        internal const float RaycastSegmentEpsilon = 0.001f;

        private GameObject avatarObject;
        public SkinnedMeshRenderer renderer;
        private Dictionary<HumanBodyBones, Transform> humanToBone;
        private Mesh bakedMesh;
        private Mesh raycastProxyMesh;
        private bool ownsRaycastProxyMesh;
        private Matrix4x4 bakedMeshToWorldMatrix;

        public Locator(GameObject avatarObject, SkinnedMeshRenderer renderer)
        {
            this.avatarObject = avatarObject;
            this.renderer = renderer;
            bakedMesh = BuildBakedMesh(renderer);
            bakedMeshToWorldMatrix = GetBakedMeshToWorldMatrix(renderer);
            raycastProxyMesh = BuildRaycastProxyMesh(bakedMesh, out ownsRaycastProxyMesh);
            this.humanToBone = new Dictionary<HumanBodyBones, Transform>();
            var humans = Enum.GetValues(typeof(HumanBodyBones));
            foreach (HumanBodyBones human in humans)
            {
                try
                {
                    var bone = FuryUtils.GetBone(avatarObject, human);
                    humanToBone.Add(human, bone.transform);
                }
                catch
                {

                }
            }
        }

        public struct Pose
        {
            public Vector3 Position;
            public Vector3 EulerAngles;
        }

        public void Dispose()
        {
            if (ownsRaycastProxyMesh && raycastProxyMesh != null)
            {
                UnityEngine.Object.DestroyImmediate(raycastProxyMesh);
                raycastProxyMesh = null;
            }
            if (bakedMesh != null)
            {
                var raycastProxyUsesBakedMesh = raycastProxyMesh == bakedMesh;
                UnityEngine.Object.DestroyImmediate(bakedMesh);
                if (raycastProxyUsesBakedMesh) raycastProxyMesh = null;
                bakedMesh = null;
            }
        }

        internal Vector3 AvatarToWorldPoint(Vector3 localPoint)
        {
            return avatarObject.transform.TransformPoint(localPoint);
        }

        internal Vector3 WorldToAvatarPoint(Vector3 worldPoint)
        {
            return avatarObject.transform.InverseTransformPoint(worldPoint);
        }

        internal Vector3 AvatarToWorldVector(Vector3 localVector)
        {
            return avatarObject.transform.rotation * localVector;
        }

        internal Vector3 AvatarToWorldDirection(Vector3 localDirection)
        {
            var worldDirection = AvatarToWorldVector(localDirection);
            return worldDirection.sqrMagnitude > Mathf.Epsilon ? worldDirection.normalized : Vector3.zero;
        }

        internal Quaternion AvatarToWorldRotation(Quaternion localRotation)
        {
            return avatarObject.transform.rotation * localRotation;
        }

        internal Quaternion AvatarLookRotation(Vector3 localForward)
        {
            return AvatarLookRotation(localForward, Vector3.up);
        }

        internal Quaternion AvatarLookRotation(Vector3 localForward, Vector3 localUp)
        {
            var worldForward = AvatarToWorldDirection(localForward);
            var worldUp = AvatarToWorldDirection(localUp);

            if (worldForward.sqrMagnitude <= Mathf.Epsilon) worldForward = avatarObject.transform.forward;
            if (worldUp.sqrMagnitude <= Mathf.Epsilon) worldUp = avatarObject.transform.up;

            if (Mathf.Abs(Vector3.Dot(worldForward, worldUp)) > 0.98f)
            {
                worldUp = AvatarToWorldDirection(Vector3.forward);
                if (Mathf.Abs(Vector3.Dot(worldForward, worldUp)) > 0.98f)
                {
                    worldUp = AvatarToWorldDirection(Vector3.right);
                }
            }

            return Quaternion.LookRotation(worldForward, worldUp);
        }

        internal Matrix4x4 AvatarTRS(Vector3 worldPosition, Quaternion localRotation)
        {
            return Matrix4x4.TRS(worldPosition, AvatarToWorldRotation(localRotation), Vector3.one);
        }

        public RaycastHit? Raycast(Vector3 origin, Vector3 target)
        {
            if (raycastProxyMesh == null) return null;

            var direction = target - origin;
            var maxDistance = direction.magnitude;
            if (maxDistance <= Mathf.Epsilon) return null;

            var hit = MeshRaycaster.Raycast(
                raycastProxyMesh,
                bakedMeshToWorldMatrix,
                new Ray(origin, direction),
                out var hitPoint,
                out var hitNormal,
                out var hitDistance);
            if (!hit || hitDistance > maxDistance + RaycastSegmentEpsilon) return null;

            return new RaycastHit
            {
                point = hitPoint,
                normal = hitNormal.normalized,
                distance = hitDistance
            };
        }

        public Vector3? RaycastTarget(Vector3 target, Vector3 offset)
        {
            var origin = offset;
            var direction = target - origin;
            var directionMagnitude = direction.magnitude;
            var maxDistance = directionMagnitude * 2f;
            if (raycastProxyMesh == null) return null;
            if (maxDistance <= Mathf.Epsilon) return null;

            var res = MeshRaycaster.Raycast(
                raycastProxyMesh,
                bakedMeshToWorldMatrix,
                new Ray(origin, direction),
                out var hitPoint,
                out var hitNormal,
                out var hitDistance);
            if (!res || hitDistance > maxDistance + RaycastSegmentEpsilon) return null;

            return hitPoint;
        }

        private static Mesh BuildBakedMesh(SkinnedMeshRenderer renderer)
        {
            if (renderer == null || renderer.sharedMesh == null) return null;

            var mesh = new Mesh
            {
                name = $"{renderer.sharedMesh.name} Baked Snapshot",
                hideFlags = HideFlags.HideAndDontSave
            };
            renderer.BakeMesh(mesh);
            return mesh;
        }

        private static Matrix4x4 GetBakedMeshToWorldMatrix(SkinnedMeshRenderer renderer)
        {
            if (renderer != null &&
                renderer.sharedMesh != null &&
                renderer.sharedMesh.boneWeights.Length > 0)
            {
                return Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);
            }

            return renderer != null ? renderer.localToWorldMatrix : Matrix4x4.identity;
        }

        private static Mesh BuildRaycastProxyMesh(Mesh bakedMesh, out bool ownsMesh)
        {
            ownsMesh = false;
            if (bakedMesh == null) return null;

            var weldedMesh = BuildWeldedRaycastMesh(bakedMesh, RaycastProxyWeldDistance);
            if (weldedMesh == null) return bakedMesh;

            ownsMesh = true;
            return weldedMesh;
        }

        private static Mesh BuildWeldedRaycastMesh(Mesh source, float weldDistance)
        {
            var verts = source.vertices;
            var tris = source.triangles;
            if (verts.Length == 0 || tris.Length == 0 || weldDistance <= 0f) return null;

            var sqrWeldDistance = weldDistance * weldDistance;
            var representatives = new List<Vector3>();
            var sums = new List<Vector3>();
            var counts = new List<int>();
            var remap = new int[verts.Length];
            var grid = new Dictionary<Vector3Int, List<int>>();

            Vector3Int GetCell(Vector3 pos)
            {
                return new Vector3Int(
                    Mathf.FloorToInt(pos.x / weldDistance),
                    Mathf.FloorToInt(pos.y / weldDistance),
                    Mathf.FloorToInt(pos.z / weldDistance));
            }

            for (var i = 0; i < verts.Length; i++)
            {
                var vertex = verts[i];
                var cell = GetCell(vertex);
                var found = -1;

                for (var x = -1; x <= 1 && found < 0; x++)
                {
                    for (var y = -1; y <= 1 && found < 0; y++)
                    {
                        for (var z = -1; z <= 1 && found < 0; z++)
                        {
                            var key = cell + new Vector3Int(x, y, z);
                            if (!grid.TryGetValue(key, out var candidates)) continue;

                            foreach (var candidate in candidates)
                            {
                                if ((representatives[candidate] - vertex).sqrMagnitude <= sqrWeldDistance)
                                {
                                    found = candidate;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (found < 0)
                {
                    found = representatives.Count;
                    representatives.Add(vertex);
                    sums.Add(Vector3.zero);
                    counts.Add(0);

                    if (!grid.TryGetValue(cell, out var entries))
                    {
                        entries = new List<int>();
                        grid[cell] = entries;
                    }
                    entries.Add(found);
                }

                remap[i] = found;
                sums[found] += vertex;
                counts[found]++;
            }

            var weldedVerts = new List<Vector3>(sums.Count);
            for (var i = 0; i < sums.Count; i++)
            {
                weldedVerts.Add(sums[i] / counts[i]);
            }

            var weldedTris = new List<int>(tris.Length);
            for (var i = 0; i < tris.Length; i += 3)
            {
                var a = remap[tris[i]];
                var b = remap[tris[i + 1]];
                var c = remap[tris[i + 2]];
                if (a == b || b == c || c == a) continue;

                weldedTris.Add(a);
                weldedTris.Add(b);
                weldedTris.Add(c);
            }

            if (weldedTris.Count == 0) return null;

            var weldedMesh = new Mesh
            {
                name = $"{source.name} Welded",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (weldedVerts.Count > 65535)
            {
                weldedMesh.indexFormat = IndexFormat.UInt32;
            }
            weldedMesh.SetVertices(weldedVerts);
            weldedMesh.SetTriangles(weldedTris, 0);
            weldedMesh.RecalculateNormals();
            weldedMesh.RecalculateBounds();
            return weldedMesh;
        }

        private Mesh GetPositionMesh(Mesh sourceMesh)
        {
            if (bakedMesh != null && sourceMesh != null && bakedMesh.vertexCount == sourceMesh.vertexCount)
            {
                return bakedMesh;
            }

            return sourceMesh;
        }

        private static Vector3[] GetPositionNormals(Mesh positionMesh, Mesh sourceMesh)
        {
            var sourceVertexCount = sourceMesh != null ? sourceMesh.vertexCount : 0;
            if (positionMesh != null)
            {
                var normals = positionMesh.normals;
                if (normals.Length == sourceVertexCount) return normals;
            }

            if (sourceMesh != null)
            {
                var normals = sourceMesh.normals;
                if (normals.Length == sourceVertexCount) return normals;
            }

            return Array.Empty<Vector3>();
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
            var positionMesh = GetPositionMesh(mesh);
            var deltaPositions = new Vector3[mesh.vertexCount];
            var blendshapeIdx = mesh.GetBlendShapeIndex(blendshape);
            mesh.GetBlendShapeFrameVertices(blendshapeIdx, 0, deltaPositions, null, null);
            var magnitudes = deltaPositions.Select(pos => pos.magnitude).ToArray();
            var sum = magnitudes.Sum();
            var weights = magnitudes.Select(mag => mag / sum).ToArray();
            var weightedPos = positionMesh.vertices.Zip(weights, (pos, weight) => pos * weight)
                .Aggregate(new Vector3(), (wpSum, wp) => wpSum + wp, (wp) => wp);
            var weightedPosWorld = bakedMeshToWorldMatrix.MultiplyPoint(weightedPos);
            return weightedPosWorld;
        }

        public bool RaycastToBlendshape(string blendshape, Vector3 normal, float distance, out Matrix4x4 mat)
        {
            var tgt = GetBlendshapeCenter(blendshape);
            tgt += AvatarToWorldVector(new Vector3(0, 0, 0.005f));
            var offset = tgt + AvatarToWorldDirection(normal) * distance;
            var hit = RaycastTarget(tgt, offset);
            mat = Matrix4x4.identity;
            if (hit == null) return false;
            mat = Matrix4x4.TRS((Vector3)hit, AvatarLookRotation(normal), Vector3.one);
            return true;
        }

        public bool RaycastToBlendshape(string blendshape, Vector3 normal, float distance, out Vector3 pos)
        {
            pos = Vector3.zero;
            var tgt = GetBlendshapeCenter(blendshape);
            // tgt.z += 0.005f;
            var offset = tgt + AvatarToWorldDirection(normal) * distance;
            var hit = RaycastTarget(tgt, offset);
            if (hit == null) return false;
            pos = (Vector3)hit;
            return true;
        }

        public Matrix4x4 GetLastPosInDir(HumanBodyBones humanBone, Vector3 dir)
        {
            var bone = FuryUtils.GetBone(avatarObject, humanBone);
            var bonePosition = bone.transform.position;
            var bonePositionLocal = WorldToAvatarPoint(bonePosition);
            var dirLocal = dir.sqrMagnitude > Mathf.Epsilon ? dir.normalized : Vector3.up;
            var boneIdx = Array.IndexOf(renderer.bones, bone.transform);
            var mesh = renderer.sharedMesh;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneWeights = mesh.GetAllBoneWeights();
            var positionMesh = GetPositionMesh(mesh);
            var verts = positionMesh.vertices;

            var vIdx = 0;
            var boneArrayIdx = 0;
            var bestProjection = float.MaxValue;
            var bestPosLocal = bonePositionLocal;
            var found = false;
            foreach (var bonesCount in bonesPerVertex)
            {
                for (var i = 0; i < bonesCount; i++)
                {
                    var bw = boneWeights[boneArrayIdx];
                    if (bw.boneIndex == boneIdx && bw.weight > 0.7)
                    {
                        var pos = verts[vIdx];
                        var posWorld = bakedMeshToWorldMatrix.MultiplyPoint(pos);
                        var posLocal = WorldToAvatarPoint(posWorld);
                        var diffLocal = bonePositionLocal - posLocal;
                        var perpendicular = diffLocal - Vector3.Project(diffLocal, dirLocal);
                        var projection = Vector3.Dot(posLocal, dirLocal);
                        if (projection < bestProjection && perpendicular.magnitude < 0.01f)
                        {
                            bestProjection = projection;
                            bestPosLocal = posLocal;
                            found = true;
                        }
                    }
                    boneArrayIdx++;
                }
                vIdx++;
            }

            var resultLocal = bonePositionLocal;
            if (found)
            {
                var along = Vector3.Dot(bestPosLocal - bonePositionLocal, dirLocal);
                resultLocal = bonePositionLocal + dirLocal * along;
            }
            return Matrix4x4.TRS(AvatarToWorldPoint(resultLocal), AvatarLookRotation(-dirLocal, Vector3.forward), Vector3.one);
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

            var boneIdces = new HashSet<int>();
            void traverse(Transform transform)
            {
                if (allButCurrentHumanBones.Contains(transform)) return;
                var boneIdx = Array.IndexOf(renderer.bones, transform);

                if (boneIdx >= 0) boneIdces.Add(boneIdx);
                foreach (Transform t in transform)
                {
                    traverse(t);
                }
            }
            traverse(bone.transform);

            var mesh = renderer.sharedMesh;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneWeights = mesh.GetAllBoneWeights();
            var positionMesh = GetPositionMesh(mesh);
            var verts = positionMesh.vertices;

            bool TryGetBest(bool useSideFilter, out Vector3 maxPos)
            {
                maxPos = bonePosition;
                var bestAxisValue = sign > 0 ? float.MinValue : float.MaxValue;
                var found = false;

                var vIdx = 0;
                var boneArrayIdx = 0;
                foreach (var bonesCount in bonesPerVertex)
                {
                    var pos = verts[vIdx];
                    var posWorld = bakedMeshToWorldMatrix.MultiplyPoint(pos);
                    var posLocal = WorldToAvatarPoint(posWorld);

                    vIdx++;

                    var totalWeight = 0f;
                    for (var i = 0; i < bonesCount; i++)
                    {
                        var bw = boneWeights[boneArrayIdx];
                        boneArrayIdx++;
                        if (!boneIdces.Contains(bw.boneIndex)) continue;
                        totalWeight += bw.weight;
                    }
                    if (totalWeight < weightThreshold) continue;
                    if (useSideFilter && xSide > 0 && posLocal.x < 0) continue;
                    if (useSideFilter && xSide < 0 && posLocal.x > 0) continue;

                    var axisValue = posLocal[(int)axis];
                    if (!found || (sign > 0 ? axisValue > bestAxisValue : axisValue < bestAxisValue))
                    {
                        bestAxisValue = axisValue;
                        maxPos = posWorld;
                        found = true;
                    }
                }

                return found;
            }

            var useSide = Mathf.Abs(xSide) > Mathf.Epsilon;
            if (TryGetBest(useSide, out var result)) return result;
            if (useSide && TryGetBest(false, out result)) return result;
            return bonePosition;
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
            var origin = hip.position + AvatarToWorldVector(new Vector3(0, -0.05f, 0));
            var direction = AvatarToWorldDirection(Vector3.up);
            var hit = Raycast(origin, origin + direction);
            return hit?.point;
        }

        public enum Axis
        {
            X = 0, Y = 1, Z = 2
        }

        public static (Axis, float) GetDominantAxisInDir(Vector3 axis, Matrix4x4 worldMat)
        {
            var axisDirection = NormalizeOrZero(axis);
            if (axisDirection.sqrMagnitude <= Mathf.Epsilon) return (Axis.X, 1);

            var sec = 0;
            var bestDot = float.MinValue;
            var res = 0f;
            for (var i = 0; i < 3; i++)
            {
                var column = GetNormalizedAxis(worldMat, (Axis)i);
                if (column.sqrMagnitude <= Mathf.Epsilon) continue;

                var res1 = Vector3.Dot(axisDirection, column);
                var dot = Math.Abs(res1);
                if (dot > bestDot)
                {
                    sec = i;
                    bestDot = dot;
                    res = res1;
                }
            }
            var sign = res >= 0 ? 1 : -1;
            return ((Axis)sec, sign);
            // return sec;
        }

        internal static Vector3 GetNormalizedAxis(Matrix4x4 matrix, Axis axis)
        {
            var column = matrix.GetColumn((int)axis);
            return NormalizeOrZero(new Vector3(column.x, column.y, column.z));
        }

        private static Vector3 NormalizeOrZero(Vector3 vector)
        {
            return vector.sqrMagnitude > Mathf.Epsilon ? vector.normalized : Vector3.zero;
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
            var positionMesh = GetPositionMesh(mesh);
            var verts = positionMesh.vertices;
            var normals = GetPositionNormals(positionMesh, mesh);
            if (normals.Length != mesh.vertexCount) return (bonePosition, normal.normalized);


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
                        var posWorld = bakedMeshToWorldMatrix.MultiplyPoint(pos);
                        var vNormalWorld = bakedMeshToWorldMatrix.MultiplyVector(vNormal);
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
            var xMin = WorldToAvatarPoint(GetMaxPosInAxis(human, Axis.X, -1));
            var xMax = WorldToAvatarPoint(GetMaxPosInAxis(human, Axis.X, 1));
            var yMin = WorldToAvatarPoint(GetMaxPosInAxis(human, Axis.Y, -1));
            var yMax = WorldToAvatarPoint(GetMaxPosInAxis(human, Axis.Y, 1));
            var zMin = WorldToAvatarPoint(GetMaxPosInAxis(human, Axis.Z, -1));
            var zMax = WorldToAvatarPoint(GetMaxPosInAxis(human, Axis.Z, 1));
            float midX = (xMin.x + xMax.x) * 0.5f;
            float midY = (yMin.y + yMax.y) * 0.5f;
            float midZ = (zMin.z + zMax.z) * 0.5f;
            return AvatarToWorldPoint(new Vector3(midX, midY, midZ));
        }

        public Vector3 GetAvgNormalCloseToPoint(Vector3 point, Vector3 normal, float angleThreshold, float maxDistance)
        {
            var mesh = renderer.sharedMesh;
            var positionMesh = GetPositionMesh(mesh);
            var verts = positionMesh.vertices;
            var normals = GetPositionNormals(positionMesh, mesh);
            if (normals.Length != mesh.vertexCount) return normal.normalized;
            var normalSum = Vector3.zero;
            var count = 0;
            for (var i = 0; i < mesh.vertexCount; i++)
            {
                var pos = verts[i];
                var posWorld = bakedMeshToWorldMatrix.MultiplyPoint(pos);
                var n = normals[i];
                var nWorld = bakedMeshToWorldMatrix.MultiplyVector(n);
                var dist = Vector3.Distance(posWorld, point);
                if (dist > maxDistance) continue;
                var deg = Vector3.Angle(nWorld, normal);
                if (deg > angleThreshold) continue;
                normalSum += nWorld;
                count++;
            }
            if (count == 0) return normal.normalized;
            var normalAvg = normalSum / count;
            return normalAvg.normalized;
        }
    }


}
