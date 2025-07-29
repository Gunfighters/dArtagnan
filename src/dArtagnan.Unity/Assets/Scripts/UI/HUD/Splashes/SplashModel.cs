using R3;
using UnityEngine;

namespace UI.HUD.Splashes
{
    [CreateAssetMenu(fileName = "SplashModel", menuName = "d'Artagnan/Splashes Model", order = 0)]
    public class SplashModel : ScriptableObject
    {
        public SerializableReactiveProperty<bool> gameStart;
        public SerializableReactiveProperty<bool> roundStart;
        public SerializableReactiveProperty<bool> roundOver;
        public SerializableReactiveProperty<bool> gameOver;
        public float splashDuration;
    }
}