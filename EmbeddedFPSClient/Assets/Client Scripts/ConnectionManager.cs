﻿using System;
using System.Net;
using DarkRift;
using DarkRift.Client.Unity;
using UnityEngine;

[RequireComponent(typeof(UnityClient))]
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;

    [Header("Settings")]
    [SerializeField]
    private string ipAdress;
    [SerializeField]
    private int port;
    private int udport = 4297;

    [Header("References")]
    [SerializeField]
    private LoginManager loginManager;

    public UnityClient Client { get; private set; }

    public ushort PlayerId { get; set; }

    public LobbyInfoData LobbyInfoData { get; set; }

    public delegate void OnConnectedDelegate();
    public event OnConnectedDelegate OnConnected;
    void Awake()
    {
        Debug.Log("Has been called");
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
        Client = GetComponent<UnityClient>();
    }

    void Start()
    {
        Client.ConnectInBackground(IPAddress.Parse(ipAdress), port,udport,true, ConnectCallback);
    }

    private void ConnectCallback(Exception exception)
    {
        if (Client.Connected)
        {
            OnConnected?.Invoke();
        }
        else
        {
            Debug.LogError("Unable to connect to server.");
        }
    }
}
