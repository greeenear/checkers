using System.Collections.Generic;
using UnityEngine;

namespace controller {
    [System.Serializable]
    public struct BoardInfo {
        public Transform boardTransform;
        public Transform cellTransform;
        public Vector2Int boardSize;
    }

    public class Resources : MonoBehaviour {
        public BoardInfo board8x8;
        public BoardInfo board10x10;
        public GameObject highlightCell;
        public GameObject whiteChecker;
        public GameObject blackChecker;

        public readonly List<Vector2Int> directions = new List<Vector2Int> {
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1)
        };
    }
}