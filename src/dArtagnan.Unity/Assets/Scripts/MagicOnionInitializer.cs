using Cysharp.Net.Http;
using dArtagnan.Shared;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Unity;
using UnityEngine;

public class MagicOnionInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeStreamingHub()
    {
        GrpcChannelProviderHost.Initialize(new DefaultGrpcChannelProvider(() => new GrpcChannelOptions()
        {
            HttpHandler = new YetAnotherHttpHandler()
            {
                Http2Only = true,
            },
            DisposeHttpClient = true,
        }));
    }
}