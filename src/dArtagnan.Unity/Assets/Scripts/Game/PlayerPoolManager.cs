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
        public IObjectPool<PlayerView> Pool;

        private void Awake()
        {
            Instance = this;
            Pool = new ObjectPool<PlayerView>(
                CreateGameObjectBase,
                ActionOnGet,
                ActionOnRelease,
                ActionOnDestroy,
                maxSize: config.poolSize
            );
        }

        private PlayerView CreateGameObjectBase()
        {
            var gameObjectBase = GameObject.Instantiate(config.playerPrefab).GetComponent<PlayerView>();
            gameObjectBase.transform.SetParent(transform);
            return gameObjectBase;
        }

        private static void ActionOnGet(PlayerView playerView)
        {
            playerView.gameObject.SetActive(true);
            playerView.transform.localPosition = Vector3.zero;
        }

        private static void ActionOnRelease(PlayerView playerView)
        {
            playerView.gameObject.SetActive(false);
        }

        private static void ActionOnDestroy(PlayerView playerView)
        {
            Destroy(playerView.gameObject);
        }
    }
}