﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eraser : MonoBehaviour {

    private Draw _draw;

    private bool _enabled;
    private bool _erasing;

    private GameObject[] _lines;

    private Vector3 mouseA;

    private GUIStyle _areaStyle;
    private GUIStyle _optionStyle;
    private GUIStyle _selectedStyle;

    // Use this for initialization
    void Start ()
    {
        _draw = GetComponent<Draw>();
        _enabled = false;
        _erasing = false;

        _areaStyle = new GUIStyle();
        _areaStyle.normal.background = Utils.CreateColorTexture(238, 238, 236, 242);

        _optionStyle = new GUIStyle();
        _optionStyle.alignment = TextAnchor.MiddleCenter;
        _optionStyle.normal.textColor = Utils.ColorFromRGBA(46, 52, 54);
        _optionStyle.normal.background = Utils.CreateColorTexture(186, 189, 182);

        _selectedStyle = new GUIStyle(_optionStyle);
        _selectedStyle.normal.textColor = Color.white;
        _selectedStyle.normal.background = Utils.CreateColorTexture(0, 122, 255);
    }
	
	// Update is called once per frame
	void Update ()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if (_enabled)
            {
                if (!Utils.GUIContains(Input.mousePosition))
                {
                    _erasing = true;
                    mouseA = Utils.MouseToWorld(Input.mousePosition);
                }
            }
        }
		else if(Input.GetMouseButton(0))
        {
            if(_erasing)
            {
                Vector3 mouseB = Utils.MouseToWorld(Input.mousePosition);

                if (mouseA != mouseB)
                {
                    if (_draw.CheckTolerance(mouseA, mouseB))
                    {
                        foreach (GameObject go in _lines)
                        {
                            if (go != null)
                            {
                                LineRenderer lr = go.GetComponent<LineRenderer>();
                                Vector3[] points = new Vector3[lr.positionCount];
                                lr.GetPositions(points);

                                Vector3 localMouseA = go.transform.worldToLocalMatrix.MultiplyPoint(mouseA);
                                Vector3 localMouseB = go.transform.worldToLocalMatrix.MultiplyPoint(mouseB);

                                for (int i = 1; i < lr.positionCount; i++)
                                {
                                    // check if same slice
                                    if (localMouseA.z != points[i].z) break;

                                    Vector2 intersection;
                                    if (LineSegmentsIntersection(localMouseA, localMouseB, points[i], points[i - 1], out intersection))
                                    {
                                        _draw.RemoveLine(go);
                                        break;
                                    }
                                }
                            }
                        }

                        mouseA = mouseB;
                    }
                }
            }
        }
        else if(Input.GetMouseButtonUp(0))
        {
            _erasing = false;
        }

        // test Disable
        if (Input.GetKeyDown(KeyCode.D))
        {
            _enabled = !_enabled;

            if (_enabled)
            {
                _lines = GameObject.FindGameObjectsWithTag("DrawLine");
            }
        }

        // test intersection
        if (Input.GetKeyDown(KeyCode.I))
        {
            Vector2 i;
            print(LineSegmentsIntersection(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(1, 0), out i));
        }
    }

    void OnGUI()
    {
        int border = 20;
        int right = 150 + border;
        int height = 40;
        int width = 50;

        if (GUI.Button(new Rect(Screen.width - width * 2 - right, border, width * 2, height), "", _areaStyle))
        {
            _enabled = !_enabled;
            _draw.Enabled = !_enabled;

            if (_enabled)
            {
                _lines = GameObject.FindGameObjectsWithTag("DrawLine");
            }
        }

        GUI.Box(new Rect(Screen.width - width * 2 - right + 1, border + 1, width - 2, height - 2), "Rubber", _enabled? _selectedStyle : _optionStyle);

        GUI.Box(new Rect(Screen.width - width - right + 1, border + 1, width - 2, height - 2), "Pencil", _enabled ? _optionStyle : _selectedStyle);
    }

    public static bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
        {
            return false;
        }

        var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }
}
