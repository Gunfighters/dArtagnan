using dArtagnan.Shared;
using UnityEngine;

public class GameHubReceiver : IGameHubReceiver
{
    public void OnJoin(int accuracy)
    {
        Debug.Log("OnJoin");
    }

    public void OnMove(int index, Vector2 position)
    {
        Debug.Log("OnMove");
    }

    public void OnShoot(int target)
    {
        Debug.Log("OnShoot");
    }

    public void OnDeath(int index)
    {
        Debug.Log("OnDeath");
    }
}