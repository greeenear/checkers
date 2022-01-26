using System;
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
        Checker = 4,
        King = 8
    }

    public enum ChColor {
        White = 1,
        Black = 2
    }

    public enum CellTy {
        Filled = 1,
        Empty = 2,
        OutOfBoard = 4,
        Any = Filled | Empty
    }

    public struct ChLoc {
        public int[,] board;
        public Vector2Int pos;

        public static ChLoc Mk(int[,] board, Vector2Int pos) {
            return new ChLoc { board = board, pos = pos };
        }
    }

    public struct PossibleGraph {
        public Vector2Int[] cells;
        public int[,] connect;
        public int[] marks;
    }

    public struct Cell {
        public CellTy type;
        public int ch;
    }

    public static class Checkers {
        public static int GetPossiblePaths(ChLoc loc, ChKind kind, PossibleGraph graph) {
            var cells = graph.cells;
            var connect = graph.connect;
            var marks = graph.marks;
            var board = loc.board;
            var pos = loc.pos;

            if (board == null || cells == null || connect == null || marks == null) {
                Debug.LogError("GetPossiblePaths: incorrect parameters");
                return -1;
            }

            var cell = GetCell(board, pos);
            if (cell.type != CellTy.Filled) {
                Debug.LogError("GetPossiblePaths: start position is empty or out of board");
                return -1;
            }
            var ch = cell.ch;
            var chIsWhite = (ch & (int)ChColor.White) > 0;

            var size = 0;
            size = AddNode(pos, graph, size);
            if (size < 0) {
                Debug.LogError("GetPossiblePaths: cant added node");
                return -1;
            }

            var xDir = 1;
            if ((ch & (int)ChColor.White) > 0) {
                xDir = -1;
            }

            var mark = 1;
            var needAttack = false;
            for (int i = -1; i <= 1 && !needAttack; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);

                    var length = 0;
                    if ((ch & (int)ChType.King) > 0) {
                        length = GetMaxApt(board, pos, dir, CellTy.Empty);
                    }

                    var enemyPos = loc.pos + dir * (length + 1);
                    var filled = GetMaxApt(board, enemyPos - dir, dir, CellTy.Filled);
                    var any = GetMaxApt(board, enemyPos - dir, dir, CellTy.Any);
                    
                    var enemyIsWhite = (GetCell(board, enemyPos).ch & (int)ChColor.White) > 0;

                    needAttack = filled == 1 && any > 1 && enemyIsWhite != chIsWhite;
                    if (needAttack) break;

                    if ((dir.x != xDir && (ch & (int)ChType.Checker) > 0)) continue;

                    if ((ch & (int)ChType.Checker) > 0 && filled == 0 && any > 0) length++;
                    for (int k = 0; k < length; k++) {
                        var newSize = AddNode(pos + dir * (k + 1), graph, size);

                        if (newSize <= size) {
                            Debug.LogError("GetPossiblePaths: cant added node");
                            return -1;
                        }

                        connect[0, size] |= mark;
                        marks[size] |= mark;
                        size = newSize;
                    }

                    mark = mark << 1;
                }
            }

            if (needAttack) {
                loc.board[loc.pos.x, loc.pos.y] = 0;
                size = GetAttackPaths(loc, kind, ch, graph, Vector2Int.zero, 1, 1);
                loc.board[loc.pos.x, loc.pos.y] = ch;
            }

            return size;
        }

        private static int GetAttackPaths(
            ChLoc loc,
            ChKind kind,
            int ch,
            PossibleGraph graph,
            Vector2Int prevDir,
            int size,
            int mark
        ) {
            var board = loc.board;
            var pos = loc.pos;
            var connect = graph.connect;
            var cells = graph.cells;
            var marks = graph.marks;

            if (board == null || cells == null || connect == null || marks == null) {
                Debug.LogError("GetAttackPaths: incorrect parameters");
                return -1;
            }

            if (cells.Length < 1) {
                Debug.LogError("GetAttackPaths: cells empty");
                return -1;
            }
            var posIndex = Array.IndexOf(cells, pos);

            if (posIndex < 0) {
                Debug.LogError("GetAttackPaths: incorrect position");
                return -1;
            }

            var xDir = 1;
            if ((ch & (int)ChColor.White) > 0) {
                xDir = -1;
            }

            for (int i = -1; i <= 1; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);
                    if (dir == prevDir) continue;

                    var isChecker = (ch & (int)ChType.Checker) > 0;
                    if (kind == ChKind.English && dir.x != xDir && isChecker) {
                        continue;
                    }
                    var emptyLen = 0;
                    if ((ch & (int)ChType.King) > 0) {
                        emptyLen = GetMaxApt(loc.board, loc.pos, dir, CellTy.Empty);
                    }

                    var enemyPos = loc.pos + dir * (emptyLen + 1);
                    var filled = GetMaxApt(board, enemyPos - dir, dir, CellTy.Filled);
                    var any = GetMaxApt(board, enemyPos - dir, dir, CellTy.Any);

                    if (filled != 1 || any < 2) continue;

                    var chIsWhite = (ch & (int)ChColor.White) > 0;
                    var enIsWhite = (GetCell(board, enemyPos).ch & (int)ChColor.White) > 0;

                    if (chIsWhite == enIsWhite) continue;

                    emptyLen = GetMaxApt(board, enemyPos, dir, CellTy.Empty);
                    if (emptyLen < 0) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    var maxEmptyLen = emptyLen;
                    if ((ch & (int)ChType.Checker) > 0 || kind == ChKind.English) maxEmptyLen = 1;
                    emptyLen = Mathf.Clamp(emptyLen, 0, maxEmptyLen);

                    var circleDir = false;
                    for (var p = enemyPos + dir; p != enemyPos + dir * (emptyLen + 1); p += dir) {
                        var curCellIndex = Array.IndexOf(cells, p, 0, size);

                        for (int n = 0; n < size && curCellIndex > 0; n++) {
                            if ((marks[curCellIndex] & mark) > 0) circleDir = true;
                        }
                    }
                    if (circleDir) continue;

                    for (int k = 0; k < emptyLen; k++) {
                        var attackPos = enemyPos + dir * (k + 1);
                        int attackPosInd = Array.IndexOf(cells, attackPos, 0, size);

                        if (attackPosInd < 0) {
                            attackPosInd = size;
                            size = AddNode(attackPos, graph, size);

                            if (size < 0) return -1;
                        }
                        marks[attackPosInd] |= mark;
                        connect[posIndex, attackPosInd] |= mark;

                        var newLoc = ChLoc.Mk(board, attackPos);
                        size = GetAttackPaths(newLoc, kind, ch, graph, -dir, size, mark);
                        if (size < 0) return -1;
                    }

                    if (posIndex == 0) {
                        mark = mark << 1;
                    }
                }
            }

            return size;
        }

        private static int GetMaxApt(int[,] board, Vector2Int pos, Vector2Int dir, CellTy type) {
            int len = 0;
            for (var p = pos + dir; (GetCell(board, p).type & type) > 0; p += dir, ++len);

            return len;
        }

        public static Cell GetCell(int[,] board, Vector2Int index) {
            if (board == null) {
                Debug.LogError("GetCell: board is null");
                return new Cell { type = CellTy.OutOfBoard };
            }

            var type = CellTy.Filled;
            int checker = 0;

            var boardSize = new Vector2Int(board.GetLength(0), board.GetLength(1));
            if (index.x < 0 || index.x >= boardSize.x || index.y < 0 || index.y >= boardSize.y) {
                type = CellTy.OutOfBoard;
            }

            if (type != CellTy.OutOfBoard) {
                var ch = board[index.x, index.y];
                if (ch != 0) {
                    checker = ch;
                } else {
                    type = CellTy.Empty;
                }
            }

            return new Cell { type = type, ch = checker };
        }

        private static int AddNode(Vector2Int node, PossibleGraph graph, int size) {
            var cells = graph.cells;
            var connect = graph.connect;
            var marks = graph.marks;

            if (graph.cells == null || graph.connect == null || graph.marks == null) return -1;

            var width = connect.GetLength(0);
            var height = connect.GetLength(1);
            var minSide = Mathf.Min(width, height);

            var nextSize = size + 1;

            if (cells.Length < nextSize || minSide < nextSize || marks.Length < nextSize) {
                Debug.LogError("AddNode: buffer overflow");
                return -1;
            }

            cells[size] = node;
            marks[size] = 0;

            for (int i = 0; i < nextSize; i++) {
                connect[size, i] = connect[i, size] = 0;
            }

            return nextSize;
        }

        public static void ShowMatrix(PossibleGraph graph) {
            if (graph.cells == null || graph.connect == null) {
                Debug.LogError("ShowMatrix: incorrect parameters");
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
    }
}