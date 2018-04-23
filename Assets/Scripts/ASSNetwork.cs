﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASSNetwork : MonoBehaviour {

    public enum ASSPeerType
    {
        server,
        client
    }

    public ASSPeerType peerType;
    private int port;
    private string serverAddress;

    private NetworkView _networkView;
    private ConfigProperties _config;

    public bool showGUI = false;

    void Start()
    {
        Application.runInBackground = true;
        _networkView = GetComponent<NetworkView>();
        _config = GameObject.Find("Main").GetComponent<ConfigProperties>();

        port = _config.port;
        serverAddress = _config.address;
        peerType = _config.networkPeerType;

        if (peerType == ASSPeerType.server)
        {
            Network.InitializeServer(2, port, false);
        }
        else
        {
            Network.Connect(serverAddress, port);
        }

    }

    void Update()
    {

    }

    void OnGUI()
    {
        if (!showGUI) return;


        int left = 10;
        int top = 10;
        int lineSize = 20;

        GUI.Label(new Rect(left, top, 200, lineSize), "Network Port: "); left += 160;
        GUI.Label(new Rect(left, top, 100, lineSize), "" + port); left = 10;

        top += lineSize;

        GUI.Label(new Rect(left, top, 200, lineSize), "Server Address: "); left += 160;
        GUI.Label(new Rect(left, top, 100, lineSize), "" + serverAddress); left = 10;

        top += lineSize;

        GUI.Label(new Rect(left, top, 200, lineSize), "Number of clients: "); left += 160;
        GUI.Label(new Rect(left, top, 100, lineSize), "" + Network.connections.Length); left = 10;
    }

    [RPC]
    void RPC_broadcastLine(string userID, int slice, string structure, Vector3[] line)
    {

    }
    public void broadcastLine(string userID, int slice, string structure, Vector3[] line)
    {

    }

}
