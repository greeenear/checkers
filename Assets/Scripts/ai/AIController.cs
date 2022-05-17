using checkers;
using UnityEngine;
using option;
using System.Collections.Generic;

namespace ai {
    public struct ChPath {
        public int weight;
        public List<Vector2Int> path;

        public static ChPath Mk(int weight, List<Vector2Int> path) {
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
                    var possibleCells = new List<Vector2Int>();
                    possibleCells.Add(possibleGraphs[i].cells[startRow]);

                    for (int j = 0; j < buffSize[i]; j++) {
                        var startCell = possibleGraphs[i].cells[startRow];
                        if ((possibleGraphs[i].connect[startRow, j] & startMark) > 0) {
                            var secondPos = possibleGraphs[i].cells[j];
                            var firstPos = possibleGraphs[i].cells[startRow];

                            var dir = secondPos - firstPos;
                            var nDir = new Vector2Int(
                                dir.x / Mathf.Abs(dir.x),
                                dir.y / Mathf.Abs(dir.y)
                            );

                            for (var next = firstPos + nDir; next != secondPos; next += nDir) {
                                var curCh = Checkers.GetCell(board, next);
                                if (curCh != 0) {
                                    if ((curCh & Checkers.KING) > 0) {
                                        weight += 20;
                                    } else {
                                        weight += 10;
                                    }
                                    break;
                                }
                                if (next == secondPos) weight++;
                                if (secondPos.x > 4) weight += 2;
                                if (secondPos.x == 7) weight += 2;
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

        public static ChPath GetBestPath(List<ChPath> paths, int[,] board) {
            CheckNextMove(paths, board);
            var maxWeight = 0;
            var pathIndex = 0;
            for (int i = 0; i < paths.Count; i++) {
                if (paths[i].weight > maxWeight) {
                    if (paths[i].path.Count < 2) continue;
                    maxWeight = paths[i].weight;
                    pathIndex = i;
                }
            }

            var equalPaths = new List<ChPath>();
            foreach (var path in paths)
            {
                if (path.weight == maxWeight) equalPaths.Add(path);
            }

            return equalPaths[Random.Range(0, equalPaths.Count)];
        }

        private static void CheckNextMove(List<ChPath> paths, int [,] board) {
            for (int i = 0; i < paths.Count; i++) {
                var startPos = paths[i].path[0];
                var lastPos = paths[i].path[paths[i].path.Count - 1];
                var color = board[startPos.x, startPos.y] & Checkers.WHITE;
                var weight = paths[i].weight;
                var firsCell = Checkers.GetCell(
                    board,
                    new Vector2Int(lastPos.x + 1, lastPos.y - 1)
                );
                var secondCell = Checkers.GetCell(
                    board,
                    new Vector2Int(lastPos.x + 1, lastPos.y + 1)
                );

                if ((firsCell & Checkers.WHITE) == color &&
                    (secondCell & Checkers.WHITE) == color || firsCell <= 0 && secondCell <= 0) {
                    weight += 4;
                }
                paths[i] = ChPath.Mk(weight, paths[i].path);
            }
        }
    }
}
