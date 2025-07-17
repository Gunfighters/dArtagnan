using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "PlayerPoolConfig", menuName = "d'Artagnan/PlayerPoolConfig", order = 0)]
    public class PlayerPoolConfig : ScriptableObject
    {
        public GameObject playerPrefab;
        public int poolSize;
    }
}