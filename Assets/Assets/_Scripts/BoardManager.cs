using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public Camera cam;
    public Material lineMaterial;

    private List<Vector3> points = new List<Vector3>();
    private List<Triangle> triangles = new List<Triangle>();

    void Start()
    {
        LineRendererDrawer.DrawBoardOutline(
            transform,
            lineMaterial
        );
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
        points.Add(newPoint);

        // Rebuild triangulation
        triangles = Triangulation.Generate(points, transform);

        // Draw
        LineRendererDrawer.Draw(triangles,lineMaterial,transform);
    }
}