﻿using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(PlayerLogic))]
public class ServerPlayer : MonoBehaviour
{
    public float moveSpeed;
    public float sprintSpeed;
    public float jumpHeight = 1.2f;
    public bool grounded = true;
    public GameObject cinemachineCameraTarget;
    public float topClamp = 70.0f;
    public float bottomClamp = -30.0f;

    private ClientConnection clientConnection;
    private Room room;

    private PlayerStateData currentPlayerStateData;

    [SerializeField]
    private Buffer<PlayerInputData> inputBuffer = new Buffer<PlayerInputData>(1, 2);

    private int health;

    public PlayerLogic PlayerLogic { get; private set; }
    public uint InputTick { get; private set; }
    public IClient Client { get; private set; }
    public PlayerStateData CurrentPlayerStateData => currentPlayerStateData;
    public List<PlayerStateData> PlayerStateDataHistory { get; } = new List<PlayerStateData>();

    [SerializeField]
    private PlayerInputData[] inputs;

    [SerializeField]
    private float syncSpeed = 10f;

    CharacterController characterController;

    void Awake()
    {
        PlayerLogic = GetComponent<PlayerLogic>();
    }

    public void Initialize(Vector3 position, ClientConnection clientConnection)
    {
        this.clientConnection = clientConnection;
        room = clientConnection.Room;
        Client = clientConnection.Client;
        this.clientConnection.Player = this;
        characterController = GetComponent<CharacterController>();
        currentPlayerStateData = new PlayerStateData(Client.ID,0, position, Quaternion.identity.eulerAngles);
        InputTick = room.ServerTick;
        health = 100;

        var playerSpawnData = room.GetSpawnDataForAllPlayers();
        using (Message m = Message.Create((ushort)Tags.GameStartDataResponse, new GameStartData(playerSpawnData, room.ServerTick)))
        {
            Client.SendMessage(m, SendMode.Reliable);
        }
    }

    public void RecieveInput(PlayerInputData input)
    {
        inputBuffer.Add(input);
    }

    public void TakeDamage(int value)
    {
        health -= value;
        if (health <= 0)
        {
            health = 100;
            currentPlayerStateData.Position = new Vector3(0,1,0) + transform.position;
            currentPlayerStateData.Gravity = 0;
            transform.position = currentPlayerStateData.Position;
        }
        room.UpdatePlayerHealth(this, (byte)health);
    }

    public void PlayerPreUpdate()
    {
        inputs = inputBuffer.Get();
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i].Keyinputs[2])
            {
                room.PerformShootRayCast(inputs[i].Time, this);
                break;
            }
        }
    }

    public PlayerStateData PlayerUpdate()
    {
        float inputX=0;
        float inputY = 0;
        bool sprint = false;

        if (inputs.Length > 0)
        {
            PlayerInputData input = inputs.First();
            InputTick++;

            for (int i = 1; i < inputs.Length; i++)
            {
                InputTick++;
                for (int j = 0; j < input.Keyinputs.Length; j++)
                {
                    input.Keyinputs[j] = input.Keyinputs[j] || inputs[i].Keyinputs[j];
                }
                input.LookDirection = inputs[i].LookDirection;
                inputX = input.MovementInputs[0];
                inputY = input.MovementInputs[1];
                sprint = input.Keyinputs[1];
            }

            currentPlayerStateData = PlayerLogic.GetNextFrameData(input, currentPlayerStateData);
        }
        
        PlayerStateDataHistory.Add(currentPlayerStateData);
        if (PlayerStateDataHistory.Count > 10)
        {
            PlayerStateDataHistory.RemoveAt(0);
        }

        Vector3 newPosition= transform.position+ transform.forward * inputY + transform.right * inputX;

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime);

        transform.rotation = Quaternion.Euler(0, currentPlayerStateData.LookDirection.y,0);
        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(currentPlayerStateData.LookDirection.x, 0, 0);
        return currentPlayerStateData;
    }

    public PlayerSpawnData GetPlayerSpawnData()
    {
        return new PlayerSpawnData(Client.ID, clientConnection.Name, transform.localPosition);
    }
}
