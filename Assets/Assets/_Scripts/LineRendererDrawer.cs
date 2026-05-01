using System.Collections.Generic;
using UnityEngine;

public static class LineRendererDrawer
{
    static List<LineRenderer> pool = new List<LineRenderer>();
    static int poolIndex = 0;

    const int depthLayers = 8;
    const float depthStep = 0.1f;

    static Transform parent;

    public static void Draw(List<Triangle> tris, Material mat, Transform board)
    {
        if (parent == null)
        {
            parent = new GameObject("LinePool").transform;
        }

        poolIndex = 0;

        DrawBoardOutline(board, mat);

        HashSet<(Vector3, Vector3)> uniqueEdges = new HashSet<(Vector3, Vector3)>();

        foreach (var t in tris)
        {
            AddEdge(uniqueEdges, t.a, t.b);
            AddEdge(uniqueEdges, t.b, t.c);
            AddEdge(uniqueEdges, t.c, t.a);
        }

        foreach (var e in uniqueEdges)
        {
            DrawExtrudedEdge(e.Item1, e.Item2, mat, board);
        }

        // Disable unused pooled objects
        for (int i = poolIndex; i < pool.Count; i++)
        {
            if (pool[i] != null)
                pool[i].gameObject.SetActive(false);
        }
    }

    static void AddEdge(HashSet<(Vector3, Vector3)> set, Vector3 a, Vector3 b)
    {
        if (a == b) return;

        // Normalize order to avoid duplicates
        if (a.GetHashCode() < b.GetHashCode())
            set.Add((a, b));
        else
            set.Add((b, a));
    }

    static LineRenderer GetLine(Material mat)
    {
        LineRenderer lr;

        if (poolIndex < pool.Count)
        {
            lr = pool[poolIndex];
            lr.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = new GameObject("Line");
            go.transform.parent = parent;

            lr = go.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.useWorldSpace = true;
            lr.numCapVertices = 0;

            pool.Add(lr);
        }

        poolIndex++;
        return lr;
    }

    static void DrawExtrudedEdge(Vector3 a, Vector3 b, Material mat, Transform board)
    {
        // FRONT GLOW
        DrawGlowEdge(a, b, mat, 0.05f);

        Vector3 prevA = a;
        Vector3 prevB = b;

        for (int i = 1; i <= depthLayers; i++)
        {
            float d = i * depthStep;
            float t = (float)i / depthLayers;
            float fade = Mathf.Pow(1f - t, 2f);

            Vector3 offset = board.TransformDirection(new Vector3(0, -d, 0));

            Vector3 A = a + offset;
            Vector3 B = b + offset;

            DrawEdge(A, B, mat, 0.03f, fade);

            // ribs
            DrawEdge(prevA, A, mat, 0.015f, fade * 0.6f);
            DrawEdge(prevB, B, mat, 0.015f, fade * 0.6f);

            prevA = A;
            prevB = B;
        }
    }

    static void DrawGlowEdge(Vector3 a, Vector3 b, Material mat, float coreWidth)
    {
        DrawEdge(a, b, mat, coreWidth * 4f, 0.08f);
        DrawEdge(a, b, mat, coreWidth * 2f, 0.25f);
        DrawEdge(a, b, mat, coreWidth, 1f);
    }

    static void DrawEdge(Vector3 a, Vector3 b, Material mat, float width, float fade)
    {
        LineRenderer lr = GetLine(mat);

        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = 2;

        lr.SetPosition(0, a + Vector3.up * 0.02f);
        lr.SetPosition(1, b + Vector3.up * 0.02f);

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        lr.GetPropertyBlock(mpb);
        mpb.SetFloat("_Fade", fade);
        lr.SetPropertyBlock(mpb);
    }

    public static void DrawBoardOutline(Transform board, Material mat)
    {
        Vector3[] c = GetBoundaryPoints(board);

        for (int i = 0; i < c.Length; i++)
        {
            DrawGlowEdge(c[i], c[(i + 1) % c.Length], mat, 0.07f);
        }
    }

    static Vector3[] GetBoundaryPoints(Transform board)
    {
        float size = 5f;
        int segmentsPerEdge = 4;

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