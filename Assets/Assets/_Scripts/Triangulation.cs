using System.Collections.Generic;
using UnityEngine;

public struct Edge
{
    public int A, B;
    public Edge(int a, int b) { A = a; B = b; }

    public bool Equals(Edge other)
    {
        return (A == other.A && B == other.B) || (A == other.B && B == other.A);
    }
}

public struct Tri2D
{
    public int A, B, C;
    public Vector2 CircumCenter;
    public float CircumRadiusSq;

    public Tri2D(int a, int b, int c, List<Vector2> points)
    {
        A = a; B = b; C = c;

        Vector2 pA = points[A];
        Vector2 pB = points[B];
        Vector2 pC = points[C];

        float dA = pA.x * pA.x + pA.y * pA.y;
        float dB = pB.x * pB.x + pB.y * pB.y;
        float dC = pC.x * pC.x + pC.y * pC.y;

        float D = 2 * (pA.x * (pB.y - pC.y) + pB.x * (pC.y - pA.y) + pC.x * (pA.y - pB.y));

        if (Mathf.Abs(D) < Mathf.Epsilon)
        {
            CircumCenter = (pA + pB + pC) / 3f;
            CircumRadiusSq = float.MaxValue;
        }
        else
        {
            float ux = (dA * (pB.y - pC.y) + dB * (pC.y - pA.y) + dC * (pA.y - pB.y)) / D;
            float uy = (dA * (pC.x - pB.x) + dB * (pA.x - pC.x) + dC * (pB.x - pA.x)) / D;

            CircumCenter = new Vector2(ux, uy);
            CircumRadiusSq = (pA.x - ux) * (pA.x - ux) + (pA.y - uy) * (pA.y - uy);
        }
    }
}

public static class Triangulation
{
    /// <summary>
    /// Generates a Delaunay triangulation for a set of points within a board's boundary.
    /// </summary>
    public static List<Triangle> Generate(List<Vector3> points, Transform board)
    {
        List<Vector2> points2D = new List<Vector2>();
        List<Vector3> points3D = new List<Vector3>();

        // 1. Add 16 boundary points (indices 0 to 15)
        Vector3[] boundary = GetBoundaryPoints(board);
        foreach (var p in boundary)
        {
            Vector3 local = board.InverseTransformPoint(p);
            points2D.Add(new Vector2(local.x, local.z));
            points3D.Add(p);
        }

        // 2. Add super triangle dummy points (indices 16, 17, 18)
        points2D.Add(new Vector2(-500, -500));
        points2D.Add(new Vector2(500, -500));
        points2D.Add(new Vector2(0, 500));
        points3D.Add(Vector3.zero);
        points3D.Add(Vector3.zero);
        points3D.Add(Vector3.zero);

        // 3. Add user clicked points (starting from index 19)
        foreach (var p in points)
        {
            Vector3 local = board.InverseTransformPoint(p);
            points2D.Add(new Vector2(local.x, local.z));
            points3D.Add(p);
        }

        // 4. Run Triangulation
        List<int> indices = DelaunayTriangulate(points2D);

        // 5. Convert indices to 3D triangles
        List<Triangle> tris = new List<Triangle>();
        for (int i = 0; i < indices.Count; i += 3)
        {
            int a = indices[i];
            int b = indices[i + 1];
            int c = indices[i + 2];

            // Ignore any triangles that use the super triangle dummy points
            if (a >= 16 && a <= 18) continue;
            if (b >= 16 && b <= 18) continue;
            if (c >= 16 && c <= 18) continue;

            tris.Add(new Triangle(points3D[a], points3D[b], points3D[c]));
        }

        return tris;
    }

    // --- Internal Geometric Algorithms ---

    private static List<int> DelaunayTriangulate(List<Vector2> points)
    {
        if (points.Count < 3) return new List<int>();

        List<Tri2D> triangles = new List<Tri2D>();

        // Add super triangle (indices 16, 17, 18)
        triangles.Add(new Tri2D(16, 17, 18, points));

        // Delaunay triangulate the 16 boundary points
        for (int pIdx = 0; pIdx < 16; pIdx++)
        {
            InsertPointDelaunay(pIdx, triangles, points);
        }

        // Remove super triangle
        triangles.RemoveAll(t => t.A >= 16 || t.B >= 16 || t.C >= 16);

        // Insert user clicked points (indices 19 to count-1) using Standard Delaunay
        for (int pIdx = 19; pIdx < points.Count; pIdx++)
        {
            InsertPointDelaunay(pIdx, triangles, points);
        }

        // Extract indices
        List<int> indices = new List<int>();
        foreach (var t in triangles)
        {
            indices.Add(t.A);
            indices.Add(t.B);
            indices.Add(t.C);
        }

        return indices;
    }

    private static void InsertPointDelaunay(int pIdx, List<Tri2D> triangles, List<Vector2> points)
    {
        Vector2 p = points[pIdx];
        List<Tri2D> badTris = new List<Tri2D>();

        // Find triangles whose circumcircle contains the new point
        foreach (var t in triangles)
        {
            float distSq = (p.x - t.CircumCenter.x) * (p.x - t.CircumCenter.x) +
                           (p.y - t.CircumCenter.y) * (p.y - t.CircumCenter.y);

            if (distSq < t.CircumRadiusSq + 0.0001f)
            {
                badTris.Add(t);
            }
        }

        // Find the boundary of the hole created by removing bad triangles
        List<Edge> polygon = new List<Edge>();
        foreach (var t in badTris)
        {
            polygon.Add(new Edge(t.A, t.B));
            polygon.Add(new Edge(t.B, t.C));
            polygon.Add(new Edge(t.C, t.A));
        }

        List<Edge> boundary = new List<Edge>();
        for (int i = 0; i < polygon.Count; i++)
        {
            bool shared = false;
            for (int j = 0; j < polygon.Count; j++)
            {
                if (i == j) continue;
                if (polygon[i].Equals(polygon[j]))
                {
                    shared = true;
                    break;
                }
            }
            if (!shared) boundary.Add(polygon[i]);
        }

        // Remove old triangles and create new ones using the point and boundary edges
        foreach (var t in badTris) triangles.Remove(t);

        foreach (var e in boundary)
        {
            triangles.Add(new Tri2D(e.A, e.B, pIdx, points));
        }
    }

    private static Vector3[] GetBoundaryPoints(Transform board)
    {
        float size = 5f;
        int segmentsPerEdge = 4; // 16 total points

        Vector3 c0 = board.TransformPoint(new Vector3(-size, 0, -size));
        Vector3 c1 = board.TransformPoint(new Vector3(size, 0, -size));
        Vector3 c2 = board.TransformPoint(new Vector3(size, 0, size));
        Vector3 c3 = board.TransformPoint(new Vector3(-size, 0, size));

        List<Vector3> boundary = new List<Vector3>();

        for (int i = 0; i < segmentsPerEdge; i++) boundary.Add(Vector3.Lerp(c0, c1, (float)i / segmentsPerEdge));
        for (int i = 0; i < segmentsPerEdge; i++) boundary.Add(Vector3.Lerp(c1, c2, (float)i / segmentsPerEdge));
        for (int i = 0; i < segmentsPerEdge; i++) boundary.Add(Vector3.Lerp(c2, c3, (float)i / segmentsPerEdge));
        for (int i = 0; i < segmentsPerEdge; i++) boundary.Add(Vector3.Lerp(c3, c0, (float)i / segmentsPerEdge));

        return boundary.ToArray();
    }
}