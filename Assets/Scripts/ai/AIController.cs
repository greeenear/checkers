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
                            var secondPos = possibleGraphs[i].cells[j];
                            var firstPos = possibleGraphs[i].cells[startRow];

                            var dir = secondPos - firstPos;
                            var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                            for (var next = firstPos + nDir; next != secondPos; next += nDir) {
                                var curCh = Checkers.GetCell(board, next);
                                if (curCh != 0) {
                                    if ((curCh & Checkers.KING) > 0) {
                                        weight += 4;
                                    } else {
                                        weight += 2;
                                    }
                                    break;
                                }
                                if (next == secondPos) weight++;
                            }
                            if (firstPos + nDir == secondPos) weight++;
                            possibleGraphs[i].connect[startRow, j] -= startMark;
                            startRow = j;
                            j = -1;
                            possibleCells.Add(possibleGraphs[i].cells[startRow]);
                        }
                    }
                    checkerPaths.Add(ChPath.Mk(weight, possibleCells));
                    startMark *= 2;
                }
            }

            for (int i = 0; i < checkerPaths.Count; i++) {
                Debug.Log(checkerPaths[i].weight);
                foreach (var path in checkerPaths[i].path)
                {
                    Debug.Log(path);
                }
                Debug.Log("______");
            }
            return checkerPaths;
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

        public static ChPath GetPerfetcPath(List<ChPath> paths) {
            var maxWeight = 0;
            var pathIndex = 0;
            for (int i = 0; i < paths.Count; i++) {
                if (paths[i].weight > maxWeight) {
                    maxWeight = paths[i].weight;
                    pathIndex = i;
                }
            }

            return paths[pathIndex];
        }
    }
}
