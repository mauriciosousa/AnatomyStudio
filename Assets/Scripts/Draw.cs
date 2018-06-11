using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Draw : MonoBehaviour {

    private LineRenderer _currentLine;
    private Slicer _slicer;
    private StructuresList _sList;

    private string _currentVolume;

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

    private bool _enabled;
    public bool Enabled
    {
        get
        {
            return _enabled;
        }

        set
        {
            _enabled = value;
        }
    }

    // Use this for initialization
    void Start ()
    {
        _main = GetComponent<Main>();
        _assnetwork = GameObject.Find("Network").GetComponent<ASSNetwork>();

        _slicer = GetComponent<Slicer>();
        _sList = GetComponent<StructuresList>();
        _currentVolume = "none";
        _drawing = false;
        _enabled = true;
    }
	
	// Update is called once per frame
	void Update ()
    {
        // draw
        if (_main.deviceType == DeviceType.Tablet)
        {
            // update volume
            _currentVolume = _sList.CurrentStructure;

            Vector3 mousePosition = Utils.MouseToWorld(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                if (_enabled)
                {
                    if (!Utils.GUIContains(Input.mousePosition))
                    {
                        if (_currentVolume != "none")
                        {
                            StartDrawing(mousePosition);
                        }
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
                    if (_currentLine != null && _currentLine.positionCount > 1)
                    {
                        Vector3[] positions = new Vector3[_currentLine.positionCount];
                        _currentLine.GetPositions(positions);
                        _assnetwork.broadcastLine(_currentLine.name, _slicer.Slice, _currentVolume, positions);
                    }
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

            // test Disable
            if(Input.GetKeyDown(KeyCode.D))
            {
                _enabled = !_enabled;
            }
        }
    }

    void OnGUI()
    {

    }

    internal void AddLine(string lineID, int slice, string structure, Vector3[] line)
    {
        if (GameObject.Find(lineID) != null) return; 

        // create line
        LineRenderer lr = CreateLine(structure, lineID);

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
                _currentLine = CreateLine(_currentVolume, GenerateID());

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

    public bool CheckTolerance(Vector3 p1, Vector3 p2)
    {
        return Vector3.Distance(p1, p2) > pointTolerance;
    }

    private void AddPoint(Vector3 localPoint, LineRenderer lineRenderer, string volumeName)
    {
        lineRenderer.SetPosition(lineRenderer.positionCount++, localPoint);
    }

    private void StartDrawing(Vector3 point)
    {
        _currentLine = null;

        _startingPoint = point;

        _drawing = true;
    }

    private LineRenderer CreateLine(string volumeName, string lineID)
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
        go.name = lineID;
        go.tag = "DrawLine";
        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        LineRenderer lr = go.GetComponent<LineRenderer>();
        lr.material = Resources.Load("Materials/" + _sList.GetMaterialName(volumeName) + "Line", typeof(Material)) as Material;

        return lr;
    }

    private void UpdateVolume(string volumeName)
    {
        if (_main.deviceType == DeviceType.Tablet && disableTabletVolumes) return;
        if (volumeName == "none") return;

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

    private Mesh CreateMesh(string volume)
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        List<int> triangles = new List<int>();

        GameObject go = GameObject.Find(volume + "Lines");

        int numPoints = 0;
        foreach (Transform child in go.transform)
        {
            if(child.gameObject.activeInHierarchy)
                numPoints += child.GetComponent<LineRenderer>().positionCount;
        }

        if (numPoints < 3) return null;

        double[][] vertices = new double[numPoints][];

        int i = 0;
        foreach (Transform child in go.transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                LineRenderer lr = child.GetComponent<LineRenderer>();
                for (int j = 0; j < lr.positionCount; j++)
                {
                    Vector3 v = lr.GetPosition(j);
                    vertices[i++] = new double[3] { v.x, v.y, v.z };
                }
            }
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

    private string GenerateID()
    {
        return Guid.NewGuid().ToString("N");
    }

    public void RemoveLine(GameObject gameObject)
    {
        string volume = gameObject.transform.parent.gameObject.name.Replace("Lines", "");
        gameObject.SetActive(false);
        Destroy(gameObject);
        UpdateVolume(volume);
    }
}
