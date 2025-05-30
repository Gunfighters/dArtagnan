using System.Collections.Generic;
using System.Net.Sockets;
using dArtagnan.Shared;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public List<GameObject> Players;
    async void Start()
    {
        Debug.Log("Start");
        var channel = GrpcChannelx.ForAddress("http://localhost:5080");
        var serviceClient = MagicOnionClient.Create<IGameService>(channel);
        var result = await serviceClient.SumAsync(1, 2);
        Debug.Log(result);
        Debug.Log("Start");
        var receiver = new GameHubReceiver();
        Debug.Log("Start");
        var client = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(channel, receiver);
        Debug.Log("Start");
        await client.JoinAsync(1);
        Debug.Log("Start");
    }

    void Update()
    {
        
    }
}