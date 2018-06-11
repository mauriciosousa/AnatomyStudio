using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class Storage : MonoBehaviour {

    private string _dataPath = "data.txt";
    private string _dataFile = "data.txt";


    public ASSNetwork network;

    public char separator = '#';

    void Start () {

        _dataPath = gameObject.GetComponent<ConfigProperties>().storageDataFile;
	}

    void Update()
    {
        if (((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyDown(KeyCode.S)) || Input.GetKeyDown(KeyCode.G))
        {
            Save();
        }
    }

    public void Load()
    {
        string[] lines = null;
        if (network.peerType == ASSNetwork.ASSPeerType.server)
        {
            if (File.Exists(_dataPath))
            {
                lines = File.ReadAllLines(_dataPath);
                _loadLines(lines);
            }
            else
            {
                throw new System.Exception("Cannot find file: " + _dataPath);
            }
        }
        else
        {
            StartCoroutine(HTTPGetDataFile());
        }
    }

    public void Save()
    {
        if (network.peerType != ASSNetwork.ASSPeerType.server) return;

        GameObject[] lines = GameObject.FindGameObjectsWithTag("DrawLine");

        SliceLoader sl = gameObject.GetComponent<SliceLoader>();

        List<string> dataLines = new List<string>();

        foreach (GameObject line in lines)
        {
            string structureName = line.transform.parent.name.Replace("Lines", "");
            string lineID = line.name;

            LineRenderer lr = line.GetComponent<LineRenderer>();
            Vector3[] positions = new Vector3[lr.positionCount];
            lr.GetPositions(positions);

            int slice = Mathf.RoundToInt(positions[0].z / sl.SliceDepth) + 1;

            string separatorStr = "" + separator;

            string dataLine = "";
            dataLine += structureName + separatorStr;
            dataLine += lineID + separatorStr;
            print("SLICE " + slice);
            dataLine += slice;
            foreach (Vector3 p in positions)
            {
                dataLine += separatorStr + p.x + separatorStr + p.y + separatorStr + p.z;
            }
            dataLines.Add(dataLine);
        }

        if (lines.Length > 0)
        {

            FileInfo fileInfo = new FileInfo(_dataPath);
            if (!fileInfo.Exists)
                Directory.CreateDirectory(fileInfo.Directory.FullName);

            Debug.Log("Writing to " + _dataPath);
            File.WriteAllLines(_dataPath, dataLines.ToArray(), Encoding.UTF8);
        }
    }

    private void _loadLines(string[] lines)
    {
        if (lines != null)
        {
            foreach (string line in lines)
            {
                string[] values = line.Split(separator);

                string structureName = values[0];
                string lineID = values[1];
                int slice = int.Parse(values[2]);

                List<Vector3> positions = new List<Vector3>();
                for (int i = 3; i < values.Length;)
                {
                    Vector3 position = new Vector3();
                    position.x = float.Parse(values[i++]);
                    position.y = float.Parse(values[i++]);
                    position.z = float.Parse(values[i++]);
                    positions.Add(position);
                }

                print("STRUCTURE " + structureName);
                gameObject.GetComponent<Draw>().AddLine(lineID, slice, structureName, positions.ToArray());
            }
        }
    }

    private IEnumerator HTTPGetDataFile()
    {
        SliceLoader loader = GetComponent<SliceLoader>();
         
        string url = "http://" + loader.SlicesHTTP + "/Data/" + _dataFile;

        using (WWW www = new WWW(url))
        {
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                string strLines = www.text.Replace(System.Environment.NewLine, "\n");

                string[] lines = strLines.Split('\n');
                _loadLines(lines);
                Debug.Log("Slices loaded");
            }
            else
            {
                Debug.Log("Slice donwload error: " + www.error);
            }
        }
    }
}
