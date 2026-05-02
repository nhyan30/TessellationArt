using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public Camera cam;
    public Material lineMaterial;
    public MeshLineDrawer meshDrawer;

    [Header("Audio")]
    public AudioClip hitSound; // Assign your sound effect here in the Inspector

    private AudioSource audioSource;
    private List<Vector3> points = new List<Vector3>();
    private List<Triangle> triangles = new List<Triangle>();

    void Start()
    {
        // Get or add an AudioSource component automatically
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

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
            triangles = Triangulation.Generate(points, transform, 16, null);
        }

        meshDrawer.Draw(triangles, transform);

        // Play the hit sound effect
        if (hitSound != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f); // 🔥 variation
            audioSource.PlayOneShot(hitSound);
        }
    }
}