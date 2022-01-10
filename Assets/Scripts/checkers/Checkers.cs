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
            var marks = 1;
            var cellCount = 1;
            loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.None();

            for (int i = -1; i <= 1 && !needAttack; i += 2) {
                for (int j = -1; j <= 1 && (ch.type != ChType.Checker || i == xDir); j += 2) {

                    var dir = new Vector2Int(i, j);

                    var length = GetMaxEmpty(loc, dir);
                    if (length == -1) {
                        Debug.LogError("BadLength");
                        return -1;
                    }

                    var max = length;
                    if (ch.type == ChType.Checker || kind == ChKind.English) max = 1;
                    length = Mathf.Clamp(length, 0, max);

                    if (ch.type != ChType.Checker && kind != ChKind.English || length != 1) {
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
                        }
                    }

                    for (int k = 0; k < length; k++) {
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
                cellCount = GetAttackPaths(loc, kind, ch, graph, 1, 1, 1, 0);
            }
            loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.Some(ch);
            return cellCount;
        }

        private static int GetAttackPaths(
            ChLocation loc,
            ChKind kind,
            Checker ch,
            PossibleGraph graph,
            int cCount,
            int marks,
            int lastColum,
            int curRow
        ) {
            if (loc.board == null || graph.cells == null || graph.connect == null) {
                Debug.LogError("BadParams");
                return -1;
            }

            int startRow = cCount;
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j == 0) continue;
                    var dir = new Vector2Int(i, j);

                    int curCol = cCount;
                    var length = GetMaxEmpty(loc, dir);
                    if (length == -1) {
                        Debug.LogError("BadLength");
                        return -1;
                    }
                    var max = length;
                    if ((ch.type == ChType.Checker || kind == ChKind.English) && length != 0) {
                        continue;
                    }

                    var lastPos = GetCellByIndex(loc.board, loc.pos + dir * (length + 1));
                    if (lastPos == new Vector2Int(-1, -1)) continue;

                    var chOpt = loc.board[lastPos.x, lastPos.y];
                    if (chOpt.IsSome() && chOpt.Peel().color == ch.color) continue;

                    var nextPos = GetCellByIndex(loc.board, loc.pos + dir * (length + 2));
                    if (nextPos == new Vector2Int(-1, -1)) continue;

                    length = GetMaxEmpty(new ChLocation { board = loc.board, pos = lastPos }, dir);
                    max = length;
                    if (ch.type == ChType.Checker || kind == ChKind.English) max = 1;
                    max = Mathf.Clamp(length, 0, max);

                    var badDir = false;
                    for (int m = 0; m < max; m++) {
                        for (int k = 0; k < cCount; k++) {
                            var curCell = nextPos + dir * m;
                            if (graph.cells[k] == curCell) {
                                for (int l = 0; l < cCount; l++) {
                                    var connect = graph.connect[k, l];
                                    var isInvMove = connect == marks && graph.cells[l] == loc.pos;
                                    if (isInvMove || ((graph.marks[k] & marks) == marks)) {
                                        badDir = true;
                                    }
                                }
                            }
                        }
                    }

                    if (loc.board[nextPos.x, nextPos.y].IsSome() || badDir) continue;

                    for (int k = 0; k < cCount; k++) {
                        if (graph.cells[k] == nextPos) {
                            curCol = k;
                            startRow = lastColum;
                        }
                    }

                    for (int k = 0; k < max; k++) {
                        if (graph.cells.GetLength(0) < curCol) {
                            Debug.LogError("InsufficientBufferSize");
                            return -1;
                        }
                        graph.cells[curCol] = nextPos + dir * k;
                        graph.marks[curCol] += marks;
                        graph.connect[startRow - 1, curCol] = marks;
                        cCount++;
                        for (int l = 0; l <= cCount; l++) {
                            graph.connect[l, cCount] = 0;
                            graph.connect[cCount, l] = 0;
                        }

                        curCol++;
                        var oldPos = loc.pos;
                        loc.pos = nextPos + dir * k;

                        var row = curRow + 1;
                        cCount = GetAttackPaths(loc, kind, ch, graph, cCount, marks, curCol, row);
                        curCol = cCount;
                        if (cCount == -1) return -1;
                        loc.pos = oldPos;

                    }

                    if (curRow == 0) {
                        marks = marks << 1;
                    }
                }
            }

            return cCount;
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

        private static void GetAllPaths(PossibleGraph graph, int clCount, List<Vector2Int> path) {
            for (int i = 0; i < clCount; i++) {
                path.Add(graph.cells[i]);
            }
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