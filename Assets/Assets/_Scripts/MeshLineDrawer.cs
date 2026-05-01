using System.Collections.Generic;
using UnityEngine;

public class MeshLineDrawer : MonoBehaviour
{
    Mesh mesh;

    List<Vector3> verts = new List<Vector3>();
    List<int> indices = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();

    const int depthLayers = 10;
    const float depthStep = 0.1f;

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "LineMesh";

        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void Draw(List<Triangle> tris, Transform board)
    {
        verts.Clear();
        indices.Clear();
        uvs.Clear();
        colors.Clear();

        // ✅ ALWAYS draw outline
        DrawBoardOutline(board);

        // ✅ Only draw triangles if they exist
        if (tris != null && tris.Count > 0)
        {
            HashSet<(Vector3, Vector3)> edges = new HashSet<(Vector3, Vector3)>();

            foreach (var t in tris)
            {
                AddEdge(edges, t.a, t.b);
                AddEdge(edges, t.b, t.c);
                AddEdge(edges, t.c, t.a);
            }

            foreach (var e in edges)
            {
                DrawExtrudedEdge(e.Item1, e.Item2, board);
            }
        }

        // ✅ THIS is what you were missing in Start()
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(indices, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
    }

    public void DrawBoardOutline(Transform board)
    {
        Vector3[] c = GetBoundaryPoints(board);

        for (int i = 0; i < c.Length; i++)
        {
            Vector3 a = c[i];
            Vector3 b = c[(i + 1) % c.Length];

            // Optional: give it depth too (matches inner edges look)
            DrawExtrudedEdge(a, b, board);
        }
    }

    Vector3[] GetBoundaryPoints(Transform board)
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

    void AddEdge(HashSet<(Vector3, Vector3)> set, Vector3 a, Vector3 b)
    {
        if (a.GetHashCode() < b.GetHashCode())
            set.Add((a, b));
        else
            set.Add((b, a));
    }

    void DrawExtrudedEdge(Vector3 a, Vector3 b, Transform board)
    {
        DrawGlow(a, b);

        Vector3 prevA = a;
        Vector3 prevB = b;

        for (int i = 1; i <= depthLayers; i++)
        {
            float t = (float)i / depthLayers;
            float fade = Mathf.Pow(1f - t, 2f);

            Vector3 offset = board.TransformDirection(new Vector3(0, -i * depthStep, 0));

            Vector3 A = a + offset;
            Vector3 B = b + offset;

            AddLineQuad(A, B, 0.03f, fade);

            AddLineQuad(prevA, A, 0.015f, fade * 0.6f);
            AddLineQuad(prevB, B, 0.015f, fade * 0.6f);

            prevA = A;
            prevB = B;
        }
    }

    void DrawGlow(Vector3 a, Vector3 b)
    {
        AddLineQuad(a, b, 0.2f, 0.08f);
        AddLineQuad(a, b, 0.1f, 0.25f);
        AddLineQuad(a, b, 0.05f, 1f);
    }

    void AddLineQuad(Vector3 a, Vector3 b, float width, float fade)
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 dir = (b - a).normalized;
        Vector3 normal = Vector3.Cross(dir, camForward).normalized * width;

        int start = verts.Count;

        verts.Add(a - normal);
        verts.Add(a + normal);
        verts.Add(b - normal);
        verts.Add(b + normal);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));

        Color c = new Color(fade, 0, 0, 1);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);

        indices.Add(start + 0);
        indices.Add(start + 1);
        indices.Add(start + 2);

        indices.Add(start + 2);
        indices.Add(start + 1);
        indices.Add(start + 3);
    }
}