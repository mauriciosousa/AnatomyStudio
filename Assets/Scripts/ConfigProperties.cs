using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConfigProperties : MonoBehaviour {

    public string configFilename = "config.txt";

    public string configFilenameFullPath
    {
        get
        {
            return Application.dataPath + System.IO.Path.DirectorySeparatorChar + configFilename;
        }
    }

    public ASSNetwork.ASSPeerType networkPeerType
    {
        get
        {
            return (ASSNetwork.ASSPeerType) Enum.Parse(typeof(ASSNetwork.ASSPeerType), _load("peer.type"));
        }
    }

    public DeviceType device
    {
        get
        {
            return (DeviceType)Enum.Parse(typeof(DeviceType), _load("device.type"));
        }
    }

    public int port
    {
        get
        {
            return int.Parse(_load("server.port"));
        }
    }

    public string address
    {
        get
        {
            return _load("server.address");
        }
    }

    private string _load(string property)
    {
        if (File.Exists(configFilenameFullPath))
        {
            List<string> lines = new List<string>(File.ReadAllLines(configFilenameFullPath));
            foreach (string line in lines)
            {
                if (line.Split('=')[0] == property)
                {
                    Debug.Log("Found: " + property + " - " + line.Split('=')[1]);
                    return line.Split('=')[1];
                }
            }
            throw new Exception(property + ": Not Found");
        }
        else
            throw new Exception(property + ": Not Found");
    }
}
