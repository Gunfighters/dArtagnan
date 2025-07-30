using System.Collections.Generic;
using System.Linq;
using Game;
using R3;
using UnityEngine;

namespace UI.CanvasManager
{
    public class CanvasManagerView : MonoBehaviour
    {
        public List<CanvasMetaData> canvasList;

        private void Awake()
        {
            CanvasManagerPresenter.Initialize(this);
        }
    }
}