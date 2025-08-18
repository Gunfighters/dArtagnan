using R3;
using UnityEngine;

namespace Game
{
    public class GameView : MonoBehaviour
    {
        private void Awake()
        {
            var model = new GameModel().AddTo(this);
            GamePresenter.Initialize(this, model);
            GameService.SetInstance(model);
        }

        private void OnDestroy()
        {
            GameService.ClearInstance();
        }
    }
}