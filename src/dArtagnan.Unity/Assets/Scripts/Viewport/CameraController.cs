using System.Linq;
using dArtagnan.Shared;
using Game;
using Game.Player.Data;
using R3;
using UnityEngine;

namespace Viewport
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        private PlayerModel _targetModel;
        public SpriteRenderer groundRenderer;
        public Vector3 offset = new(0, 0, -10);
        public Camera cam;
        [SerializeField] public float cameraMoveSpeed;
        public float height;
        public float width;
        public Vector2 mapSize;
        public Vector2 center;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            mapSize = groundRenderer.bounds.size / 2;
            height = cam.orthographicSize;
            width = height * cam.aspect;
        }

        private void Start()
        {
            GameService.CameraTarget.Skip(1).Subscribe(target =>
            {
                _targetModel = target;
                _targetModel.Alive.Subscribe(newAlive =>
                {
                    if (!newAlive)
                        GameService.CameraTarget.Value =
                            GameService.PlayerModels.FirstOrDefault(pair => pair.Value.Alive.Value).Value;
                });
            });
        }

        private void Update()
        {
            if (_targetModel is not null)
            {
                LimitCameraArea();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, mapSize * 2);
        }

        private void LimitCameraArea()
        {
            transform.position = Vector3.Lerp(transform.position,
                _targetModel.Position.CurrentValue + (Vector2) offset,
                Time.deltaTime * cameraMoveSpeed);
            var lx = mapSize.x - width;
            var clampX = Mathf.Clamp(transform.position.x, -lx + center.x, lx + center.x);

            var ly = mapSize.y - height;
            var clampY = Mathf.Clamp(transform.position.y, -ly + center.y, ly + center.y);

            transform.position = new Vector3(clampX, clampY, -10f);
        }
    }
}