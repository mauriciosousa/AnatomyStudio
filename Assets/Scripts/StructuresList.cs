using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Structure
{
    public string name;
    public string material;
    public GUIStyle guicolor;

    public Structure(string name, string materialName)
    {
        this.name = name;
        this.material = materialName;

        Material material = Resources.Load("Materials/" + materialName + "Line", typeof(Material)) as Material;

        this.guicolor = new GUIStyle();
        this.guicolor.normal.background = Utils.CreateColorTexture(material.color);
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
            if (_hidden) return _sliderArea;
            else return new Rect(0, 0, Screen.width, Screen.height);
        }
    }

    private GUIStyle _sliderAreaStyle;
    private GUIStyle _structureStyle;
    private GUIStyle _currentStructureStyle;
    private GUIStyle _textStyle;
    private GUIStyle _currentTextStyle;

    private int right = 0;
    private bool _hidden;
    private bool _wasHidden;

    private int structureWidth = 150;
    private int structureHeight = 40;

    private float _offsetSpeed;
    private bool _sliding;
    private float _structuresChanged;

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
        _structuresChanged = 0;
        _hidden = true;
        _wasHidden = true;
        load();

        _sliderArea = new Rect(0, 0, structureWidth, Screen.height);

        _sliderAreaStyle = new GUIStyle();
        _sliderAreaStyle.normal.background = Utils.CreateColorTexture(238, 238, 236, 242);

        _structureStyle = new GUIStyle();
        _structureStyle.alignment = TextAnchor.MiddleLeft;
        _structureStyle.normal.textColor = Utils.ColorFromRGBA(46, 52, 54);
        _structureStyle.normal.background = Utils.CreateColorTexture(186, 189, 182);

        _textStyle = new GUIStyle();
        _textStyle.alignment = TextAnchor.MiddleLeft;
        _textStyle.normal.textColor = Utils.ColorFromRGBA(46, 52, 54);

        _currentStructureStyle = new GUIStyle(_structureStyle);
        _currentStructureStyle.normal.textColor = Color.white;
        _currentStructureStyle.normal.background = Utils.CreateColorTexture(0, 122, 255);

        _currentTextStyle = new GUIStyle(_textStyle);
        _currentTextStyle.normal.textColor = Color.white;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _sliding = false;

            if (_sliderArea.Contains(Input.mousePosition))
            {
                _wasHidden = _hidden;

                if (_hidden)
                {
                    _hidden = false;
                }
                else
                {
                    _sliding = true;
                    _offsetSpeed = 0;
                    _structuresChanged = 0;
                }
            }
            else
            {
                if (!_hidden) _hidden = true;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (_sliding)
            {
                if (_sliding)
                {
                    _offset += -Utils.MouseDelta.y;
                    _offsetSpeed = -Utils.MouseVelocity.y;
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
            _structuresChanged++;
        }
        while (_offset < -halfStructureHeight)
        {
            if (_fakeStructure == _structures.Count - 1) break;
            _offset += structureHeight;
            _fakeStructure++;
            _structuresChanged++;
        }

        // lock min & max structures
        if ((_fakeStructure == 0 && _offset > 0) || (_fakeStructure == _structures.Count - 1 && _offset < 0))
        {
            _offset = 0;
            _offsetSpeed = 0;
        }

        // hide / show
        if (_hidden) right = -(structureWidth - structureHeight + 1);
        else right = 0;
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
            StructureGUIButton(new Rect(Screen.width - structureWidth + 1 - right, currentStructurePosY + 1, structureWidth - 2, structureHeight - 2), _fakeStructure, true);

            // draw structures up
            for (int i = 1; i <= halfStructuresCount; i++)
            {
                if (_fakeStructure - i < 0) break;
                if (StructureGUIButton(new Rect(Screen.width - structureWidth + 1 - right, currentStructurePosY - structureHeight * i + 1, structureWidth - 2, structureHeight - 2), _fakeStructure - i))
                {
                    if (_structuresChanged == 0 && !_wasHidden)
                    {
                        _fakeStructure = _fakeStructure - i;
                    }
                }
            }
            // draw structures down
            for (int i = 1; i <= halfStructuresCount; i++)
            {
                if (_fakeStructure + i >= _structures.Count) break;
                if (StructureGUIButton(new Rect(Screen.width - structureWidth + 1 - right, currentStructurePosY + structureHeight * i + 1, structureWidth - 2, structureHeight - 2), _fakeStructure + i))
                {
                    if (_structuresChanged == 0 && !_wasHidden)
                    {
                        _fakeStructure = _fakeStructure + i;
                    }
                }
            }
        }
    }
    
    private bool StructureGUIButton(Rect rect, int number, bool selected = false)
    {
        bool ret = GUI.Button(rect, "", selected? _currentStructureStyle : _structureStyle);

        GUI.Box(new Rect(rect.x + 5, rect.y + 5, rect.height - 10, rect.height - 10), "", _structures[number].guicolor);
        GUI.Box(new Rect(rect.x + rect.height + 5, rect.y + 5, rect.width - rect.height - 10, rect.height - 10), _structures[number].name, selected ? _currentTextStyle : _textStyle);

        return ret;
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
