using UnityEngine;
using option;

namespace checkers {
    public enum ChKind {
        Russian,
        English,
        Pool,
        International
    }

    public enum ChType {
        Checker,
        King
    }

    public enum ChColor {
        White,
        Black,
        Count
    }

    public struct Checker {
        public ChType type;
        public ChColor color;
    }

    public static class Checkers {
        public static Option<Vector2Int>[,] GetPossiblePaths(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Option<Vector2Int>[,] matrix
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return matrix;
            }

            var chOpt = board[pos.x, pos.y];
            if (chOpt.IsNone()) return matrix;
            var ch = chOpt.Peel();
            var nodes = new Vector2Int[15];
            GetPossibleSubPath(board, pos, kind, ch, Vector2Int.zero, matrix, nodes, Vector2Int.zero, false);
            Debug.Log(pos);
            foreach (var a in nodes) {
                Debug.Log(a);
            }

            return matrix;
        }

        public static int GetNextCellsIndex(Option<Vector2Int>[,] matrix, Vector2Int targetPos) {
            for (int i = 0; i < matrix.GetLength(1); i++) {
                for (int j = 0; j < matrix.GetLength(0); j++) {
                    var posOpt = matrix[i, j];
                    if (posOpt.IsNone()) continue;
                    var pos = posOpt.Peel();

                    if (pos == targetPos) {
                        return j;
                    }
                }
            }
            return 0;
        }

        private static void GetPossibleSubPath(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Checker ch,
            Vector2Int index,
            Option<Vector2Int>[,] matrix,
            Vector2Int [] nodes,
            Vector2Int badDir,
            bool needAttack
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }
            bool wasUsualMove = false;

            var xDir = 1;
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j == 0) continue;

                    var dir = new Vector2Int(i, j);
                    var chFound = false;
                    var size = new Vector2Int(board.GetLength(1), board.GetLength(0));
                    for (var next = pos + dir; IsOnBoard(size, next); next += dir) {
                        var nextOpt = board[next.x, next.y];
                        if (nextOpt.IsSome()) {
                            var nextColor = nextOpt.Peel().color;

                            if (dir == -badDir || chFound || nextColor == ch.color) {
                                break;
                            }
                            chFound = true;
                        } else {
                            var wrongMove = ch.type == ChType.Checker && dir.x != xDir;
                            switch (kind) {
                                case ChKind.Pool:
                                case ChKind.Russian:
                                case ChKind.International:
                                    wrongMove = wrongMove && !chFound;
                                    break;
                            }

                            if (!wrongMove) {
                                index.y++;
                                if (chFound == true) {
                                    if (wasUsualMove) {
                                        for (int k = 0; k < matrix.GetLength(0); k++) {
                                            matrix[0,k] = Option<Vector2Int>.None();
                                        }
                                    }
                                    needAttack = true;
                                    matrix[index.x, index.y] = Option<Vector2Int>.Some(next);
                                    nodes[index.y] = next;
                                    var ind = new Vector2Int(index.y, index.y);
                                    GetPossibleSubPath(board, next, kind, ch, ind, matrix, nodes, dir, needAttack);
                                } else if (!needAttack) {
                                    wasUsualMove = true;
                                    matrix[index.x, index.y] = Option<Vector2Int>.Some(next);
                                }
                            }
                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }
            return;
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }

    }
}