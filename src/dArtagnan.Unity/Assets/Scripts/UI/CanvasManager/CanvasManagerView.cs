using System.Collections.Generic;
using UnityEngine;

namespace UI.CanvasManager
{
    public class CanvasManagerView : MonoBehaviour
    {
        public List<CanvasMetaData> canvasList;

        private void Start()
        {
            CanvasManagerPresenter.Initialize(this, new CanvasManagerModel());
        }
    }
}