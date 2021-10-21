using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace controller {
    public class Resources : MonoBehaviour {
        public Transform cellSize;
        public Vector2Int boardSize = new Vector2Int(8, 8);
        public GameObject storageHighlightCells;
        public GameObject highlightCell;
        public GameObject whiteChecker;
        public GameObject blackChecker;
        public Transform boardPos;
    }
}