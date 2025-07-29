using System;
using Game;
using UnityEngine;

namespace UI.CanvasManager
{
    [Serializable]
    public struct CanvasMetaData
    {
        public GameScreen screen;
        public Canvas canvas;
    }
}