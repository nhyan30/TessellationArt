using System.Collections.Generic;
using UnityEngine;

public static class LineRendererDrawer
{
    static List<GameObject> lines = new List<GameObject>();

    static int depthLayers = 7;
    static float depthStep = .1f;
    static float scaleStep = .035f;

    public static void Draw(List<Triangle> tris, Material mat, Transform board)
    {
        Clear();
        DrawBoardOutline(board, mat);

        foreach (var t in tris)
        {
            DrawExtrudedTriangle(t, mat, board);
        }
    }

    public static void DrawBoardOutline(Transform board, Material mat)
    {
        Vector3[] c = GetBoundaryPoints(board);

        // Draw outline segments connecting all 8 boundary points
        for (int i = 0; i < c.Length; i++)
        {
            DrawEdge(c[i], c[(i + 1) % c.Length], mat, .1f);
        }
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
        boundary.Add(Vector3.Lerp(c0, c1, 0.5f));

        // Right edge (c1 to c2)
        boundary.Add(c1);
        boundary.Add(Vector3.Lerp(c1, c2, 0.5f));

        // Top edge (c2 to c3)
        boundary.Add(c2);
        boundary.Add(Vector3.Lerp(c2, c3, 0.5f));

        // Left edge (c3 to c0)
        boundary.Add(c3);
        boundary.Add(Vector3.Lerp(c3, c0, 0.5f));

        return boundary.ToArray();
    }

    static void DrawExtrudedTriangle(Triangle tri, Material mat, Transform board)
    {
        Vector3 center = (tri.a + tri.b + tri.c) / 3f;

        // FRONT BRIGHT FACE
        DrawEdge(tri.a, tri.b, mat, .05f);
        DrawEdge(tri.b, tri.c, mat, .05f);
        DrawEdge(tri.c, tri.a, mat, .05f);

        // DEPTH COPIES BEHIND
        for (int i = 1; i <= depthLayers; i++)
        {
            float d = i * depthStep;

            Vector3 boardCenter = board.position;
            Vector3 dirToCenter = (boardCenter - center).normalized;

            // Calculate depth offset in local space to support board rotation properly
            Vector3 localDirToCenter = board.InverseTransformDirection(dirToCenter);
            Vector3 localDepthOffset = new Vector3(
                localDirToCenter.x * d * 0.6f,
                -d * 0.15f,
                localDirToCenter.z * d * 0.8f // Fixed Z-axis bug here
            );
            Vector3 depthOffset = board.TransformDirection(localDepthOffset);

            float s = 1f - (i * scaleStep);

            Vector3 A = center + (tri.a - center) * s + depthOffset;
            Vector3 B = center + (tri.b - center) * s + depthOffset;
            Vector3 C = center + (tri.c - center) * s + depthOffset;

            DrawEdge(A, B, mat, .025f);
            DrawEdge(B, C, mat, .025f);
            DrawEdge(C, A, mat, .025f);

            // Connect layers for tunnel ribs
            if (i == 1)
            {
                DrawEdge(tri.a, A, mat, .018f);
                DrawEdge(tri.b, B, mat, .018f);
                DrawEdge(tri.c, C, mat, .018f);
            }
        }
    }

    static void DrawEdge(Vector3 a, Vector3 b, Material mat, float width)
    {
        GameObject go = new GameObject("Line");
        LineRenderer lr = go.AddComponent<LineRenderer>();

        lr.material = mat;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        lr.SetPosition(0, a + Vector3.up * .02f);
        lr.SetPosition(1, b + Vector3.up * .02f);

        lr.numCapVertices = 8;

        lines.Add(go);
    }

    static void Clear()
    {
        foreach (var l in lines)
            if (l != null)
                GameObject.Destroy(l);

        lines.Clear();
    }
}