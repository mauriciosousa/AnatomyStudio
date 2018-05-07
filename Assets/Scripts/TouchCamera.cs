using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchCamera : MonoBehaviour {

    public MeshRenderer sliceMesh;

    private int[] _touchIDs;
    private Vector2[] _lastTouchPos;
    private int _lastTouchCount;

    private Camera _camera;

    // legacy mouse control
    public bool enableMouse;
    private Vector2 _lastMousePos;

	// Use this for initialization
	void Start ()
    {
        _lastTouchPos = new Vector2[2];
        _touchIDs = new int[2];
        _lastTouchCount = 0;

        _camera = this.GetComponent<Camera>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if(Input.touchCount == 2)
        {
            if(_lastTouchCount != Input.touchCount)
            {
                _touchIDs[0] = Input.touches[0].fingerId;
                _touchIDs[1] = Input.touches[1].fingerId;
                _lastTouchPos[0] = Input.touches[0].position;
                _lastTouchPos[1] = Input.touches[1].position;
            }
            else
            {
                Plane pl = new Plane(sliceMesh.transform.forward, sliceMesh.transform.position);

                // get touches info
                Vector2[] newTouchPos = new Vector2[2];
                foreach (Touch t in Input.touches)
                {
                    if (t.fingerId == _touchIDs[0]) newTouchPos[0] = t.position;
                    else if (t.fingerId == _touchIDs[1]) newTouchPos[1] = t.position;
                }
                Vector3 lastTouch1Pos = ScreenToPlane(_lastTouchPos[0], pl);

                // set scale
                float lastDistance = Vector2.Distance(_lastTouchPos[0], _lastTouchPos[1]);
                float newDistance = Vector2.Distance(newTouchPos[0], newTouchPos[1]);
                _camera.orthographicSize *= lastDistance / newDistance;

                // set position
                Vector3 newTouch1Pos = ScreenToPlane(newTouchPos[0], pl);
                this.transform.position -= newTouch1Pos - lastTouch1Pos;

                // set orientation
                lastTouch1Pos = ScreenToPlane(_lastTouchPos[0], pl);
                Vector3 lastTouch2Pos = ScreenToPlane(_lastTouchPos[1], pl);
                newTouch1Pos = ScreenToPlane(newTouchPos[0], pl);
                Vector3 newTouch2Pos = ScreenToPlane(newTouchPos[1], pl);

                Vector3 oldVector = lastTouch2Pos - lastTouch1Pos;
                Vector3 newVector = newTouch2Pos - newTouch1Pos;

                this.transform.RotateAround(newTouch1Pos, Vector3.Cross(newVector, oldVector), Vector3.Angle(newVector, oldVector));

                // validate position
                ValidatePosition();

                // store touches position
                _lastTouchPos[0] = newTouchPos[0];
                _lastTouchPos[1] = newTouchPos[1];
            }
        }

        _lastTouchCount = Input.touchCount;

        // ******
        // legacy mouse control
        // ******
        if (enableMouse)
        {
            if (Input.GetMouseButtonDown(1))
            {
                _lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(1))
            {
                Plane pl = new Plane(sliceMesh.transform.forward, sliceMesh.transform.position);

                // get last mouse position
                Vector3 lastPoint = ScreenToPlane(_lastMousePos, pl);

                // set scale
                if (Input.GetKeyDown(KeyCode.Z)) // zoom in
                {
                    _camera.orthographicSize *= 0.9f;
                }
                if (Input.GetKeyDown(KeyCode.X)) // zoom out
                {
                    _camera.orthographicSize *= 1.1f;
                }

                // set position
                Vector3 newPoint = ScreenToPlane(Input.mousePosition, pl);

                this.transform.position -= newPoint - lastPoint;

                // set orientation
                if (Input.GetKeyDown(KeyCode.A))
                {
                    this.transform.RotateAround(newPoint, this.transform.forward, -45);
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    this.transform.RotateAround(newPoint, this.transform.forward, 45);
                }

                // validate position
                ValidatePosition();

                // store mouse position
                _lastMousePos = Input.mousePosition;
            }
        }
    }

    private void ValidatePosition()
    {
        float enter;
        Plane pl = new Plane(sliceMesh.transform.forward, sliceMesh.transform.position);
        Ray ray = new Ray(this.transform.position, this.transform.forward);
        pl.Raycast(ray, out enter);
        Vector3 cameraPoint = ray.GetPoint(enter);
        Vector3 closestPoint = pl.ClosestPointOnPlane(sliceMesh.bounds.ClosestPoint(cameraPoint));
        this.transform.position += closestPoint - cameraPoint;
    }

    private Vector3 ScreenToPlane(Vector3 screenPoint, Plane plane)
    {
        float enter;
        Ray ray = _camera.ScreenPointToRay(screenPoint);
        if (plane.Raycast(ray, out enter)) return ray.GetPoint(enter);
        else return Vector3.positiveInfinity;
    }
}
