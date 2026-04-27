using System.Collections.Generic;
using UnityEngine;

public static class Triangulation
{
    public static List<Triangle> Generate(List<Vector3> points, Transform board)
    {
        List<Triangle> tris = new List<Triangle>();

        // Get board corners
        Vector3[] corners = GetBoardCorners(board);

        if (points.Count == 1)
        {
            Vector3 p = points[0];

            tris.Add(new Triangle(corners[0], corners[1], p));
            tris.Add(new Triangle(corners[1], corners[2], p));
            tris.Add(new Triangle(corners[2], corners[3], p));
            tris.Add(new Triangle(corners[3], corners[0], p));

            return tris;
        }

        // Start from previous triangles
        tris = new List<Triangle>();

        // Simple approach:
        foreach (var p in points)
        {
            if (tris.Count == 0)
            {
                tris.Add(new Triangle(corners[0], corners[1], p));
                tris.Add(new Triangle(corners[1], corners[2], p));
                tris.Add(new Triangle(corners[2], corners[3], p));
                tris.Add(new Triangle(corners[3], corners[0], p));
            }
            else
            {
                List<Triangle> newTris = new List<Triangle>();

                foreach (var t in tris)
                {
                    if (PointInsideTriangle(p, t))
                    {
                        newTris.Add(new Triangle(t.a, t.b, p));
                        newTris.Add(new Triangle(t.b, t.c, p));
                        newTris.Add(new Triangle(t.c, t.a, p));
                    }
                    else
                    {
                        newTris.Add(t);
                    }
                }

                tris = newTris;
            }
        }

        return tris;
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

    static bool PointInsideTriangle(Vector3 p, Triangle t)
    {
        Vector3 v0 = t.c - t.a;
        Vector3 v1 = t.b - t.a;
        Vector3 v2 = p - t.a;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v < 1);
    }
}