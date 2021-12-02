using UnityEngine;
using UnityEngine.UI;

namespace ui {
    [System.Serializable]
    public struct CheckerImages {
        public RawImage checkerImg;
        public RawImage kingImg;
        public RawImage emptyCell;
    }

    [System.Serializable]
    public struct BoardImages {
        public GameObject boardImage10x10;
        public BoardImageRes boardImage8x8;
    }

    public class UiResources : MonoBehaviour {
        public LoadPanelRes loadPanel;
        public Button pageBut;
        public CheckerImages checkerImages;
        public BoardImages boardImages;
    }
}