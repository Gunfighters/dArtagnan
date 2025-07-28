using Game.Player;
using Game.Player.Components;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class PlayerPoolManager : MonoBehaviour
    {
        public static PlayerPoolManager Instance { get; private set; }
        public PlayerPoolConfig config;
        public IObjectPool<PlayerCore> Pool;

        private void Awake()
        {
            Instance = this;
            Pool = new ObjectPool<PlayerCore>(
                CreateGameObjectBase,
                ActionOnGet,
                ActionOnRelease,
                ActionOnDestroy,
                maxSize: config.poolSize
            );
        }

        private PlayerCore CreateGameObjectBase()
        {
            var gameObjectBase = GameObject.Instantiate(config.playerPrefab).GetComponent<PlayerCore>();
            gameObjectBase.transform.SetParent(transform);
            return gameObjectBase;
        }

        private static void ActionOnGet(PlayerCore playerCore)
        {
            playerCore.gameObject.SetActive(true);
            playerCore.transform.localPosition = Vector3.zero;
        }

        private static void ActionOnRelease(PlayerCore playerCore)
        {
            playerCore.gameObject.SetActive(false);
        }

        private static void ActionOnDestroy(PlayerCore playerCore)
        {
            Destroy(playerCore.gameObject);
        }
    }
}