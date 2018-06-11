using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationHandle : MonoBehaviour
{
    private Transform _translationHandle;
    private Transform _tabletop;

    private RotateOtherAround _rotate;

    private bool _movingHandle;

    // Use this for initialization
    void Start ()
    {
        _translationHandle = GameObject.Find("TranslationHandle").transform;
        _tabletop = GameObject.Find("Tabletop").transform;

        _rotate = GetComponent<RotateOtherAround>();

        _movingHandle = false;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (!_movingHandle)
            _rotate.SetPosition(_translationHandle.position - _tabletop.right.normalized * SliceIndicator.frameWidth);
    }

    public void StartMoving()
    {
        _movingHandle = true;
    }

    public void EndMoving()
    {
        _movingHandle = false;
    }
}
