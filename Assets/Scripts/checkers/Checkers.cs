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

    public struct ChLocation {
        public Option<Checker>[,] board;
        public Vector2Int pos;

        public static ChLocation Mk(Option<Checker>[,] board, Vector2Int pos) {
            return new ChLocation { board = board, pos = pos };
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
        public static int GetPossiblePaths(ChLocation loc, ChKind kind, PossibleGraph graph) {
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
            if (cell.type == CellTy.OutOfBoard) {
                Debug.LogError("GetPossiblePaths: start position is empty or out of board");
                return -1;
            }

            var size = 0;
            size = AddNode(pos, graph, size);
            if (size < 0) {
                Debug.LogError("GetPossiblePaths: cant added node");
                return -1;
            }

            var needAttack = false;
            var mark = 1;

            var xDir = 1;
            if (cell.ch.color == ChColor.White) {
                xDir = -1;
            }

            for (int i = -1; i <= 1 && !needAttack; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);

                    var length = GetMaxApt(loc, dir, CellTy.Empty);
                    if (length < 0) {
                        Debug.LogError("GetPossiblePaths: cant get max empty");
                        return -1;
                    }

                    var max = length;
                    if (cell.ch.type == ChType.Checker || kind == ChKind.English) max = 1;
                    length = Mathf.Clamp(length, 0, max);

                    if (cell.ch.type != ChType.Checker && kind != ChKind.English || length != 1) {
                        var nextLoc = ChLocation.Mk(board, pos + dir * length);
                        var filledLength = GetMaxApt(nextLoc, dir, CellTy.Filled);

                        if (filledLength == 1 && GetMaxApt(nextLoc, dir, CellTy.Any) > 1) {
                            var afterLastCell = GetCell(loc.board, pos + dir * (length + 1));
                            needAttack = afterLastCell.ch.color != cell.ch.color;

                            if (needAttack) break;
                        }
                    }

                    if ((dir.x != xDir && cell.ch.type == ChType.Checker)) continue;

                    for (int k = 0; k < length; k++) {
                        var newSize = AddNode(pos + dir * (k + 1), graph, size);

                        if (newSize <= size) {
                            Debug.LogError("GetPossiblePaths: cant added node");
                            return -1;
                        }

                        connect[0, size] = mark;
                        marks[size] = mark;
                        size = newSize;
                    }

                    mark = mark * 2;
                }
            }

            if (needAttack) {
                var checker = cell.ch;
                loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.None();
                size = GetAttackPaths(loc, kind, cell.ch, graph, Vector2Int.zero, 1, 1);
                loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.Some(checker);
            }

            return size;
        }

        private static int GetAttackPaths(
            ChLocation loc,
            ChKind kind,
            Checker ch,
            PossibleGraph graph,
            Vector2Int badDir,
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
            }

            var xDir = 1;
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            for (int i = -1; i <= 1; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);
                    if (dir == badDir) continue;

                    if (kind == ChKind.English && dir.x != xDir && ch.type == ChType.Checker) {
                        continue;
                    }

                    var emptyLen = GetMaxApt(loc, dir, CellTy.Empty);
                    if (emptyLen < 0) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    if ((ch.type == ChType.Checker || kind == ChKind.English) && emptyLen != 0) {
                        continue;
                    }

                    var enemyPos = pos + dir * (emptyLen + 1);

                    var nextLoc = ChLocation.Mk(board, enemyPos - dir);
                    var filledLength = GetMaxApt(nextLoc, dir, CellTy.Filled);
                    if (filledLength < 0) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    if (filledLength != 1 || GetMaxApt(nextLoc, dir, CellTy.Any) < 2) continue;

                    if (GetCell(board, enemyPos).ch.color == ch.color) continue;

                    var enemyLoc = ChLocation.Mk(board, enemyPos);
                    emptyLen = GetMaxApt(enemyLoc, dir, CellTy.Empty);
                    if (emptyLen < 0) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    var maxEmptyLen = emptyLen;
                    if (ch.type == ChType.Checker || kind == ChKind.English) maxEmptyLen = 1;
                    emptyLen = Mathf.Clamp(emptyLen, 0, maxEmptyLen);

                    var badDirr = false;
                    for (var p = enemyPos + dir; p != enemyPos + dir * (emptyLen + 1); p += dir) {
                        var curCellIndex = Array.IndexOf(cells, p, 0, size);

                        for (int n = 0; n < size && curCellIndex > 0; n++) {
                            if ((marks[curCellIndex] & mark) > 0) badDirr = true;
                        }
                    }

                    if (badDirr) continue;

                    for (int k = 0; k < emptyLen; k++) {
                        var attackPos = enemyPos + dir * (k + 1);
                        int attackPosInd = Array.IndexOf(cells, attackPos, 0, size);

                        if (attackPosInd < 0) {
                            attackPosInd = size;
                            size = AddNode(attackPos, graph, size);

                            if (size < 0) return -1;
                        }
                        marks[attackPosInd] += mark;
                        connect[posIndex, attackPosInd] += mark;


                        var newLoc = ChLocation.Mk(board, attackPos);
                        size = GetAttackPaths(newLoc, kind, ch, graph, -dir, size, mark);
                        if (size < 0) return -1;
                    }

                    if (posIndex == 0) {
                        mark = mark * 2;
                    }
                }
            }

            return size;
        }

        private static int GetMaxApt(ChLocation loc, Vector2Int dir, CellTy type) {
            int len = 0;
            var pos = loc.pos + dir;
            for (var p = pos; (GetCell(loc.board, p).type & type) > 0; p += dir, ++len);

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