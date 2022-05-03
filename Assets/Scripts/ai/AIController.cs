using checkers;
using UnityEngine;
using option;
using System.Collections.Generic;

namespace ai {
    public struct ChPath {
        public int weight;
        public List<Vector2> path;

        public static ChPath Mk(int weight, List<Vector2> path) {
            return new ChPath { path = path, weight = weight };
        }
    }

    public struct CheckerPaths {
    }

    public static class AIController {
        public static List<ChPath> GetAIPaths(
            PossibleGraph[] possibleGraphs,
            int[] buffSize,
            int whoseMove,
            int [,] board
        ) {
            var checkerPaths = new List<ChPath>();
            for (int i = 0; i < possibleGraphs.Length; i++) {
                var ch = Checkers.GetCell(board, possibleGraphs[i].cells[0]);
                if (ch == 0) continue;

                var color = ch & Checkers.WHITE;
                if (color != whoseMove) continue;

                var startMark = 1;
                for (int k = 0; k < GetMarkCount(possibleGraphs[i].marks); k++) {
                    var startRow = 0;
                    var weight = 0;
                    var possibleCells = new List<Vector2>();
                    possibleCells.Add(possibleGraphs[i].cells[startRow]);
                    //Debug.Log(possibleCells[possibleCells.Count - 1]);

                    for (int j = 0; j < buffSize[i]; j++) {
                        var startCell = possibleGraphs[i].cells[startRow];
                        if ((possibleGraphs[i].connect[startRow, j] & startMark) > 0) {
                            possibleGraphs[i].connect[startRow, j] -= startMark;
                            startRow = j;
                            j = -1;
                            possibleCells.Add(possibleGraphs[i].cells[startRow]);
                        }
                    }
                    checkerPaths.Add(ChPath.Mk(0, possibleCells));
                    startMark *= 2;
                }
            }

            for (int i = 0; i < checkerPaths.Count; i++) {
                foreach (var path in checkerPaths[i].path)
                {
                    Debug.Log(path);
                }
                Debug.Log("______");
            }
            return null;
        }

        public static int GetMarkCount(int[] marks) {
            var curMark = 0;
            for (int i = 0; i < marks.Length; i++) {
                if (marks[i] > curMark) curMark = marks[i];
            }

            int result = 0;
            while (curMark > 0) {
                curMark = curMark >> 1;
                result++;
            }

            return result;
        }
    }
}
