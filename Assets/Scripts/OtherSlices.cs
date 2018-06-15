using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherSlices : MonoBehaviour
{
    private Dictionary<string, int> _slices;

    private Transform _tabletop;

	void Start ()
    {
        _slices = new Dictionary<string, int>();

        _tabletop = GameObject.Find("Tabletop").transform;
    }
	
	void Update ()
    {
		
	}

    public void SetSlice(string userID, int slice)
    {
        _slices[userID] = slice;

        GameObject sliceIndicator = GameObject.Find(userID);

        if(sliceIndicator == null)
        {
            sliceIndicator = Instantiate(Resources.Load("Prefabs/SliceIndicator", typeof(GameObject))) as GameObject;
            sliceIndicator.name = userID;
            sliceIndicator.transform.Find("UserName").gameObject.GetComponent<TextMesh>().text = "| " + userID;
            sliceIndicator.transform.Find("UserName").gameObject.SetActive(true);
            sliceIndicator.transform.parent = _tabletop;
            sliceIndicator.transform.localPosition = Vector3.zero;
            sliceIndicator.transform.localEulerAngles = new Vector3(0, -90, 0);
            sliceIndicator.transform.Find("HandlePlaceholder").gameObject.SetActive(false);
            sliceIndicator.transform.Find("SlicePreview").gameObject.SetActive(false);
        }

        sliceIndicator.GetComponent<SliceIndicator>().Slice = slice;
    }

    public string GetUserInSlice(int slice)
    {
        foreach(KeyValuePair<string, int> p in _slices)
        {
            if (p.Value == slice)
                return p.Key;
        }
        return "";
    }

    public int GetNearestSliceWithUser(int slice)
    {
        int minDiff = int.MaxValue;
        int retSlice = -1;

        foreach (int i in _slices.Values)
        {
            int diff = Mathf.Abs(i - slice);

            if (diff < minDiff)
            {
                minDiff = diff;
                retSlice = i;
            }
        }

        return retSlice;
    }
}
