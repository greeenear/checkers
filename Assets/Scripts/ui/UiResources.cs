using UnityEngine;
using UnityEngine.UI;

namespace ui {
    [System.Serializable]
    public struct CheckerImages {
        public RawImage checkerImg;
        public RawImage kingImg;
        public BoardImageRes emptyCell;
    }

    [System.Serializable]
    public struct BoardImages {
        public GameObject boardImage10x10;
        public GameObject boardImage8x8;
    }

    public class UiResources : MonoBehaviour {
        public LoadPanelRes loadPanel;
        public PageButRes pageBut;
        public CheckerImages checkerImages;
        public BoardImages boardImages;
        public RawImage spaceBetweenButtons;
    }
}