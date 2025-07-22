using System.Linq;
using UnityEngine;

public static class ChannelListenerInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeAll()
    {
        foreach (var l in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IChannelListener>())
        {
            l.Initialize();
        }
    }
}