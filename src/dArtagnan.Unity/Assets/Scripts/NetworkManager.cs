using dArtagnan.Shared;
using MagicOnion;
using MagicOnion.Client;
using UnityEngine;

/// <summary>
///     서버에서 정보를 받아 GameManager의 함수를 호출합니다.
/// </summary>
public class NetworkManager
{
    private IGameHub client;
    public GameManager GameManager;
    private GameHubReceiver receiver;

    private async void Start()
    {
        var channel = GrpcChannelx.ForAddress("http://localhost:5080");
        var serviceClient = MagicOnionClient.Create<IGameService>(channel);
        Debug.Log("Start");
        var result = await serviceClient.SumAsync(1, 2);
        Debug.Log(result);
        Debug.Log("Start");
        receiver = new GameHubReceiver();
        Debug.Log("Start");
        client = await StreamingHubClient.ConnectAsync<IGameHub, IGameHubReceiver>(channel, receiver);
        Debug.Log("Start");
        await client.JoinAsync(1);
        Debug.Log("Start");
    }
}