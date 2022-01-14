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
        OutOrEmpty = OutOfBoard | Empty,
        OutOrFilled = OutOfBoard | Filled
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
            if ((cell.type & CellTy.OutOrEmpty) > 0) {
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
                for (int j = -1; j <= 1 && (cell.ch.type != ChType.Checker || i == xDir); j += 2) {
                    var dir = new Vector2Int(i, j);

                    var length = GetMaxEmpty(loc, dir);
                    if (length == -1) {
                        Debug.LogError("GetPossiblePaths: cant get max empty");
                        return -1;
                    }

                    var max = length;
                    if (cell.ch.type == ChType.Checker || kind == ChKind.English) max = 1;
                    length = Mathf.Clamp(length, 0, max);

                    if (cell.ch.type != ChType.Checker && kind != ChKind.English || length != 1) {
                        var afterLastPos = pos + dir * (length + 1);

                        var afterLastCell = GetCell(board, afterLastPos);
                        var farPos = afterLastPos + dir;
                        var farCell = GetCell(board, farPos);

                        var farCellIsEmpty = (farCell.type & CellTy.Empty) > 0;
                        if ((afterLastCell.type & CellTy.Filled) > 0 && farCellIsEmpty) {
                            needAttack = afterLastCell.ch.color != cell.ch.color;
                            if (needAttack) break;
                        }
                    }

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
                size = GetAttackPaths(loc, kind, cell.ch, graph, 1, 1, 1, 0);
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
            int lastColum,
            int curRow
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

                    int curCol = size;
                    var length = GetMaxEmpty(loc, dir);
                    if (length == -1) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    var max = length;
                    if ((ch.type == ChType.Checker || kind == ChKind.English) && length != 0) {
                        continue;
                    }

                    var afterLastPos = pos + dir * (length + 1);
                    var afterLastCell = GetCell(board, afterLastPos);
                    if ((afterLastCell.type & CellTy.OutOrEmpty) > 0) continue;
                    if (afterLastCell.ch.color == ch.color) continue;

                    var farPos = afterLastPos + dir;
                    var farCell = GetCell(board, farPos);

                    if ((farCell.type & CellTy.OutOrFilled) > 0 && farPos != cells[0]) continue;

                    var newLoc = new ChLocation { board = board, pos = afterLastPos };
                    length = GetMaxEmpty(newLoc, dir);
                    if (length == -1) {
                        Debug.LogError("GetAttackPaths: cant get max empty");
                        return -1;
                    }

                    max = length;
                    if (ch.type == ChType.Checker || kind == ChKind.English) max = 1;
                    max = Mathf.Clamp(length, 0, max);
                    if (farPos == cells[0]) max++;

                    var badDir = false;
                    for (int k = 0; k <= max; k++) {
                        for (int l = 0; l < size; l++) {
                            var curCell = farPos + dir * k;
                            if (cells[l] == curCell) {
                                for (int n = 0; n < size; n++) {
                                    var isInvMove = connect[l, n] == mark && cells[n] == pos;
                                    // var isInvMove = connect[l, n] != 0 && cells[n] == pos;
                                    // isInvMove = isInvMove && (marks[n] & mark) > 0;
                                    if (isInvMove || ((marks[l] & mark) == mark)) badDir = true;
                                }
                            }
                        }
                    }

                    if (badDir) continue;

                    for (int k = 0; k < size; k++) {
                        if (cells[k] == farPos) {
                            curCol = k;
                            startRow = lastColum;
                        }
                    }

                    for (int k = 0; k < max; k++) {
                        if (size == curCol){
                            size = AddNode(farPos + dir * k, graph, size);
                        }
                        marks[curCol] += mark;
                        connect[startRow - 1, curCol] = mark;
                        curCol++;

                        var oldPos = pos;
                        loc.pos = farPos + dir * k;
                        var row = curRow + 1;
                        size = GetAttackPaths(loc, kind, ch, graph, size, mark, curCol, row);
                        curCol = size;
                        if (size == -1) return -1;
                        loc.pos = oldPos;
                    }

                    if (curRow == 0) {
                        mark = mark << 1;
                    }
                }
            }

            return size;
        }

        private static int GetMaxEmpty(ChLocation loc, Vector2Int dir) {
            int len = 0;
            var pos = loc.pos + dir;
            for (var p = pos; GetCell(loc.board, p).type == CellTy.Empty; p += dir, ++len);

            return len;
        }

        private static Cell GetCell(Option<Checker>[,] board, Vector2Int index) {
            if (board == null) {
                Debug.LogError("GetCell: board is null");
                return new Cell();
            }

            var type = CellTy.Filled;
            var checker = new Checker();

            var boardSize = new Vector2Int(board.GetLength(0), board.GetLength(1));
            if (!IsOnBoard(boardSize, index)) type = CellTy.OutOfBoard;

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

            var width = graph.connect.GetLength(0);
            var height = graph.connect.GetLength(1);
            var maxConnectionSize = Mathf.Min(width, height);
            
            var maxSize = size + 1;

            if (cells.Length < maxSize || maxConnectionSize < maxSize || marks.Length < maxSize) {
                Debug.LogError("AddNode: buffer overflow");
                return -1;
            }

            cells[size] = node;
            marks[size] = 0;

            for (int i = 0; i < maxSize; i++) {
                connect[size, i] = connect[i, size] = 0;
            }

            return maxSize;
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

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}