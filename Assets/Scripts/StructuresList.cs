using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Structure
{
    public string name;
    public string material;

    public Structure(string name, string material)
    {
        this.name = name;
        this.material = material;
    }
}

public class StructuresList : MonoBehaviour {

    private Main _main;

    private List<Structure> _structures;

    private string filename = "structures.txt";

    public int _currentStructure;
    public string CurrentStructure
    {
        get
        {
            return _structures[_currentStructure].name;
        }
    }

    private int _fakeStructure;
    private float _offset;

    private Rect _sliderArea;
    public Rect SliderArea
    {
        get
        {
            return _sliderArea;
        }
    }

    private GUIStyle _sliderAreaStyle;
    private GUIStyle _structureStyle;
    private GUIStyle _currentStructureStyle;

    public int right = 0;

    private int structureWidth = 200;
    private int structureHeight = 40;

    private float _offsetSpeed;
    private bool _sliding;

    private List<MouseSnapshot> _mouseHistory;
    private float _timeInterval = 0.1f; // in seconds

    // Use this for initialization
    void Start ()
    {
        _main = GetComponent<Main>();

        _structures = new List<Structure>();
        _currentStructure = 0;
        _fakeStructure = _currentStructure;
        _offset = 0;
        _offsetSpeed = 0;
        _sliding = false;
        load();

        _sliderArea = new Rect(0, 0, structureWidth, Screen.height);

        _sliderAreaStyle = new GUIStyle();
        _sliderAreaStyle.normal.background = Slicer.CreateColorTexture(Slicer.ColorFromRGBA(238, 238, 236, 242));

        _structureStyle = new GUIStyle();
        _structureStyle.alignment = TextAnchor.MiddleCenter;
        _structureStyle.normal.textColor = Slicer.ColorFromRGBA(46, 52, 54);
        _structureStyle.normal.background = Slicer.CreateColorTexture(Slicer.ColorFromRGBA(186, 189, 182));

        _currentStructureStyle = new GUIStyle(_structureStyle);
        _currentStructureStyle.normal.textColor = Color.white;
        _currentStructureStyle.normal.background = Slicer.CreateColorTexture(Slicer.ColorFromRGBA(0, 122, 255));

        // velocity

        _mouseHistory = new List<MouseSnapshot>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _sliding = false;

            if (_sliderArea.Contains(Input.mousePosition))
            {
                _sliding = true;
                _offsetSpeed = 0;

                _mouseHistory.Clear();
                _mouseHistory.Add(new MouseSnapshot(DateTime.Now, Input.mousePosition));
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (_sliding)
            {
                float mouseDelta = -(Input.mousePosition.y - _mouseHistory[_mouseHistory.Count - 1].position.y);

                _mouseHistory.Add(new MouseSnapshot(DateTime.Now, Input.mousePosition));

                float mouseYSpeed = -CalcMouseVelocity().y;

                if (_sliding)
                {
                    _offset += mouseDelta;
                    _offsetSpeed = mouseYSpeed;
                }
            }
        }
        else
        {
            // kinectic scrolling
            
            float speedLoss = 2000 * Time.deltaTime;

            if (_sliding)
            {
                if (_offsetSpeed > 0) _offsetSpeed = Mathf.Max(_offsetSpeed - speedLoss, 0);
                else _offsetSpeed = Mathf.Min(_offsetSpeed + speedLoss, 0);

                if (_offsetSpeed == 0)
                {
                    _sliding = false;
                    _currentStructure = _fakeStructure;
                }
                else _offset += _offsetSpeed * Time.deltaTime;
            }
        }

        // change structure

        float halfStructureHeight = structureHeight / 2.0f;

        while (_offset > halfStructureHeight)
        {
            if (_fakeStructure == 0) break;
            _offset -= structureHeight;
            _fakeStructure--;
        }
        while (_offset < -halfStructureHeight)
        {
            if (_fakeStructure == _structures.Count - 1) break;
            _offset += structureHeight;
            _fakeStructure++;
        }

        // lock min & max structures
        if ((_fakeStructure == 0 && _offset > 0) || (_fakeStructure == _structures.Count - 1 && _offset < 0))
        {
            _offset = 0;
            _offsetSpeed = 0;
        }
    }

    void OnGUI()
    {
        if (_main.deviceType == DeviceType.Tablet)
        {
            // background area
            _sliderArea = new Rect(Screen.width - structureWidth - right, 0, structureWidth, Screen.height);
            GUI.Box(_sliderArea, "", _sliderAreaStyle);

            int structuresCount = (int)Mathf.Ceil(Screen.height / structureHeight) + 1;
            int halfStructuresCount = (int)Mathf.Ceil(structuresCount / 2.0f);

            int currentStructurePosY = (int)Mathf.Ceil(Screen.height / 2.0f - structureHeight / 2.0f + _offset);

            // draw current structure
            GUI.Box(new Rect(Screen.width - structureWidth + 1 - right, currentStructurePosY + 1, structureWidth - 2, structureHeight - 2), _structures[_fakeStructure].name, _currentStructureStyle);
            // draw structures up
            for (int i = 1; i <= halfStructuresCount; i++)
            {
                if (_fakeStructure - i < 0) break;
                GUI.Box(new Rect(Screen.width - structureWidth + 1 - right, currentStructurePosY - structureHeight * i + 1, structureWidth - 2, structureHeight - 2), _structures[_fakeStructure - i].name, _structureStyle);
            }
            // draw structures down
            for (int i = 1; i <= halfStructuresCount; i++)
            {
                if (_fakeStructure + i >= _structures.Count) break;
                GUI.Box(new Rect(Screen.width - structureWidth + 1 - right, currentStructurePosY + structureHeight * i + 1, structureWidth - 2, structureHeight - 2), _structures[_fakeStructure + i].name, _structureStyle);
            }
        }
    }
    
    private void load()
    {
        string fullPath = Application.dataPath + System.IO.Path.DirectorySeparatorChar + filename;

        if (File.Exists(fullPath))
        {
            List<string> lines = new List<string>(File.ReadAllLines(fullPath));
            foreach (string line in lines)
            {
                string[] words = line.Split(':');
                if (words.Length == 2)
                    _structures.Add(new Structure(words[0], words[1]));
            }
        }
        else
            throw new Exception("Structures file not found");
    }

    private Vector2 CalcMouseVelocity()
    {
        DateTime now = DateTime.Now;

        while (_mouseHistory[0].time.AddSeconds(_timeInterval) < now)
        {
            _mouseHistory.RemoveAt(0);
        }

        float time = (float)_mouseHistory[_mouseHistory.Count - 1].time.Subtract(_mouseHistory[0].time).TotalSeconds;
        Vector2 distance = _mouseHistory[_mouseHistory.Count - 1].position - _mouseHistory[0].position;

        return distance / time;
    }

    public string GetMaterialName(string structureName)
    {
        foreach(Structure s in _structures)
        {
            if (s.name == structureName)
                return s.material;
        }
        return null;
    }
}
