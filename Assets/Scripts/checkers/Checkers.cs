using System.Runtime.InteropServices;
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

    public struct ChLocation {

    }

    public struct Buffer {
        public int [,] matrix;
        public Vector2Int [] nodes;
    }

    public struct Checker {
        public ChType type;
        public ChColor color;
    }

    public static class Checkers {
        public static int[,] GetPossiblePaths(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Buffer buf
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return buf.matrix;
            }

            var chOpt = board[pos.x, pos.y];
            if (chOpt.IsNone()) return buf.matrix;
            var ch = chOpt.Peel();
            var nodes = new Vector2Int[15];
            int refi = 1;
            GetPossibleSubPath(board, pos, kind, ch, Vector2Int.zero, buf, false, ref refi, 1);

            var str = "";
            foreach (var a in nodes) {
                str += a.ToString() + "   ";
            }
            Debug.Log(str);

            return buf.matrix;
        }

        // public static int GetNextCellsIndex(int[,] matrix, Vector2Int targetPos) {
        //     for (int i = 0; i < matrix.GetLength(1); i++) {
        //         for (int j = 0; j < matrix.GetLength(0); j++) {
        //             var posOpt = matrix[i, j];
        //             if (posOpt.IsNone()) continue;
        //             var pos = posOpt.Peel();

        //             if (pos == targetPos) {
        //                 return j;
        //             }
        //         }
        //     }
        //     return 0;
        // }

        public static void ShowMatrix(int[,] matrix) {
            for (int i = 0; i < matrix.GetLength(1); i++) {
                string a = "";
                for (int j = 0; j < matrix.GetLength(0); j++) {
                    a += "        " + matrix[i,j].ToString();
                }
                Debug.Log(a);
            }
        }

        private static int GetPossibleSubPath(//убрать индекс
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Checker ch,
            Vector2Int index,
            Buffer buf,
            bool needAttack,
            ref int n,
            int marked
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return 0;
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
                            bool isBadDir = false;
                            for (int k = 0; k < n; k++) {
                                if (buf.nodes[k] == next + dir) {
                                    for (int l = 0; l < n; l++) {
                                        if (buf.matrix[k, l] == marked) isBadDir = true;
                                    }
                                }
                            }

                            if (isBadDir || chFound || nextColor == ch.color) {
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
                                if (chFound == true) {
                                    if (wasUsualMove) {
                                        for (int k = 0; k < n; k++) {
                                            buf.matrix[0,k] = 0;
                                        }
                                        wasUsualMove = false;
                                    }
                                    needAttack = true;
                                    index.y = n;
                                    n++;
                                    for (int k = 0; k < n; k++) {
                                        if (buf.nodes[k] == next) {
                                            index.y = k;
                                            n--;
                                            break;
                                        }
                                    }
                                    buf.matrix[index.x, index.y] = marked;
                                    buf.nodes[index.y] = next;
                                    var ind = new Vector2Int(index.y, 0);
                                    GetPossibleSubPath(board, next, kind, ch, ind, buf, needAttack, ref n, marked);
                                } else if (!needAttack) {
                                    wasUsualMove = true;
                                    buf.matrix[index.x, index.y] = marked;
                                }
                            }
                            if (index.x == 0) {
                                marked++;
                            }
                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }
            return n;
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}