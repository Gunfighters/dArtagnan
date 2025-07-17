using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class PlayerPoolManager : MonoBehaviour
    {
        public PlayerPoolConfig config;
        public IObjectPool<Player> Pool;

        private void Awake()
        {
            Pool = new ObjectPool<Player>(
                CreateGameObjectBase,
                ActionOnGet,
                ActionOnRelease,
                ActionOnDestroy,
                maxSize: config.poolSize
            );
        }

        private Player CreateGameObjectBase()
        {
            var gameObjectBase = Instantiate(config.playerPrefab).GetComponent<Player>();
            gameObjectBase.transform.SetParent(transform);
            return gameObjectBase;
        }

        private static void ActionOnGet(Player player)
        {
            player.gameObject.SetActive(true);
            player.transform.localPosition = Vector3.zero;
        }

        private static void ActionOnRelease(Player player)
        {
            player.gameObject.SetActive(false);
        }

        private static void ActionOnDestroy(Player player)
        {
            Destroy(player.gameObject);
        }
    }
}