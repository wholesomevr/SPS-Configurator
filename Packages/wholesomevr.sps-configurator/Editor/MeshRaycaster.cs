using UnityEngine;


public static class MeshRaycaster
{
    /// <returns>True if the ray hits the mesh.  The nearest hit is returned.</returns>
    public static bool Raycast(
        Mesh mesh,            // must be marked “Read/Write” in the importer
        Matrix4x4 worldMat,   // transform that places the mesh in the scene
        Ray worldRay,        // ray in world space
        out Vector3 hitPoint,      // hit position (world)
        out Vector3 hitNormal,     // interpolated normal (world)
        out float hitDistance)    // distance from ray.origin to hitPoint
    {
        hitPoint = Vector3.zero;
        hitNormal = Vector3.zero;
        hitDistance = float.MaxValue;

        // 1. Bring the ray into the mesh’s local space
        var localMat = worldMat.inverse;
        Vector3 localOrigin = localMat.MultiplyPoint(worldRay.origin);
        Vector3 localDirection = localMat.MultiplyVector(worldRay.direction).normalized;

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        Vector3[] normals = mesh.normals;      // may be empty
        bool haveNorms = normals.Length == verts.Length;

        bool hit = false;

        // 2. Test every triangle
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];

            if (RayIntersectsTriangleRobust(localOrigin, localDirection, v0, v1, v2,
                                      out float t, out float u, out float v))
            {
                if (t < hitDistance && t > 0f)     // keep nearest positive hit
                {
                    // 3. Convert hit to world space
                    Vector3 localHit = localOrigin + localDirection * t;
                    Vector3 worldHit = worldMat.MultiplyPoint(localHit);

                    // 4. Figure out the surface normal
                    Vector3 n0, n1, n2;
                    if (haveNorms)
                    {
                        n0 = normals[tris[i]];
                        n1 = normals[tris[i + 1]];
                        n2 = normals[tris[i + 2]];
                    }
                    else
                    {
                        // geometric normal
                        n0 = n1 = n2 = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                    }
                    Vector3 localNormal = ((1 - u - v) * n0 + u * n1 + v * n2).normalized;
                    Vector3 worldNormal = worldMat.MultiplyVector(localNormal);

                    // 5. Store best hit so far
                    hitPoint = worldHit;
                    hitNormal = worldNormal;
                    hitDistance = Vector3.Distance(worldRay.origin, worldHit);
                    hit = true;
                }
            }
        }
        return hit;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Möller‑Trumbore intersection (local‑space)
    private static bool RayIntersectsTriangle(
        Vector3 O, Vector3 D,
        Vector3 V0, Vector3 V1, Vector3 V2,
        out float t, out float u, out float v)
    {
        t = u = v = 0f;
        const float EPS = 1e-6f;

        Vector3 e1 = V1 - V0;
        Vector3 e2 = V2 - V0;
        Vector3 P = Vector3.Cross(D, e2);
        float det = Vector3.Dot(e1, P);

        if (det > -EPS && det < EPS) return false;   // parallel

        float invDet = 1f / det;
        Vector3 S = O - V0;

        u = Vector3.Dot(S, P) * invDet;
        if (u < 0f || u > 1f) return false;

        Vector3 Q = Vector3.Cross(S, e1);
        v = Vector3.Dot(D, Q) * invDet;
        if (v < 0f || u + v > 1f) return false;

        t = Vector3.Dot(e2, Q) * invDet;
        return t > EPS;
    }

    private static bool RayIntersectsTriangleRobust(
    Vector3 O, Vector3 D,
    Vector3 V0, Vector3 V1, Vector3 V2,
    out float t, out float u, out float v)
    {
        t = u = v = 0f;

        // Tunables
        const double BASE_EPS = 1e-12;   // gets scaled by triangle size
        const double BARY_EPS = 1e-8;    // slack for barycentric edge cases
        const double TMIN = 1e-6;    // ignore hits extremely close to origin

        // --- Promote everything to double (component-wise) -------------------------
        double ox = O.x, oy = O.y, oz = O.z;
        double dx = D.x, dy = D.y, dz = D.z;

        double v0x = V0.x, v0y = V0.y, v0z = V0.z;
        double v1x = V1.x, v1y = V1.y, v1z = V1.z;
        double v2x = V2.x, v2y = V2.y, v2z = V2.z;

        // Edges
        double e1x = v1x - v0x, e1y = v1y - v0y, e1z = v1z - v0z;
        double e2x = v2x - v0x, e2y = v2y - v0y, e2z = v2z - v0z;

        // P = D x e2
        double px = dy * e2z - dz * e2y;
        double py = dz * e2x - dx * e2z;
        double pz = dx * e2y - dy * e2x;

        // det = dot(e1, P)
        double det = e1x * px + e1y * py + e1z * pz;

        // Scale-aware epsilon (prevents large/small triangles from breaking the test)
        double scale = (e1x * e1x + e1y * e1y + e1z * e1z) +
                       (e2x * e2x + e2y * e2y + e2z * e2z);
        double epsDet = BASE_EPS * scale;

        if (System.Math.Abs(det) < epsDet)
        {
            return false;
            // --- Coplanar / near-parallel fallback --------------------------------
            // Plane normal (double)
            double nx = e1y * e2z - e1z * e2y;
            double ny = e1z * e2x - e1x * e2z;
            double nz = e1x * e2y - e1y * e2x;

            double denom = nx * dx + ny * dy + nz * dz;
            if (System.Math.Abs(denom) < 1e-15)  // Truly parallel to the plane
                return false;

            double tD = (nx * (v0x - ox) + ny * (v0y - oy) + nz * (v0z - oz)) / denom;
            if (tD <= TMIN)
                return false;

            // Intersection point
            double pxw = ox + dx * tD;
            double pyw = oy + dy * tD;
            double pzw = oz + dz * tD;

            // Project to the dominant axis plane (drop the largest normal component)
            double anx = System.Math.Abs(nx);
            double any = System.Math.Abs(ny);
            double anz = System.Math.Abs(nz);

            int drop; // 0 -> drop x, 1 -> y, 2 -> z
            if (anx > any && anx > anz) drop = 0;
            else if (any > anz) drop = 1;
            else drop = 2;

            // Helper to pick 2D coords depending on drop axis
            double ax0, ay0, ax1, ay1, ax2, ay2, apx, apy;
            if (drop == 0) // drop x -> use (y,z)
            {
                ax0 = v0y; ay0 = v0z;
                ax1 = v1y; ay1 = v1z;
                ax2 = v2y; ay2 = v2z;
                apx = pyw; apy = pzw;
            }
            else if (drop == 1) // drop y -> use (x,z)
            {
                ax0 = v0x; ay0 = v0z;
                ax1 = v1x; ay1 = v1z;
                ax2 = v2x; ay2 = v2z;
                apx = pxw; apy = pzw;
            }
            else // drop z -> use (x,y)
            {
                ax0 = v0x; ay0 = v0y;
                ax1 = v1x; ay1 = v1y;
                ax2 = v2x; ay2 = v2y;
                apx = pxw; apy = pyw;
            }

            // 2D barycentric (stable for coplanar case)
            double denom2D = (ay1 - ay2) * (ax0 - ax2) + (ax2 - ax1) * (ay0 - ay2);
            if (System.Math.Abs(denom2D) < 1e-30)
                return false; // degenerate triangle

            double u2D = ((ay1 - ay2) * (apx - ax2) + (ax2 - ax1) * (apy - ay2)) / denom2D;
            double v2D = ((ay2 - ay0) * (apx - ax2) + (ax0 - ax2) * (apy - ay2)) / denom2D;
            double w2D = 1.0 - u2D - v2D;

            if (u2D < -BARY_EPS || v2D < -BARY_EPS || w2D < -BARY_EPS)
                return false;

            t = (float)tD;
            u = (float)u2D;
            v = (float)v2D;
            return true;
        }

        // --- Regular Möller–Trumbore path (double precision math) -----------------
        double invDet = 1.0 / det;

        // S = O - V0
        double sx = ox - v0x, sy = oy - v0y, sz = oz - v0z;

        // u = dot(S, P) * invDet
        double uD = (sx * px + sy * py + sz * pz) * invDet;
        if (uD < -BARY_EPS || uD > 1.0 + BARY_EPS)
            return false;

        // Q = S x e1
        double qx = sy * e1z - sz * e1y;
        double qy = sz * e1x - sx * e1z;
        double qz = sx * e1y - sy * e1x;

        // v = dot(D, Q) * invDet
        double vD = (dx * qx + dy * qy + dz * qz) * invDet;
        if (vD < -BARY_EPS || (uD + vD) > 1.0 + BARY_EPS)
            return false;

        // t = dot(e2, Q) * invDet
        double tD2 = (e2x * qx + e2y * qy + e2z * qz) * invDet;
        if (tD2 <= TMIN)
            return false;

        t = (float)tD2;
        u = (float)uD;
        v = (float)vD;
        return true;
    }
}