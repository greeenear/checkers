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

    public struct CellInfo {
        public Vector2Int pos;
        public bool isAttack;
        
        public static CellInfo Mk(Vector2Int pos, bool isAttack) {
            return new CellInfo { pos = pos, isAttack = isAttack };
        }
    }

    public static class Checkers {
        public static Option<CellInfo>[,] GetAdjacencyMatrtix(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Option<CellInfo>[,] matrix
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return matrix;
            }

            var chOpt = board[pos.x, pos.y];
            if (chOpt.IsNone()) return matrix;
            var ch = chOpt.Peel();
            FillAdjacencyMatrtix(board, pos, kind, ch, Vector2Int.zero, matrix, Vector2Int.zero);

            return matrix;
        }

        public static int GetNextCellsIndex(Option<CellInfo>[,] matrix, Vector2Int targetPos) {
            for (int i = 0; i < matrix.GetLength(1); i++) {
                for (int j = 0; j < matrix.GetLength(0); j++) {
                    var posOpt = matrix[i, j];
                    if (posOpt.IsNone()) continue;
                    var pos = posOpt.Peel();

                    if (pos.pos == targetPos) {
                        return j;
                    }
                }
            }
            return 0;
        }

        public static void Move(Option<Checker>[,] board, Vector2Int from, Vector2Int to) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }

            var chOpt = board[from.x, from.y];
            if (chOpt.IsNone()) return;
            var ch = chOpt.Peel();

            board[to.x, to.y] = Option<Checker>.Some(ch);
            board[from.x, from.y] = Option<Checker>.None();
        }

        private static void FillAdjacencyMatrtix(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Checker ch,
            Vector2Int index,
            Option<CellInfo>[,] matrix,
            Vector2Int badDir
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }

            var needAttack = false;
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
                                    needAttack = true;
                                    var nextCell = CellInfo.Mk(next, true);
                                    matrix[index.x, index.y] = Option<CellInfo>.Some(nextCell);
                                    var ind = new Vector2Int(index.y, index.y);
                                    FillAdjacencyMatrtix(board, next, kind, ch, ind, matrix, dir);
                                } else {
                                    var nextCell = CellInfo.Mk(next, false);
                                    matrix[index.x, index.y] = Option<CellInfo>.Some(nextCell);
                                }
                            }
                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }

            if (index.x == 0 && needAttack) {
                for (int i = 0; i < matrix.GetLength(1); i++) {
                    for (int j = 0; j < matrix.GetLength(0); j++) {
                        if (matrix[i,j].IsSome() && !matrix[i,j].Peel().isAttack) {
                            matrix[i,j] = Option<CellInfo>.None();
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