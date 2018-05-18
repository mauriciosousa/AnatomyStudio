using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Draw : MonoBehaviour {

    private LineRenderer _currentLine;
    private Slicer _slicer;
    private SliceLoader _loader;
    private StructuresList _sList;

    private string _currentVolume;
    private Dictionary<string, List<Vector3>> _points;

    private Vector3 _startingPoint;
    private bool _drawing;

    private Main _main;
    private ASSNetwork _assnetwork;

    public Transform tabletop;

    public float pointTolerance;

    public bool disableTabletVolumes;

    public bool Drawing
    {
        get
        {
            return _drawing;
        }
    }

    // Use this for initialization
    void Start ()
    {
        _main = GetComponent<Main>();
        _assnetwork = GameObject.Find("Network").GetComponent<ASSNetwork>();

        _slicer = GetComponent<Slicer>();
        _loader = GetComponent<SliceLoader>();
        _sList = GetComponent<StructuresList>();
        _currentVolume = "none";
        _points = new Dictionary<string, List<Vector3>>();
        _drawing = false;
    }
	
	// Update is called once per frame
	void Update ()
    {
        // draw
        if (_main.deviceType == DeviceType.Tablet)
        {
            // update volume
            _currentVolume = _sList.CurrentStructure;

            Vector3 mousePosition = MouseToWorld(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseToGUI = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

                if (!(_slicer.SliderArea.Contains(mouseToGUI) || _sList.SliderArea.Contains(mouseToGUI)))
                {
                    if (_currentVolume != "none")
                    {
                        StartDrawing(mousePosition);
                    }
                }
            }
            if (Input.GetMouseButton(0))
            {
                if (Drawing)
                {
                    UpdateDrawing(mousePosition);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (Drawing)
                {
                    EndDrawing();

                    // broadcast line
                    Vector3[] positions = new Vector3[_currentLine.positionCount];
                    _currentLine.GetPositions(positions);
                    _assnetwork.broadcastLine("", _slicer.Slice, _currentVolume, positions);
                }
            }

            // if more than one touch abort drawing
            if (Drawing && Input.touchCount > 1)
                AbortDrawing();

            // test Abort drawing
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (Drawing)
                    AbortDrawing();
            }
        }
    }

    void OnGUI()
    {

    }

    internal void AddLine(string userID, int slice, string structure, Vector3[] line)
    {
        // create line
        LineRenderer lr = CreateLine(structure);

        // add points
        foreach (Vector3 p in line)
        {
            AddPoint(p, lr, structure);
        }

        // update volume (or structure)
        UpdateVolume(structure);
    }

    private void AbortDrawing()
    {
        _points[_currentVolume].RemoveRange(_points[_currentVolume].Count - _currentLine.positionCount, _currentLine.positionCount);
        Destroy(_currentLine.gameObject);

        _drawing = false;
    }

    private void EndDrawing()
    {
        UpdateVolume(_currentVolume);

        _drawing = false;
    }

    private void UpdateDrawing(Vector3 worldPoint)
    {
        Vector3 localPoint = tabletop.transform.worldToLocalMatrix.MultiplyPoint(worldPoint);

        if (!float.IsPositiveInfinity(_startingPoint.x))
        {
            Vector3 localStartingPoint = tabletop.transform.worldToLocalMatrix.MultiplyPoint(_startingPoint);

            if (CheckTolerance(localPoint, localStartingPoint))
            {
                AddPoint(localStartingPoint, _currentLine, _currentVolume);
                AddPoint(localPoint, _currentLine, _currentVolume);

                _startingPoint = Vector3.one * float.PositiveInfinity;
            }
        }
        else if (CheckTolerance(localPoint, _currentLine.GetPosition(_currentLine.positionCount - 1)))
        {
            AddPoint(localPoint, _currentLine, _currentVolume);
        }
    }

    private bool CheckTolerance(Vector3 p1, Vector3 p2)
    {
        return Vector3.Distance(p1, p2) > pointTolerance;
    }

    private void AddPoint(Vector3 localPoint, LineRenderer lineRenderer, string volumeName)
    {
        lineRenderer.SetPosition(lineRenderer.positionCount++, localPoint);
        _points[volumeName].Add(localPoint);
    }

    private void StartDrawing(Vector3 point)
    {
        _currentLine = CreateLine(_currentVolume);

        _startingPoint = point;

        _drawing = true;
    }

    private LineRenderer CreateLine(string volumeName)
    {
        string fullLinesName = volumeName + "Lines";

        GameObject parent = GameObject.Find(fullLinesName);
        if (parent == null)
        {
            parent = new GameObject(fullLinesName);
            parent.transform.parent = tabletop;
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
        }

        GameObject go = Instantiate(Resources.Load("Prefabs/Line", typeof(GameObject))) as GameObject;
        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        LineRenderer lr = go.GetComponent<LineRenderer>();
        lr.material = Resources.Load("Materials/" + _sList.GetMaterialName(volumeName) + "Line", typeof(Material)) as Material;

        if (!_points.ContainsKey(volumeName)) _points[volumeName] = new List<Vector3>();

        return lr;
    }

    private Vector3 MouseToWorld(Vector2 mousePosition)
    {
        Plane plane = new Plane(Camera.main.transform.forward, Camera.main.transform.position + Camera.main.transform.forward * _slicer.Slice * _loader.sliceDepth);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float hitDistance;

        if(plane.Raycast(ray, out hitDistance))
            return ray.GetPoint(hitDistance);
        else
            return Vector3.zero;
    }

    private void UpdateVolume(string volumeName)
    {
        if (_main.deviceType == DeviceType.Tablet && disableTabletVolumes) return;
        if (volumeName == "none") return;
        if (!_points.ContainsKey(volumeName)) return;

        string fullVolumeName = volumeName + "Mesh";

        GameObject volume = GameObject.Find(fullVolumeName);
        if(volume == null)
        {
            volume = Instantiate(Resources.Load("Prefabs/Volume", typeof(GameObject))) as GameObject;
            volume.name = fullVolumeName;
            volume.GetComponent<MeshRenderer>().material = Resources.Load("Materials/" + _sList.GetMaterialName(volumeName) + "Volume", typeof(Material)) as Material;
            volume.transform.parent = tabletop;
            volume.transform.localPosition = Vector3.zero;
            volume.transform.localRotation = Quaternion.identity;
        }
        volume.GetComponent<MeshFilter>().mesh = CreateMesh(volumeName);
    }

    private Mesh CreateMesh(String volume)
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        List<int> triangles = new List<int>();

        double[][] vertices = new double[_points[volume].Count][];

        int i = 0;
        foreach (Vector3 v in _points[volume])
        {
            vertices[i++] = new double[3] { v.x, v.y, v.z };
        }

        try
        {
            var result = MIConvexHull.ConvexHull.Create(vertices);

            List<Vector3> vertices2 = new List<Vector3>();

            i = 0;
            foreach (MIConvexHull.DefaultVertex v in result.Points)
            {
                vertices2.Add(VertexToVector(v));
            }
            m.vertices = vertices2.ToArray();

            foreach (var face in result.Faces)
            {
                triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[0])));
                triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[1])));
                triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[2])));
            }

            m.triangles = triangles.ToArray();
            m.RecalculateNormals();
        }
        catch(Exception e)
        {
            print(e);
        }

        return m;
    }

    private Vector3 VertexToVector(MIConvexHull.DefaultVertex v)
    {
        return new Vector3((float)v.Position[0], (float)v.Position[1], (float)v.Position[2]);
    }
}
