using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ConnectPoint
{
    first, last
}

public class Circle
{
    public Circle()
    {
        lines = new List<LineInfo>();
    }

    public List<LineInfo> lines;
    public int lineStartID;
    public int pointStartID;
    public int directionClockwise;
    public int pointCount;

    public void nextPoint(ref int id, ref LineInfo linein,ref int inc)
    {
        if((linein.line.positionCount -1) == id && inc ==1)
        {
            id = linein.NextLinePoint == ConnectPoint.first ? 0 : linein.NextLine.line.positionCount-1;
            inc = id == 0 ? 1 : -1;
            linein = linein.NextLine;
        }
        else if (id==0 && inc ==-1)
        {
            id = linein.PrevLinePoint == ConnectPoint.first ? 0 : linein.PrevLine.line.positionCount-1;
            inc = id == 0 ? 1 : -1;
            linein = linein.PrevLine;
        }
        else
        {
            id += inc;
        }
    }

    public void remove(LineRenderer lr)
    {
        int i = 0;
        foreach (LineInfo li in lines)
            if (lr == li.line)
            {
                break;
            }
            else
            {
                i++;
            }
        lines.RemoveAt(i);
    }
}

public class LineInfo
{
    
    public LineInfo(LineRenderer lr)
    {
        line = lr;
    }
    public LineRenderer line;
    public LineInfo NextLine;
    public ConnectPoint NextLinePoint;
    public LineInfo PrevLine;
    public ConnectPoint PrevLinePoint;
}

public class VolumeLineInfo : MonoBehaviour {

	// Use this for initialization
    Dictionary<int, Circle> _linesAtSlice = new Dictionary<int, Circle>();

    public Dictionary<int, Circle> LinesAtSlice
    {
        get
        {
            return _linesAtSlice;
        }

        set
        {
            _linesAtSlice = value;
        }
    }

    public void updateLines(int slice)
    {
        if (_linesAtSlice[slice].lines.Count == 0)
        {
            return;
        }

        LineInfo maxLine = null;
        LineInfo minLine = null;
        float thisSliceZ = 0;

        // begin cenas DM

        List<LineInfo> usedLines = new List<LineInfo>();

        LineInfo firstLine = _linesAtSlice[slice].lines[0];
        ConnectPoint firstCP = ConnectPoint.first;
        LineInfo currentLine = firstLine;
        ConnectPoint currentCP = ConnectPoint.last;
        LineInfo lastLine;
        ConnectPoint lastCP;

        while (currentLine != null)
        {
            usedLines.Add(currentLine);

            Vector3 point = currentLine.line.GetPosition(currentCP == ConnectPoint.first? 0 : currentLine.line.positionCount - 1);

            float mindist = float.MaxValue;
            minLine = null;
            ConnectPoint cp = ConnectPoint.first;

            foreach(LineInfo li in _linesAtSlice[slice].lines)
            {
                if (usedLines.Contains(li)) continue;

                float dist0 = (li.line.GetPosition(0) - point).magnitude;
                float distn = (li.line.GetPosition(li.line.positionCount - 1) - point).magnitude;
                if (dist0 < mindist)
                {
                    mindist = dist0;
                    minLine = li;
                    cp = ConnectPoint.first;
                }
                if (distn < mindist)
                {
                    mindist = distn;
                    minLine = li;
                    cp = ConnectPoint.last;
                }
            }

            lastLine = currentLine;
            lastCP = currentCP;

            if (minLine == null)
            {
                currentLine = firstLine;
                currentCP = firstCP;
            }
            else
            {
                currentLine = minLine;
                currentCP = cp;
            }

            // perform connection

            if (lastCP == ConnectPoint.last)
            {
                lastLine.NextLine = currentLine;
                lastLine.NextLinePoint = currentCP;
            }
            else
            {
                lastLine.PrevLine = currentLine;
                lastLine.PrevLinePoint = currentCP;
            }

            if (currentCP == ConnectPoint.last)
            {
                currentLine.NextLine = lastLine;
                currentLine.NextLinePoint = lastCP;
            }
            else
            {
                currentLine.PrevLine = lastLine;
                currentLine.PrevLinePoint = lastCP;
            }

            // just to break cycle :P

            currentLine = minLine;
            currentCP = currentCP == ConnectPoint.first ? ConnectPoint.last : ConnectPoint.first;
        }

        // end cenas DM

        /*foreach (LineInfo lr in _linesAtSlice[slice].lines)
        {

            Vector3 lastPoint = lr.line.GetPosition(lr.line.positionCount - 1);
            float mindist = float.MaxValue;
            minLine = lr;

            ConnectPoint cp = ConnectPoint.first;
            foreach (LineInfo lr2 in _linesAtSlice[slice].lines)
            {
                if (lr2 == lr) continue;

                float dist0 = (lr2.line.GetPosition(0) - lastPoint).magnitude;
                float distn = (lr2.line.GetPosition(lr2.line.positionCount - 1) - lastPoint).magnitude;
                if (dist0 < mindist)
                {
                    mindist = dist0;
                    minLine = lr2;
                    cp = ConnectPoint.first;
                }
                if (distn < mindist)
                {
                    mindist = distn;
                    minLine = lr2;
                    cp = ConnectPoint.last;
                }
            }

            lr.NextLine = minLine;
            lr.NextLinePoint = cp;

            Vector3 firstPoint = lr.line.GetPosition(0);
            mindist = float.MaxValue;
            minLine = lr;
            cp = ConnectPoint.last;
            foreach (LineInfo lr2 in _linesAtSlice[slice].lines)
            {
                if (lr2 == lr) continue;

                float dist0 = (lr2.line.GetPosition(0) - firstPoint).magnitude;
                float distn = (lr2.line.GetPosition(lr2.line.positionCount - 1) - firstPoint).magnitude;
                if (dist0 < mindist)
                {
                    mindist = dist0;
                    minLine = lr2;
                    cp = ConnectPoint.first;
                }
                if (distn < mindist)
                {
                    mindist = distn;
                    minLine = lr2;
                    cp = ConnectPoint.last;
                }
            }
            lr.PrevLine = minLine;
            lr.PrevLinePoint = cp;
        }*/

        float maxY = float.MinValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;

        LineInfo linea = _linesAtSlice[slice].lines[0];
        List<LineInfo> checkedLines = new List<LineInfo>();

        int i = 0;
        int inc = 1;
        int count = 0;

        //find maxes and mins
        while (checkedLines.Count < _linesAtSlice[slice].lines.Count)
        {

            for (; i < linea.line.positionCount && i >= 0; i += inc)
            {
                if (linea.line.GetPosition(i).x > maxX)
                {
                    maxX = linea.line.GetPosition(i).x;
                    thisSliceZ = linea.line.GetPosition(i).z;
                }

                if (linea.line.GetPosition(i).y > maxY)
                {
                    maxY = linea.line.GetPosition(i).y;
                }
                if (linea.line.GetPosition(i).y < minY)
                {
                    minY = linea.line.GetPosition(i).y;
                }
            }

            checkedLines.Add(linea);
            count += linea.line.positionCount;
            //go to prev line
            if (i == 0)
            {
                i = linea.PrevLinePoint == ConnectPoint.first ? 0 : linea.PrevLine.line.positionCount - 1;
                inc = linea.PrevLinePoint == ConnectPoint.first ? 1 : -1;
                linea = linea.PrevLine;
            }
            //go to next line
            else
            {
                i = linea.NextLinePoint == ConnectPoint.first ? 0 : linea.NextLine.line.positionCount - 1;
                inc = linea.NextLinePoint == ConnectPoint.first ? 1 : -1;
                linea = linea.NextLine;
            }

            //if closed circle
            if (checkedLines.Contains(linea)) break;
        }

        _linesAtSlice[slice].pointCount = count;

        //find maxes and mins
        int maxI = 0;
        int minI = 0;
        minLine = null;
        maxLine = null;
        float minDist = float.MaxValue;
        float maxDist = float.MaxValue;

        linea = _linesAtSlice[slice].lines[0];
        i = 0;
        inc = 1;
        Vector3 bbTop = new Vector3(maxX, maxY, thisSliceZ);
        Vector3 bbBot = new Vector3(maxX, minY, thisSliceZ);

        int j = 0;
        while (j < count)
        {
            //point closest to bbTop
            if ((linea.line.GetPosition(i) - bbTop).sqrMagnitude < maxDist)
            {
                maxLine = linea;
                maxI = i;
                maxDist = (linea.line.GetPosition(i) - bbTop).sqrMagnitude;
            }
            //point closest to bbBottom
            if ((linea.line.GetPosition(i) - bbBot).sqrMagnitude < minDist)
            {
                minLine = linea;
                minI = i;
                minDist = (linea.line.GetPosition(i) - bbBot).sqrMagnitude;
            }

            _linesAtSlice[slice].nextPoint(ref i, ref linea, ref inc);
            j++;
        }

        _linesAtSlice[slice].lineStartID = _linesAtSlice[slice].lines.IndexOf(maxLine);
        _linesAtSlice[slice].pointStartID = maxI;

        int forwardSum = 0;
        i = maxI;
        inc = +1;
        linea = maxLine;
        j = 0;
        while (j < count && !(linea == minLine && i == minI))
        {
            forwardSum++;
            _linesAtSlice[slice].nextPoint(ref i, ref linea, ref inc);
            j++;
        }

        int backSum = 0;
        i = maxI;
        inc = -1;
        linea = maxLine;
        j = 0;

        while (j < count && !(linea == minLine && i == minI))
        {
            backSum++;
            _linesAtSlice[slice].nextPoint(ref i, ref linea, ref inc);
        }

        if (forwardSum > backSum)
        {
            _linesAtSlice[slice].directionClockwise = 1;
        }
        else
        {
            _linesAtSlice[slice].directionClockwise = -1;
        }
    }

    public void removeLine(LineRenderer line, int slice)
    {
        _linesAtSlice[slice].remove(line);
        _linesAtSlice[slice].pointCount = 0;
        //if (_linesAtSlice[slice].lines.Count == 0)
        //    _linesAtSlice.Remove(slice);
    }

    public void addLine(int slice,LineRenderer line)
    {
        if (!_linesAtSlice.ContainsKey(slice))
        {
            _linesAtSlice.Add(slice, new Circle());
        }

        LineInfo li2 = new LineInfo(line);
        _linesAtSlice[slice].lines.Add(li2);
    }
}
