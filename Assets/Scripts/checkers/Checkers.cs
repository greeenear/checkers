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
        Checker,
        King
    }

    public enum ChColor {
        White,
        Black,
        Count
    }

    public enum CellTy {
        Filled = 1,
        Empty = 2,
        OutOfBoard = 4,
        Any = Filled | Empty
    }

    public struct ChLoc {
        public Option<Checker>[,] board;
        public Vector2Int pos;

        public static ChLoc Mk(Option<Checker>[,] board, Vector2Int pos) {
            return new ChLoc { board = board, pos = pos };
        }
    }

    public struct PossibleGraph {
        public Vector2Int[] cells;
        public int[,] connect;
        public int[] marks;
    }

    public struct Checker {
        public ChType type;
        public ChColor color;
    }

    public struct Cell {
        public CellTy type;
        public Checker ch;
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

            var size = 0;
            size = AddNode(pos, graph, size);
            if (size < 0) {
                Debug.LogError("GetPossiblePaths: cant added node");
                return -1;
            }

            var xDir = 1;
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            var mark = 1;
            var needAttack = false;
            for (int i = -1; i <= 1 && !needAttack; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);
                    if ((dir.x != xDir && ch.type == ChType.Checker)) continue;

                    var length = 0;
                    if (ch.type == ChType.King) {
                        length = GetMaxApt(loc, dir, CellTy.Empty);
                    }

                    var enemyPos = loc.pos + dir * (length + 1);
                    var filled = GetMaxApt(ChLoc.Mk(board, enemyPos - dir), dir, CellTy.Filled);
                    var any = GetMaxApt(ChLoc.Mk(board, enemyPos - dir), dir, CellTy.Any);
                    
                    var enemyColor = GetCell(board, enemyPos).ch.color;

                    needAttack = filled == 1 && any > 1 && enemyColor != ch.color;
                    if (needAttack) break;

                    if (ch.type == ChType.Checker && filled == 0 && any > 0) length++;
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
                loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.None();
                size = GetAttackPaths(loc, kind, ch, graph, Vector2Int.zero, 1, 1);
                loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.Some(ch);
            }

            return size;
        }

        private static int GetAttackPaths(
            ChLoc loc,
            ChKind kind,
            Checker ch,
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
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            for (int i = -1; i <= 1; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);
                    if (dir == prevDir) continue;

                    if (kind == ChKind.English && dir.x != xDir && ch.type == ChType.Checker) {
                        continue;
                    }
                    var emptyLen = 0;
                    if (ch.type == ChType.King) emptyLen = GetMaxApt(loc, dir, CellTy.Empty);

                    var enemyPos = loc.pos + dir * (emptyLen + 1);
                    var filled = GetMaxApt(ChLoc.Mk(board, enemyPos - dir), dir, CellTy.Filled);
                    var any = GetMaxApt(ChLoc.Mk(board, enemyPos - dir), dir, CellTy.Any);

                    if (filled != 1 || any < 2) continue;

                    if (GetCell(board, enemyPos).ch.color == ch.color) continue;

                    var enemyLoc = ChLoc.Mk(board, enemyPos);
                    emptyLen = GetMaxApt(enemyLoc, dir, CellTy.Empty);
                    if (emptyLen < 0) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    var maxEmptyLen = emptyLen;
                    if (ch.type == ChType.Checker || kind == ChKind.English) maxEmptyLen = 1;
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

        private static int GetMaxApt(ChLoc loc, Vector2Int dir, CellTy type) {
            int len = 0;
            for (var p = loc.pos + dir; (GetCell(loc.board, p).type & type) > 0; p += dir, ++len);

            return len;
        }

        public static Cell GetCell(Option<Checker>[,] board, Vector2Int index) {
            if (board == null) {
                Debug.LogError("GetCell: board is null");
                return new Cell { type = CellTy.OutOfBoard };
            }

            var type = CellTy.Filled;
            var checker = new Checker();

            var boardSize = new Vector2Int(board.GetLength(0), board.GetLength(1));
            if (index.x < 0 || index.x >= boardSize.x || index.y < 0 || index.y >= boardSize.y) {
                type = CellTy.OutOfBoard;
            }

            if (type != CellTy.OutOfBoard) {
                var chOpt = board[index.x, index.y];
                if (chOpt.IsSome()) {
                    checker = chOpt.Peel();
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