using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
        string fullLinesName = _currentVolume + "Lines";
        GameObject parent = GameObject.Find(fullLinesName);
        VolumeLineInfo lines = parent.GetComponent<VolumeLineInfo>(); ;
        lines.updateLines(_slicer.Slice);

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
            parent.AddComponent<VolumeLineInfo>();
            parent.transform.parent = tabletop;
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
        }
        VolumeLineInfo lines = parent.GetComponent<VolumeLineInfo>(); ;

        GameObject go = Instantiate(Resources.Load("Prefabs/Line", typeof(GameObject))) as GameObject;
        go.name = lineID;
        go.tag = "DrawLine";
        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        LineRenderer lr = go.GetComponent<LineRenderer>();
        lines.addLine(_slicer.Slice, lr);
        lr.material = Resources.Load("Materials/" + _sList.GetMaterialName(volumeName) + "Line", typeof(Material)) as Material;

        return lr;
    }

    private void UpdateVolume(string volumeName)
    {
        if (_main.deviceType == DeviceType.Tablet && disableTabletVolumes) return;
        if (volumeName == "none") return;

        CreateMesh(volumeName);
    }


    //Pra fazer tudo virar uma mesh só:
    //- se a mesh atual - numero de triangulos que eu tinha nela, + numero de pontos que eu tenho * 2 menor que 64k, refaço a mesh. Caso contrário tenho que refazer essa e a próxima. 
    private void connectLinesNotAll(Circle alines, Circle blines, int slicea, string volumeName)
    {

        GameObject parentVolume = GameObject.Find(volumeName);
        if (parentVolume == null)
        {
            parentVolume = new GameObject(volumeName);
            parentVolume.transform.parent = tabletop;
            parentVolume.transform.localPosition = Vector3.zero;
            parentVolume.transform.localRotation = Quaternion.identity;
        }

        string fullVolumeName = volumeName + slicea + "Mesh";

        GameObject volume = GameObject.Find(fullVolumeName);
        if (volume == null)
        {
            volume = Instantiate(Resources.Load("Prefabs/Volume", typeof(GameObject))) as GameObject;
            volume.name = fullVolumeName;
            volume.GetComponent<MeshRenderer>().material = Resources.Load("Materials/" + _sList.GetMaterialName(volumeName) + "Volume", typeof(Material)) as Material;
            volume.transform.parent = parentVolume.transform;
            volume.transform.localPosition = Vector3.zero;
            volume.transform.localRotation = Quaternion.identity;
        }
        
        //Start matching points
        if (alines.pointCount == 0 || blines.pointCount == 0)
        {
            volume.GetComponent<MeshFilter>().mesh = null;
            return;
        }

        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        List<int> finalTriangles = new List<int>();
        List<Vector3> finalPoints = new List<Vector3>();

   
        Circle smaller, bigger;
        if (alines.pointCount > blines.pointCount)
        {
            smaller = blines;
            bigger = alines;
        }
        else
        {
            smaller = alines;
            bigger = blines;
        }

        int trianglesPerPoint = (int)Math.Floor((bigger.pointCount - 1) / ((float)smaller.pointCount));
        int remainderPool = (bigger.pointCount - 1) % smaller.pointCount;

        LineInfo smallerLine = smaller.lines[smaller.lineStartID];
        int smallerI = smaller.pointStartID;
        int smallerInc = smaller.directionClockwise;
        LineInfo biggerLine = bigger.lines[bigger.lineStartID];
        int biggerI = bigger.pointStartID;
        int biggerInc = bigger.directionClockwise;

        int i = 0;
        int vertexID = 0;
        int edgeID = -1;

        bool invert = false;
        //find triangle direction 
        if(smallerLine.line.GetPosition(smallerI).z > biggerLine.line.GetPosition(biggerI).z)
        {
            invert = true;
        }

        while (i < smaller.pointCount)
        {
            int j = 0;
            finalPoints.Add(smallerLine.line.GetPosition(smallerI));
            vertexID++;


            //not my first edge
            if (edgeID != -1)
            {
                if (invert) { 
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 2);
                    finalTriangles.Add(vertexID - 1);
                }else
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(vertexID - 2);
                }
                edgeID = vertexID - 1;
            }
            else
            {
                finalPoints.Add(biggerLine.line.GetPosition(biggerI));
                vertexID++;
                bigger.nextPoint(ref biggerI, ref biggerLine, ref biggerInc);
                j++;
                edgeID = 0;
            }
            
            while (j < trianglesPerPoint)
            {
                bigger.nextPoint(ref biggerI, ref biggerLine, ref biggerInc);
                j++;
            }
            if (remainderPool > 0)
            {
                remainderPool--;
                bigger.nextPoint(ref biggerI, ref biggerLine, ref biggerInc);
            }

            finalPoints.Add(biggerLine.line.GetPosition(biggerI));
            vertexID++;
            if (edgeID == 0)
            {
                if (invert) { 
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 2);
                    finalTriangles.Add(vertexID - 1);
                }else
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(vertexID - 2);
                }
            }
            else
            {
                if (invert) {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(edgeID - 1);
                    finalTriangles.Add(vertexID - 1);
                }else {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(edgeID - 1);
                }
            }
            

            smaller.nextPoint(ref smallerI, ref smallerLine, ref smallerInc);
            i++;
        }

        if (invert) { 
            finalTriangles.Add(edgeID);
            finalTriangles.Add(vertexID - 1);
            finalTriangles.Add(0);
        }else
        {
            finalTriangles.Add(edgeID);
            finalTriangles.Add(0);
            finalTriangles.Add(vertexID - 1);
        }

        if (invert) { 
            finalTriangles.Add(0);
            finalTriangles.Add(vertexID - 1);
            finalTriangles.Add(1);
        }else
        {
            finalTriangles.Add(0);
            finalTriangles.Add(1);
            finalTriangles.Add(vertexID - 1);
        }

        m.vertices = finalPoints.ToArray();
        m.triangles = finalTriangles.ToArray();
        m.RecalculateNormals();
        volume.GetComponent<MeshFilter>().mesh = m;
    }

    private void connectLines(Circle alines, Circle blines, int slicea, string volumeName)
    {

        GameObject parentVolume = GameObject.Find(volumeName);
        if (parentVolume == null)
        {
            parentVolume = new GameObject(volumeName);
            parentVolume.transform.parent = tabletop;
            parentVolume.transform.localPosition = Vector3.zero;
            parentVolume.transform.localRotation = Quaternion.identity;
        }

        string fullVolumeName = volumeName + slicea + "Mesh";

        GameObject volume = GameObject.Find(fullVolumeName);
        if (volume == null)
        {
            volume = Instantiate(Resources.Load("Prefabs/Volume", typeof(GameObject))) as GameObject;
            volume.name = fullVolumeName;
            volume.GetComponent<MeshRenderer>().material = Resources.Load("Materials/" + _sList.GetMaterialName(volumeName) + "Volume", typeof(Material)) as Material;
            volume.transform.parent = parentVolume.transform;
            volume.transform.localPosition = Vector3.zero;
            volume.transform.localRotation = Quaternion.identity;
        }

        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        List<int> finalTriangles = new List<int>();
        List<Vector3> finalPoints = new List<Vector3>();

        //Start matching points

        Circle smaller, bigger;
        if (alines.pointCount > blines.pointCount)
        {
            smaller = blines;
            bigger = alines;
        }
        else
        {
            smaller = alines;
            bigger = blines;
        }

        int trianglesPerPoint = (int)Math.Floor((bigger.pointCount - 1) / ((float)smaller.pointCount));
        int remainderPool = (bigger.pointCount - 1) % smaller.pointCount;

        LineInfo smallerLine = smaller.lines[smaller.lineStartID];
        int smallerI = smaller.pointStartID;
        int smallerInc = smaller.directionClockwise;
        LineInfo biggerLine = bigger.lines[bigger.lineStartID];
        int biggerI = bigger.pointStartID;
        int biggerInc = bigger.directionClockwise;

        int i = 0;
        int vertexID = 0;
        int edgeID = -1;

        bool invert = false;
        //find triangle direction 
        if (smallerLine.line.GetPosition(smallerI).z > biggerLine.line.GetPosition(biggerI).z)
        {
            invert = true;
        }

        while (i < smaller.pointCount)
        {
            int j = 0;
            finalPoints.Add(smallerLine.line.GetPosition(smallerI));
            vertexID++;



            //not my first edge
            if (edgeID != -1)
            {
                if (invert)
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 2);
                    finalTriangles.Add(vertexID - 1);

                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(vertexID - 2);
                    finalTriangles.Add(vertexID);
                }else
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(vertexID - 2);

                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(vertexID);
                    finalTriangles.Add(vertexID - 2);
                }
                j++;
                edgeID = vertexID - 1;
            }
            else
            {
                edgeID = 0;
            }

            finalPoints.Add(biggerLine.line.GetPosition(biggerI));
            bigger.nextPoint(ref biggerI, ref biggerLine, ref biggerInc);
            vertexID++;

            while (j < trianglesPerPoint)
            {
                finalPoints.Add(biggerLine.line.GetPosition(biggerI));
                if (invert)
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(vertexID++);
                }else
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID);
                    finalTriangles.Add(vertexID++ - 1);
                }
                j++;
                bigger.nextPoint(ref biggerI, ref biggerLine, ref biggerInc);
            }
            if (remainderPool > 0)
            {
                remainderPool--;
                finalPoints.Add(biggerLine.line.GetPosition(biggerI));
                if (invert)
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID - 1);
                    finalTriangles.Add(vertexID++);
                }else
                {
                    finalTriangles.Add(edgeID);
                    finalTriangles.Add(vertexID);
                    finalTriangles.Add(vertexID++ - 1);
                }
                bigger.nextPoint(ref biggerI, ref biggerLine, ref biggerInc);
            }
            smaller.nextPoint(ref smallerI, ref smallerLine, ref smallerInc);
            i++;
        }

        if (invert)
        {
            finalTriangles.Add(edgeID);
            finalTriangles.Add(0);
            finalTriangles.Add(vertexID - 1);
            finalTriangles.Add(0);
            finalTriangles.Add(vertexID - 1);
            finalTriangles.Add(1);
        }
        else {
            finalTriangles.Add(edgeID);
            finalTriangles.Add(vertexID - 1);
            finalTriangles.Add(0);
            finalTriangles.Add(0);
            finalTriangles.Add(1);
            finalTriangles.Add(vertexID - 1);
        }
        m.vertices = finalPoints.ToArray();
        m.triangles = finalTriangles.ToArray();
        m.RecalculateNormals();
        volume.GetComponent<MeshFilter>().mesh = m;
    }

    private void CreateMesh(string volume)
    {
  
        GameObject go = GameObject.Find(volume + "Lines");

        Dictionary<int,Circle> las = go.GetComponent<VolumeLineInfo>().LinesAtSlice;

        List<int> keys =  las.Keys.ToList();
        keys.Sort();

        int i = 0;
        for (; i < keys.Count; i++)
        {
            if (keys[i] == _slicer.Slice)
                break;
        }

      
     
            //if not first slice, connect current slice with the previous one
        if(i != 0)
        {
            connectLinesNotAll(las[keys[i]], las[keys[i - 1]],keys[i],volume);

        }

        //if not last slice, connect next slice with me 
        if(i != keys.Count - 1)
        {
            connectLinesNotAll(las[keys[i]], las[keys[i + 1]],keys[i+1], volume);
        }

        if (las[keys[i]].pointCount == 0 && i != keys.Count - 1 && i!=0)
        {
            connectLinesNotAll(las[keys[i-1]], las[keys[i + 1]], keys[i + 1], volume);
        }



        //GameObject go = GameObject.Find(volume + "Lines");

        //int numPoints = 0;
        //foreach (Transform child in go.transform)
        //{
        //    if(child.gameObject.activeInHierarchy)
        //        numPoints += child.GetComponent<LineRenderer>().positionCount;
        //}

        //if (numPoints < 3) return null;

        //float[] vertices = new float[numPoints * 3];

        //int i = 0;

        /////// ISSO DEVE SER APAGADO
        //double[][] verticesold = new double[numPoints][];

        //i = 0;
        //foreach (Transform child in go.transform)
        //{
        //    if (child.gameObject.activeInHierarchy)
        //    {
        //        LineRenderer lr = child.GetComponent<LineRenderer>();
        //        for (int j = 0; j < lr.positionCount; j++)
        //        {
        //            Vector3 v = lr.GetPosition(j);
        //            verticesold[i++] =new double[3]{ v.x, v.y, v.z };
        //        }
        //    }
        //}
        //try
        //{
        //    var result = MIConvexHull.ConvexHull.Create(verticesold);

        //    List<Vector3> vertices2 = new List<Vector3>();

        //    i = 0;
        //    foreach (MIConvexHull.DefaultVertex v in result.Points)
        //    {
        //        vertices2.Add(VertexToVector(v));
        //    }
        //    m.vertices = vertices2.ToArray();

        //    foreach (var face in result.Faces)
        //    {
        //        triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[0])));
        //        triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[1])));
        //        triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[2])));
        //    }

        //    m.triangles = triangles.ToArray();
        //    m.RecalculateNormals();
        //}
        //catch (Exception e)
        //{
        //    print(e);
        //}

        //PLUGIN STUFF
        //try
        //{
        //    int outVertLength = 0;
        //    int outTriLength = 0;

        //    IntPtr verts = Marshal.AllocCoTaskMem(sizeof(float) * vertices.Length);
        //    Marshal.Copy(vertices, 0, verts, vertices.Length);

        //    IntPtr outTri = Marshal.AllocCoTaskMem(sizeof(int) * vertices.Length * 3); // we estimate 3 times the n of vertices (its actually probably 2)

        //    ReconstructCloudGP3( verts, outTri, vertices.Length, out outVertLength, out outTriLength);


        //    List<Vector3> vertices2 = new List<Vector3>();
        //    Marshal.Copy(verts, vertices, 0, outVertLength);
        //    for (i = 0; i < outVertLength;)
        //    {
        //        print(vertices[i] + " " +vertices[i + 1] + " " + vertices[i + 2]);
        //        vertices2.Add(new Vector3(vertices[i++], vertices[i++], vertices[i++]));
        //    }
        //    m.vertices = vertices2.ToArray();


        //    int[] cpptriangles = new int[outTriLength];
        //    Marshal.Copy(outTri, cpptriangles, 0, outTriLength);

        //    for (i = 0; i < outTriLength;i++)
        //    {
        //        triangles.Add(cpptriangles[i]);
        //    }

        //    m.triangles = triangles.ToArray();
        //    m.RecalculateNormals();

        //}
        //catch (Exception e)
        //{
        //    print("Deu merda no plugin");
        //    print(e);
        //}

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
        string fullLinesName = _currentVolume + "Lines";
        GameObject parent = GameObject.Find(fullLinesName);
        VolumeLineInfo lines = parent.GetComponent<VolumeLineInfo>(); ;
        lines.removeLine(gameObject.GetComponent<LineRenderer>(), _slicer.Slice);
        lines.updateLines(_slicer.Slice);

        string volume = gameObject.transform.parent.gameObject.name.Replace("Lines", "");
        gameObject.SetActive(false);
        Destroy(gameObject);

   
        UpdateVolume(volume);
    }
}
