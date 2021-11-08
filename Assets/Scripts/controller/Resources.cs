using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace controller {
    public class Resources : MonoBehaviour {
        public GameObject storageHighlightCells;
        public GameObject highlightCell;
        public GameObject whiteChecker;
        public GameObject blackChecker;

        public Transform boardTransform;
        public Transform cellTransform;
        public Vector2Int boardSize = new Vector2Int(8, 8);

        public GameObject gameMenu;

        public Transform boardTransform10x10;
        public Transform cellTransform10x10;

        public readonly List<Vector2Int> directions = new List<Vector2Int> {
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1)
        };

        public void InitializeBoard(string chKind) {
            if (chKind == "International") {
                boardTransform = boardTransform10x10;
                cellTransform = cellTransform10x10;
                boardSize = new Vector2Int(10,10);
            }
        }
    }
}