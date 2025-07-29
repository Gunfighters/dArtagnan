using System.Collections.Generic;
using Game;
using R3;
using UnityEngine;

namespace UI.CanvasManager
{
    public class CanvasManagerView : MonoBehaviour
    {
        private CanvasManagerViewModel _viewModel;
        [Header("References")]
        [SerializeField] private CanvasManagerModel model;

        [Header("UI")]
        [SerializeField] private List<CanvasMetaData> canvasList;

        private void Awake()
        {
            _viewModel = new CanvasManagerViewModel(model);
            _viewModel.Screen.Subscribe(Toggle);
        }

        private void Toggle(GameScreen screen)
        {
            canvasList.ForEach(c => c.canvas.gameObject.SetActive(c.screen == screen));
        }
    }
}