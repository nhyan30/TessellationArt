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
    public static List<Triangle> Generate(List<Vector3> points, Transform board)
    {
        List<Vector2> points2D = new List<Vector2>();
        List<Vector3> points3D = new List<Vector3>();

        // 1. Add 16 boundary points
        Vector3[] boundary = GetBoundaryPoints(board);
        foreach (var p in boundary)
        {
            Vector3 local = board.InverseTransformPoint(p);
            points2D.Add(new Vector2(local.x, local.z));
            points3D.Add(p);
        }

        // 2. Add super triangle dummy points (indices 16, 17, 18)
        points2D.Add(new Vector2(-100, -100));
        points2D.Add(new Vector2(100, -100));
        points2D.Add(new Vector2(0, 100));
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

    static List<int> DelaunayTriangulate(List<Vector2> points)
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

        // Insert user clicked points (indices 19 to count-1) using Hexagonal Hole
        for (int pIdx = 19; pIdx < points.Count; pIdx++)
        {
            InsertPoint(pIdx, triangles, points);
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

    // Forces exactly 6 connections for internal hits by removing a hexagonal hole
    static void InsertPoint(int pIdx, List<Tri2D> triangles, List<Vector2> points)
    {
        Vector2 p = points[pIdx];
        Tri2D? centerTri = null;

        // Find the triangle containing the point
        foreach (var t in triangles)
        {
            if (PointInsideTriangle(p, t, points))
            {
                centerTri = t;
                break;
            }
        }

        if (centerTri == null)
        {
            // Fallback to Delaunay if we missed (e.g., exactly on edge or numerical precision)
            InsertPointDelaunay(pIdx, triangles, points);
            return;
        }

        Tri2D T0 = centerTri.Value;

        // Find neighbors of T0
        List<Tri2D> neighbors = new List<Tri2D>();
        Edge[] edgesT0 = {
            new Edge(T0.A, T0.B),
            new Edge(T0.B, T0.C),
            new Edge(T0.C, T0.A)
        };

        foreach (var t in triangles)
        {
            if (IsSameTriangle(t, T0)) continue;

            bool isNeighbor = false;
            foreach (var e in edgesT0)
            {
                if (new Edge(t.A, t.B).Equals(e) ||
                    new Edge(t.B, t.C).Equals(e) ||
                    new Edge(t.C, t.A).Equals(e))
                {
                    isNeighbor = true;
                    break;
                }
            }
            if (isNeighbor) neighbors.Add(t);
        }

        List<Tri2D> toRemove = new List<Tri2D> { T0 };
        toRemove.AddRange(neighbors);

        List<Edge> polygon = new List<Edge>();
        foreach (var t in toRemove)
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

        foreach (var t in toRemove) triangles.Remove(t);

        // Connect the new point to the hexagonal (or polygonal) hole
        foreach (var e in boundary)
        {
            triangles.Add(new Tri2D(e.A, e.B, pIdx, points));
        }
    }

    // Standard Delaunay insertion used for the initial boundary generation
    static void InsertPointDelaunay(int pIdx, List<Tri2D> triangles, List<Vector2> points)
    {
        Vector2 p = points[pIdx];
        List<Tri2D> badTris = new List<Tri2D>();

        foreach (var t in triangles)
        {
            float distSq = (p.x - t.CircumCenter.x) * (p.x - t.CircumCenter.x) +
                           (p.y - t.CircumCenter.y) * (p.y - t.CircumCenter.y);

            if (distSq < t.CircumRadiusSq + 0.0001f)
            {
                badTris.Add(t);
            }
        }

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

        foreach (var t in badTris) triangles.Remove(t);

        foreach (var e in boundary)
        {
            triangles.Add(new Tri2D(e.A, e.B, pIdx, points));
        }
    }

    static bool PointInsideTriangle(Vector2 p, Tri2D t, List<Vector2> points)
    {
        Vector2 v0 = points[t.C] - points[t.A];
        Vector2 v1 = points[t.B] - points[t.A];
        Vector2 v2 = p - points[t.A];

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= -0.001f) && (v >= -0.001f) && (u + v <= 1.001f); // Small epsilon for edge hits
    }

    static bool IsSameTriangle(Tri2D a, Tri2D b)
    {
        return (a.A == b.A || a.A == b.B || a.A == b.C) &&
               (a.B == b.A || a.B == b.B || a.B == b.C) &&
               (a.C == b.A || a.C == b.B || a.C == b.C);
    }

    static Vector3[] GetBoundaryPoints(Transform board)
    {
        float size = 5f;

        // Corners
        Vector3 c0 = board.TransformPoint(new Vector3(-size, 0, -size));
        Vector3 c1 = board.TransformPoint(new Vector3(size, 0, -size));
        Vector3 c2 = board.TransformPoint(new Vector3(size, 0, size));
        Vector3 c3 = board.TransformPoint(new Vector3(-size, 0, size));

        List<Vector3> boundary = new List<Vector3>();

        // Bottom edge (c0 to c1)
        boundary.Add(c0);
        boundary.Add(Vector3.Lerp(c0, c1, 0.25f));
        boundary.Add(Vector3.Lerp(c0, c1, 0.5f));
        boundary.Add(Vector3.Lerp(c0, c1, 0.75f));

        // Right edge (c1 to c2)
        boundary.Add(c1);
        boundary.Add(Vector3.Lerp(c1, c2, 0.25f));
        boundary.Add(Vector3.Lerp(c1, c2, 0.5f));
        boundary.Add(Vector3.Lerp(c1, c2, 0.75f));

        // Top edge (c2 to c3)
        boundary.Add(c2);
        boundary.Add(Vector3.Lerp(c2, c3, 0.25f));
        boundary.Add(Vector3.Lerp(c2, c3, 0.5f));
        boundary.Add(Vector3.Lerp(c2, c3, 0.75f));

        // Left edge (c3 to c0)
        boundary.Add(c3);
        boundary.Add(Vector3.Lerp(c3, c0, 0.25f));
        boundary.Add(Vector3.Lerp(c3, c0, 0.5f));
        boundary.Add(Vector3.Lerp(c3, c0, 0.75f));

        return boundary.ToArray();
    }
}