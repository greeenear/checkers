using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace controller {
    public class Resources : MonoBehaviour {
        public Transform cellTransform;
        public Vector2Int boardSize = new Vector2Int(8, 8);
        public GameObject storageHighlightCells;
        public GameObject highlightCell;
        public GameObject whiteChecker;
        public GameObject blackChecker;
        public Transform boardTransform;
        public GameObject gameMenu;
        public readonly List<Vector2Int> directions = new List<Vector2Int> {
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1)
        };
    }
}