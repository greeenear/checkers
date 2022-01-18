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
        Filled,
        Empty,
        OutOfBoard
    }

    public struct ChLocation {
        public Option<Checker>[,] board;
        public Vector2Int pos;
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
            if (size == -1) {
                Debug.LogError("GetPossiblePaths: cant added node");
                return -1;
            }

            var xDir = 1;
            if (cell.ch.color == ChColor.White) {
                xDir = -1;
            }

            var needAttack = false;
            var mark = 1;

            for (int i = -1; i <= 1 && !needAttack; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);

                    var length = GetMaxApt(loc, dir, CellTy.Empty);
                    if (length == -1) {
                        Debug.LogError("GetPossiblePaths: cant get max empty");
                        return -1;
                    }

                    var max = length;
                    if (cell.ch.type == ChType.Checker || kind == ChKind.English) max = 1;
                    length = Mathf.Clamp(length, 0, max);

                    if (cell.ch.type != ChType.Checker && kind != ChKind.English || length != 1) {
                        var nextLoc = new ChLocation { pos = pos + dir * length, board = board };
                        var filledLength = GetMaxApt(nextLoc, dir, CellTy.Filled);

                        if (filledLength == 1) {
                            var afterLastCell = GetCell(loc.board, pos + dir * (length + 1));
                            needAttack = afterLastCell.ch.color != cell.ch.color;

                            if (needAttack) break;
                        }
                    }
                    
                    if (cell.ch.type == ChType.Checker && i != xDir) continue;
                    for (int k = 0; k < length; k++) {
                        var newSize = AddNode(pos + dir * (k + 1), graph, size);

                        if (newSize <= size) {
                            Debug.LogError("GetPossiblePaths: cant added node");
                            return -1;
                        }

                        connect[0, size] = 1;
                        marks[size] = mark;
                        size = newSize;
                    }

                    mark = mark << 1;
                }
            }

            if (needAttack) {
                size = GetAttackPaths(loc, kind, cell.ch, graph, 1, 1, 1);
            }

            return size;
        }

        private static int GetAttackPaths(
            ChLocation loc,
            ChKind kind,
            Checker ch,
            PossibleGraph graph,
            int size,
            int mark,
            int lastColum
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

            int startRow = size;
            for (int i = -1; i <= 1; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);

                    var emptyLen = GetMaxApt(loc, dir, CellTy.Empty);
                    if (emptyLen == -1) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    var max = emptyLen;
                    if ((ch.type == ChType.Checker || kind == ChKind.English) && emptyLen != 0) {
                        continue;
                    }

                    var afterLastPos = pos + dir * (emptyLen + 1);
                    var farPos = afterLastPos + dir;
                    if (cells.Length < 1) continue;
                    var isStart = farPos == cells[0];


                    var nextLoc = new ChLocation { pos = pos + dir * emptyLen, board = board };
                    var filledLength = GetMaxApt(nextLoc, dir, CellTy.Filled);
                    if (filledLength == -1) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }
                    if (filledLength != 1 && !isStart) continue;
                    if (GetCell(board, afterLastPos).ch.color == ch.color) continue;

                    var newLoc = new ChLocation { board = board, pos = afterLastPos };
                    emptyLen = GetMaxApt(newLoc, dir, CellTy.Empty);
                    if (emptyLen == -1) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }
                    max = emptyLen;
                    if (ch.type == ChType.Checker || kind == ChKind.English) max = 1;
                    max = Mathf.Clamp(emptyLen, 0, max);
                    if (isStart) max++;

                    var badDir = false;
                    for (int k = 0; k <= max; k++) {
                        for (int l = 0; l < size; l++) {
                            var curCell = farPos + dir * k;
                            if (cells[l] == curCell) {
                                for (int n = 0; n < size; n++) {
                                    var isInvMove = connect[l, n] == mark && cells[n] == pos;
                                    if (isInvMove || ((marks[l] & mark) == mark)) badDir = true;
                                }
                            }
                        }
                    }
                    if (badDir) continue;

                    int curCol = size;
                    for (int k = 0; k < max; k++) {
                        for (int l = 0; l < size; l++) {
                            if (cells[l] == farPos + dir * k) {
                                curCol = l;
                                startRow = lastColum;
                            }
                        }

                        if (size == curCol){
                            size = AddNode(farPos + dir * k, graph, size);
                        }
                        marks[curCol] += mark;
                        connect[startRow - 1, curCol] = mark;
                        curCol++;

                        var oldPos = pos;
                        loc.pos = farPos + dir * k;
                        size = GetAttackPaths(loc, kind, ch, graph, size, mark, curCol);
                        curCol = size;
                        if (size == -1) return -1;
                        loc.pos = oldPos;
                    }

                    if (startRow - 1 == 0) {
                        mark = mark << 1;
                    }
                }
            }

            return size;
        }

        private static int GetMaxApt(ChLocation loc, Vector2Int dir, CellTy type) {
            int len = 0;
            var pos = loc.pos + dir;
            for (var p = pos; GetCell(loc.board, p).type == type; p += dir, ++len);

            return len;
        }

        private static Cell GetCell(Option<Checker>[,] board, Vector2Int index) {
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