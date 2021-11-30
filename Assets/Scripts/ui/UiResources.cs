using UnityEngine;
using UnityEngine.UI;

namespace ui {
    [System.Serializable]
    public struct CheckerImages {
        public RawImage whiteChecker;
        public RawImage blackChecker;
        public RawImage whiteKing;
        public RawImage blackKing;
        public GameObject emptyCell;
    }

    [System.Serializable]
    public struct BoardImages {
        public GameObject boardImage10x10;
        public GameObject boardImage8x8;
    }

    public class UiResources : MonoBehaviour {
        public LoadPanelRes loadPanel;
        public Button pageBut;
        public CheckerImages checkerImages;
        public BoardImages boardImages;
    }
}