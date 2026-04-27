using System.Collections.Generic;
using UnityEngine;

public static class LineRendererDrawer
{
    static List<GameObject> lines =
        new List<GameObject>();


    static int depthLayers = 7;

    static float depthStep = .1f;
    static float scaleStep = .035f;


    public static void Draw(
    List<Triangle> tris,
    Material mat,
    Transform board)
    {
        Clear();

        DrawBoardOutline(board, mat);

        foreach (var t in tris)
        {
            DrawExtrudedTriangle(t, mat);
        }
    }

    public static void DrawBoardOutline(
    Transform board,
    Material mat)
    {
        Clear();

        Vector3[] c = GetBoardCorners(board);

        DrawEdge(c[0], c[1], mat, .1f);
        DrawEdge(c[1], c[2], mat, .1f);
        DrawEdge(c[2], c[3], mat, .1f);
        DrawEdge(c[3], c[0], mat, .1f);
    }

    static Vector3[] GetBoardCorners(Transform board)
    {
        float size = 5f;

        return new Vector3[]
        {
        board.TransformPoint(new Vector3(-size, 0, -size)),
        board.TransformPoint(new Vector3(size, 0, -size)),
        board.TransformPoint(new Vector3(size, 0, size)),
        board.TransformPoint(new Vector3(-size, 0, size))
        };
    }


    static void DrawExtrudedTriangle(
        Triangle tri,
        Material mat)
    {
        Vector3 center =
            (tri.a + tri.b + tri.c) / 3f;


        //--------------------------------
        // FRONT BRIGHT FACE
        //--------------------------------

        DrawEdge(
            tri.a,
            tri.b,
            mat,
            .05f);

        DrawEdge(
            tri.b,
            tri.c,
            mat,
            .05f);

        DrawEdge(
            tri.c,
            tri.a,
            mat,
            .05f);



        //--------------------------------
        // DEPTH COPIES BEHIND
        //--------------------------------

        for (int i = 1; i <= depthLayers; i++)
        {
            float d = i * depthStep;

            // push "into" board
            Vector3 depthOffset =
                new Vector3(
                    -d * .35f,
                    d * .04f,
                    d
                );

            float s =
                1f - (i * scaleStep);


            Vector3 A =
                center +
                (tri.a - center) * s +
                depthOffset;

            Vector3 B =
                center +
                (tri.b - center) * s +
                depthOffset;

            Vector3 C =
                center +
                (tri.c - center) * s +
                depthOffset;


            DrawEdge(
                A, B, mat, .025f);

            DrawEdge(
                B, C, mat, .025f);

            DrawEdge(
                C, A, mat, .025f);


            // connect layers for tunnel ribs
            if (i == 1)
            {
                DrawEdge(
                    tri.a, A, mat, .018f);

                DrawEdge(
                    tri.b, B, mat, .018f);

                DrawEdge(
                    tri.c, C, mat, .018f);
            }
        }
    }



    static void DrawEdge(
        Vector3 a,
        Vector3 b,
        Material mat,
        float width)
    {
        GameObject go =
            new GameObject("Line");

        LineRenderer lr =
            go.AddComponent<LineRenderer>();

        lr.material = mat;

        lr.startWidth = width;
        lr.endWidth = width;

        lr.positionCount = 2;

        lr.useWorldSpace = true;

        lr.SetPosition(
            0,
            a + Vector3.up * .02f);

        lr.SetPosition(
            1,
            b + Vector3.up * .02f);

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