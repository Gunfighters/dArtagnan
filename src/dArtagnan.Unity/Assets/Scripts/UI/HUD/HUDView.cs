using Assets.HeroEditor4D.Common.Scripts.Common;
using R3;
using UnityEngine;

namespace UI.HUD
{
    public class HUDView : MonoBehaviour
    {
        private HUDViewModel _viewModel;
        
        [Header("References")]
        [SerializeField] private HUDModel model;
        
        [Header("Canvases")]
        [SerializeField] private Canvas controls;
        [SerializeField] private Canvas spectating;
        [SerializeField] private Canvas waiting;
        [SerializeField] private Canvas playing;
        [SerializeField] private Canvas inRound;
        [SerializeField] private Canvas isHost;

        private void Awake()
        {
            _viewModel = new HUDViewModel(model);
            _viewModel.Controlling.Subscribe(controls.SetActive);
            _viewModel.Spectating.Subscribe(spectating.SetActive);
            _viewModel.Waiting.Subscribe(waiting.SetActive);
            _viewModel.Playing.Subscribe(playing.SetActive);
            _viewModel.InRound.Subscribe(inRound.SetActive);
            _viewModel.IsHost.Subscribe(isHost.SetActive);
        }
    }
}