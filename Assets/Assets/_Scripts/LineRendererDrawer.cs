using System.Collections.Generic;
using UnityEngine;

public static class LineRendererDrawer
{
    static List<GameObject> lines = new List<GameObject>();

    const int depthLayers = 8;      // Number of repeated depth copies
    const float depthStep = 0.1f;  // Vertical spacing between copies

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

        // --- Glow layer (wider, dimmer) ---
        for (int i = 0; i < c.Length; i++)
            DrawEdge(c[i], c[(i + 1) % c.Length], mat, 0.22f, 0.12f);

        // --- Core outline ---
        for (int i = 0; i < c.Length; i++)
            DrawEdge(c[i], c[(i + 1) % c.Length], mat, 0.07f, 1f);

        // --- Outline depth copies ---
        for (int d = 1; d <= 2; d++)
        {
            float fade = 1f - (float)d / 3f;
            Vector3 depthOff = board.TransformDirection(new Vector3(0, -d * 0.07f, 0));
            for (int i = 0; i < c.Length; i++)
            {
                DrawEdge(c[i] + depthOff, c[(i + 1) % c.Length] + depthOff,
                         mat, 0.04f, fade);
            }
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

    static void DrawExtrudedTriangle(Triangle tri, Material mat, Transform board)
    {
        // ── FRONT FACE (bright + glow) ─────────────────────
        DrawGlowEdge(tri.a, tri.b, mat, 0.05f);
        DrawGlowEdge(tri.b, tri.c, mat, 0.05f);
        DrawGlowEdge(tri.c, tri.a, mat, 0.05f);

        // ── DEPTH COPIES (repeated fading lines) ───────────
        Vector3 prevA = tri.a, prevB = tri.b, prevC = tri.c;

        for (int i = 1; i <= depthLayers; i++)
        {
            float d = i * depthStep;
            float fade = 1f - (float)i / (depthLayers + 1);   // Fades from 0.87 down to 0.12

            // Only translate downwards, NO scaling
            Vector3 depthOffset = board.TransformDirection(new Vector3(0, -d, 0));

            // All copies use the exact same original vertex positions, just offset
            Vector3 A = tri.a + depthOffset;
            Vector3 B = tri.b + depthOffset;
            Vector3 C = tri.c + depthOffset;

            // Constant width for all depth lines so they match the front face size
            float w = 0.03f;

            DrawEdge(A, B, mat, w, fade);
            DrawEdge(B, C, mat, w, fade);
            DrawEdge(C, A, mat, w, fade);

            // Ribs connecting each pair of adjacent layers
            DrawEdge(prevA, A, mat, 0.015f, fade * 0.6f);
            DrawEdge(prevB, B, mat, 0.015f, fade * 0.6f);
            DrawEdge(prevC, C, mat, 0.015f, fade * 0.6f);

            prevA = A; prevB = B; prevC = C;
        }
    }

    /// Draws a soft glow + bright core for a front-facing edge
    static void DrawGlowEdge(Vector3 a, Vector3 b, Material mat, float coreWidth)
    {
        DrawEdge(a, b, mat, coreWidth * 4f, 0.08f);   // outer glow
        DrawEdge(a, b, mat, coreWidth * 2f, 0.25f);   // mid glow
        DrawEdge(a, b, mat, coreWidth, 1.0f);    // bright core
    }

    static void DrawEdge(Vector3 a, Vector3 b, Material baseMat, float width, float fade)
    {
        GameObject go = new GameObject("Line");
        LineRenderer lr = go.AddComponent<LineRenderer>();

        // Create a material instance so each line can have its own _Fade value
        Material instance = new Material(baseMat);
        instance.SetFloat("_Fade", fade);

        lr.material = instance;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.numCapVertices = 8;

        lr.SetPosition(0, a + Vector3.up * 0.02f);
        lr.SetPosition(1, b + Vector3.up * 0.02f);

        lines.Add(go);
    }

    static void Clear()
    {
        foreach (var l in lines)
            if (l != null)
                Object.Destroy(l);

        lines.Clear();
    }
}