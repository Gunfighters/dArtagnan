using TMPro;
using UnityEngine;

namespace UI.ShowdownLoading
{
    public class ShowdownLoadingView : MonoBehaviour
    {
        public ShowdownLoadingFrame localPlayerFramePrefab;
        public ShowdownLoadingFrame enemyPlayerFramePrefab;
        public Transform frameGroup;
        public TextMeshProUGUI countdown;

        private void Start()
        {
            ShowdownLoadingPresenter.Initialize(new ShowdownLoadingModel(), this);
        }
    }
}