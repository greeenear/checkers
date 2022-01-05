using System.Collections.Generic;
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
        public Option<Checker>[,] board;
        public Vector2Int pos;
    }

    public struct PossibleGraph {
        public int[,] connect;
        public Vector2Int[] cells;
        public int[] marks;
    }

    public struct MatrixInfo {
        public Vector2Int index;
        public bool needAttack;
        public int markerType;
    }

    public struct Checker {
        public ChType type;
        public ChColor color;
    }

    public static class Checkers {
        public static int GetPossiblePaths(ChLocation loc, ChKind kind, PossibleGraph graph) {
            if (loc.board == null || graph.cells == null || graph.connect == null) {
                Debug.LogError("BadParams");
                return -1;
            }

            var boardSize = new Vector2Int(loc.board.GetLength(1), loc.board.GetLength(1));
            if (!IsOnBoard(boardSize, loc.pos)) {
                Debug.LogError("BadPos");
                return -1;
            }

            if (graph.cells.GetLength(0) < 1) {
                Debug.LogError("BadBufferSize");
                return -1;
            }

            graph.cells[0] = loc.pos;
            graph.connect[0, 0] = 0;

            var chOpt = loc.board[loc.pos.x, loc.pos.y];
            if (chOpt.IsNone()) return -1;
            var ch = chOpt.Peel();
            var xDir = 1;
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            var needAttack = false;
            int marks = 1;
            int cellCount = 1;

            for (int i = -1; i <= 1 && !needAttack; i += 2) {
                for (int j = -1; j <= 1 && (ch.type != ChType.Checker || i == xDir); j += 2) {

                    var dir = new Vector2Int(i, j);

                    var length = GetMaxEmpty(loc, dir);
                    if (length == -1) {
                        Debug.LogError("BadLength");
                        return -1;
                    }

                    var max = length;
                    if (ch.type == ChType.Checker || kind == ChKind.English) max = 0;
                    length = Mathf.Clamp(length, 0, max);

                    var lastPos = GetCellByIndex(loc.board, loc.pos + dir * (length + 1));
                    if (lastPos == new Vector2Int(-1, -1)) lastPos = loc.pos + dir * length;

                    var lastPosOpt = loc.board[lastPos.x, lastPos.y];
                    if (lastPosOpt.IsSome()) {
                        var isOpponent = lastPosOpt.Peel().color != ch.color;
                        var nextPos = GetCellByIndex(loc.board, loc.pos + dir * (length + 2));
                        if (nextPos != new Vector2Int(-1, -1)) {
                            if (isOpponent && loc.board[nextPos.x, nextPos.y].IsNone()) {
                                needAttack = true;
                                break;
                            }
                        };

                        length--;
                    }

                    for (int k = 0; k <= length; k++) {
                        if (graph.cells.GetLength(0) < cellCount) {
                            Debug.LogError("BadBufferSize");
                            return -1;
                        }
                        graph.cells[cellCount] = loc.pos + dir * (k + 1);
                        graph.connect[0, cellCount] = marks;
                        graph.marks[cellCount] = graph.marks[cellCount] + marks;
                        cellCount++;
                    }
                    marks = marks << 1;
                }
            }

            if (needAttack) {
                cellCount = 1;
                cellCount = GetAttackPaths(loc, kind, ch, graph, 1, marks, 1);
            }
            return cellCount;
        }

        private static int GetAttackPaths(
            ChLocation loc,
            ChKind kind,
            Checker ch,
            PossibleGraph graph,
            int cellCount,
            int marks,
            int lastColum
        ) {
            if (loc.board == null || graph.cells == null || graph.connect == null) {
                Debug.LogError("BadParams");
                return -1;
            }

            int startRow = cellCount;
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j == 0) continue;
                    var dir = new Vector2Int(i, j);

                    int curColum = cellCount;
                    var length = GetMaxEmpty(loc, dir);
                    if (length == -1) {
                        Debug.LogError("BadLength");
                        return -1;
                    }
                    var max = length;
                    if (ch.type == ChType.Checker || kind == ChKind.English) max = 0;
                    max = Mathf.Clamp(length, 0, max);

                    if (max != length) continue;
                    var lastPos = GetCellByIndex(loc.board, loc.pos + dir * (length + 1));
                    if (lastPos == new Vector2Int(-1, -1)) continue;

                    var chOpt = loc.board[lastPos.x, lastPos.y];
                    if (chOpt.IsSome() && chOpt.Peel().color == ch.color) continue;

                    var nextPos = GetCellByIndex(loc.board, loc.pos + dir * (length + 2));
                    if (nextPos == new Vector2Int(-1, -1)) continue;

                    var badDir = false;
                    for (int k = 0; k < cellCount; k++) {
                        if (graph.cells[k] == nextPos && (graph.marks[k] & marks) == marks) {
                            badDir = true;
                        }
                    }

                    if (loc.board[nextPos.x, nextPos.y].IsSome() || badDir) continue;

                    for (int k = 0; k < cellCount; k++) {
                        if (graph.cells[k] == nextPos) {
                            curColum = k;
                            startRow = lastColum;
                        }
                    }

                    for (int k = 0; k <= max; k++) {
                        if (graph.cells.GetLength(0) < curColum) {
                            Debug.LogError("BadBufferSize");
                            return -1;
                        }
                        graph.cells[curColum] = nextPos + dir * k;
                        graph.marks[curColum] += marks;
                        graph.connect[startRow - 1, curColum] = 1;
                        cellCount++;
                        for (int l = 0; l <= cellCount; l++) {
                            graph.connect[l, cellCount] = 0;
                            graph.connect[cellCount, l] = 0;
                        }
                        curColum++;
                    }

                    var oldPos = loc.pos;
                    loc.pos = nextPos;
                    cellCount = GetAttackPaths(loc, kind, ch, graph, cellCount, marks, curColum);
                    if (cellCount == -1) return -1;
                    loc.pos = oldPos;

                    if (nextPos - 2 * dir == graph.cells[0]) marks = marks << 1;
                }
            }

            return cellCount;
        }

        private static int GetMaxEmpty(ChLocation loc, Vector2Int dir) {
            if (loc.board == null) {
                Debug.LogError("BoardIsNull");
                return -1;
            }

            var size = new Vector2Int(loc.board.GetLength(1), loc.board.GetLength(0));
            if (!IsOnBoard(size, loc.pos)) {
                Debug.LogError("BadPos");
                return -1;
            }

            int len = 0;
            var pos = loc.pos + dir;
            for (var p = pos; IsOnBoard(size, p) && loc.board[p.x, p.y].IsNone(); p += dir, ++len);

            return len;
        }

        private static Vector2Int GetCellByIndex(Option<Checker>[,] board, Vector2Int index) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return new Vector2Int(-1, -1);
            }

            var boardSize = new Vector2Int(board.GetLength(0), board.GetLength(1));
            var cell = new Vector2Int(-1, -1);

            if (IsOnBoard(boardSize, index)) cell = index;

            return cell;
        }

        public static void ShowMatrix(PossibleGraph graph) {
            if (graph.cells == null || graph.connect == null) {
                Debug.LogError("BadParams");
                return;
            }

            var nodes = "";
            foreach (var a in graph.cells) {
                nodes += a.ToString() + "   ";
            }
            Debug.Log(nodes);

            var marks = "";
            foreach (var a in graph.marks) {
                marks += a.ToString() + "   ";
            }
            Debug.Log(marks);

            var matrix = "";
            for (int i = 0; i < graph.connect.GetLength(1); i++) {
                matrix = "";
                for (int j = 0; j < graph.connect.GetLength(0); j++) {
                    matrix += "        " + graph.connect[i,j].ToString();
                }
                Debug.Log(matrix);
            }
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}