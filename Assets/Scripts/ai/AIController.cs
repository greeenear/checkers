using controller;
using UnityEngine;
using option;
using System.Collections.Generic;

namespace ai {
    public struct PathCell {
        public Vector2Int cellPos;
        public int weigth;

        public static PathCell Mk(Vector2Int cellPos, int weigth) {
            return new PathCell { cellPos = cellPos, weigth = weigth };
        }
    }

    public struct CheckerPaths {
        public Vector2Int pos;
        public List<PathCell> path;

        public static CheckerPaths Mk(Vector2Int pos, List<PathCell> path) {
            return new CheckerPaths { path = path, pos = pos };
        }
    }

    public static class AIController {
        public static void GetWeightsMatrix(
            Option<Checker>[,] board,
            ChColor color,
            ChKind kind,
            List<Vector2Int> mark
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }

            if (mark == null) {
                mark = new List<Vector2Int>();
            }

            var checkersPaths = new List<CheckerPaths>();

            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j++) {
                    var chOpt = board[i, j];
                    if (chOpt.IsNone()) continue;
                    var ch = chOpt.Peel();

                    if (ch.color != color) continue;

                    var chPos = new Vector2Int(i, j);
                    checkersPaths.Add(
                        CheckerPaths.Mk(
                            chPos,
                            GetChMoveWeigth(
                                board,
                                chPos,
                                kind,
                                mark,
                                0
                            )
                        )
                    );
                }
            }
        }

        public static List<PathCell> GetChMoveWeigth(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            List<Vector2Int> mark,
            int startWeight
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return new List<PathCell>();
            }

            var size = new Vector2Int(board.GetLength(1), board.GetLength(0));

            var cellOpt = board[pos.x, pos.y];
            var curCh = cellOpt.Peel();

            var xDir = 1;
            if (curCh.color == ChColor.White) {
                xDir = -1;
            }

            var path = new List<PathCell>();
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j ==0) {
                        continue;
                    }

                    var dir = new Vector2Int(i, j);
                    var chFound = false;
                    for (var next = pos + dir; IsOnBoard(size, next); next += dir) {
                        var nextOpt = board[next.x, next.y];
                        if (nextOpt.IsSome()) {
                            var nextColor = nextOpt.Peel().color;
                            var isSentenced = mark.Contains(next);
                            if (isSentenced || chFound || nextColor == curCh.color) break;
                            chFound = true;
                        } else {
                            var wrongMove = curCh.type == ChType.Checker && dir.x != xDir;
                            switch (kind) {
                                case ChKind.Pool:
                                case ChKind.Russian:
                                case ChKind.International:
                                    wrongMove = wrongMove && !chFound;
                                    break;
                            }

                            if (!wrongMove) {
                                if (chFound == true) {
                                    mark.Add(next - dir);
                                    board[next.x, next.y] = board[pos.x, pos.y];
                                    path.Add(PathCell.Mk(next, startWeight + 1));
                                    var start = startWeight + 1;
                                    path.AddRange(GetChMoveWeigth(board, next, kind, mark, start));
                                } else {
                                    path.Add(PathCell.Mk(next, 0));
                                }
                            }

                            if (curCh.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }

            return path;
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}
