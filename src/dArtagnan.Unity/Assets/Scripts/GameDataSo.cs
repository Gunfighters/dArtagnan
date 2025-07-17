using dArtagnan.Shared;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDataSo", menuName = "d'Artagnan/Game Data", order = 0)]
public class GameDataSo : ScriptableObject
{
    [Header("Player Info")]
    public int LocalPlayerId;
    public int HostId;

    [Header("Game State")]
    public GameState CurrentState;
}