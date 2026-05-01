using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public Camera cam;
    public Material lineMaterial;
    public MeshLineDrawer meshDrawer;

    private List<Vector3> points = new List<Vector3>();
    private List<Triangle> triangles = new List<Triangle>();

    void Start()
    {
        //LineRendererDrawer.DrawBoardOutline(transform, lineMaterial);
        meshDrawer.Draw(new List<Triangle>(), transform);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                AddPoint(hit.point);
            }
        }
    }

    void AddPoint(Vector3 newPoint)
    {
        // Prevent clicking too close to existing points (avoids broken triangles)
        foreach (var p in points)
        {
            if (Vector3.Distance(p, newPoint) < 0.5f) return;
        }

        points.Add(newPoint);

        if (points.Count == 1)
        {
            // FIRST HIT: connect only to the 6 nearest boundary anchors
            triangles = Triangulation.Generate(points, transform, 8, points[0]);
        }
        else if (points.Count == 2)
        {
            triangles = Triangulation.Generate(points, transform, 12, points[0]);
        }
        else if (points.Count == 3)
        {
            triangles = Triangulation.Generate(points, transform, 14, points[0]);
        }
        else
        {
            // SECOND HIT onwards: connect to all 16 boundary anchors
            triangles = Triangulation.Generate(points, transform, 16, null);
        }

        //LineRendererDrawer.Draw(triangles, lineMaterial, transform);
        meshDrawer.Draw(triangles, transform);
    }
}