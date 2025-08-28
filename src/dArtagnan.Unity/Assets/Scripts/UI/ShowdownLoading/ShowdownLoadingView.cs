using UnityEngine;

namespace UI.ShowdownLoading
{
    public class ShowdownLoadingView : MonoBehaviour
    {
        private void Awake()
        {
            ShowdownLoadingPresenter.Initialize(this);
        }
    }
}