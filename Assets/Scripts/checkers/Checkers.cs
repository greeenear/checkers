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
            var boardSize = new Vector2Int(loc.board.GetLength(1),loc.board.GetLength(1));
            if (!IsOnBoard(boardSize, loc.pos)) {
                Debug.LogError("BadPos");
                return -1;
            }
            graph.cells[0] = loc.pos;

            var chOpt = loc.board[loc.pos.x, loc.pos.y];
            if (chOpt.IsNone()) return -1;
            var ch = chOpt.Peel();

            var needAttack = false;
            int marks = 1;
            int cellCount = 1;
            if (loc.pos == new Vector2Int(5,4)) {
                for (int i = -1; i <= 1; i++) {
                    if (needAttack) break;
                    for (int j = -1; j <= 1; j++) {
                        if (i == 0 || j == 0) continue;

                        var dir = new Vector2Int(i, j);
                        var length = GetLengthEmptyLine(loc, dir);
                        var fixLength = GetFixedLength(length, kind, ch);

                        var lastPos = loc.pos + dir * fixLength;
                        if (loc.board[lastPos.x, lastPos.y].IsSome()) {
                            var nextPos = loc.pos + dir * (length + 2);
                            if (!IsOnBoard(boardSize, nextPos)) continue;

                            if (loc.board[nextPos.x, nextPos.y].IsNone()) {
                                needAttack = true;
                                break;
                            }
                        }

                        for (int k = 0; k < fixLength; k++) {
                            graph.cells[cellCount] = loc.pos + dir * (k + 1);
                            graph.connect[0, cellCount] = marks;
                            cellCount++;
                        }
                        marks++;
                    }
                }

                if (needAttack) {
                    for (int i = 0; i < cellCount; i++) {
                        graph.connect[0, i + 1] = 0;
                        graph.cells[i + 1] = Vector2Int.zero;
                    }

                    cellCount = 1;
                    cellCount = FillAttackGraph(loc, kind, ch, graph, 1, marks, 1);
                    if (cellCount == -1) return -1;
                }
                ShowMatrix(graph);
            }

            return cellCount;
        }

        private static int FillAttackGraph(
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
                    var length = GetLengthEmptyLine(loc, dir);
                    var fixLength = GetFixedLength(length, kind, ch);
                    if (fixLength != length) continue;
        
                    var boardSize = new Vector2Int(loc.board.GetLength(1), loc.board.GetLength(1));
                    var newPos = loc.pos + dir * (length + 1);
                    var badDir = false;
                    for (int k = 0; k < cellCount; k++) {
                        if (graph.cells[k] == newPos) {
                            for (int l = 0; l < cellCount; l++) {
                                if (graph.connect[l, k] == marks) {
                                    badDir = true;
                                }
                            }
                        }
                    }

                    if (!IsOnBoard(boardSize, newPos) || badDir) continue;

                    if (loc.board[newPos.x, newPos.y].IsSome()) continue;

                    for (int k = 0; k < cellCount; k++) {
                        if (graph.cells[k] == newPos) {
                            curColum = k;
                            startRow = lastColum;
                        }
                    }

                    for (int k = 0; k < fixLength; k++) {
                        graph.cells[curColum] = newPos + dir * k;
                        graph.connect[startRow - 1, curColum] = marks;
                        cellCount++;
                        curColum++;
                    }

                    var oldPos = loc.pos;
                    loc.pos = newPos;
                    cellCount = FillAttackGraph(loc, kind, ch, graph, cellCount, marks, curColum);
                    loc.pos = oldPos;

                    if (newPos - 2 * dir == graph.cells[0]) marks++;
                }
            }

            return cellCount;
        }

        private static int GetLengthEmptyLine(ChLocation loc, Vector2Int dir) {
            if (loc.board == null) {
                Debug.LogError("BoardIsNull");
                return 0;
            }

            var lastPos = new Vector2Int(-1, -1);

            var size = new Vector2Int(loc.board.GetLength(1), loc.board.GetLength(0));
            if (!IsOnBoard(size, loc.pos)) {
                Debug.LogError("BadPos");
                return 0;
            }

            int length = 0;
            for (var next = loc.pos + dir; IsOnBoard(size, next); next += dir) {
                length++;
                lastPos = next;
                if (loc.board[next.x, next.y].IsSome()) {
                    break;
                }
            }

            return length;
        }

        private static int GetFixedLength(int length, ChKind kind, Checker ch) {
            if (length == 0) return 0;
            if (ch.type == ChType.Checker || kind == ChKind.English) {
                return 1;
            }

            return length;
        }

        private static int GetPossibleSubPath(
            ChLocation loc,
            ChKind kind,
            Checker ch,
            PossibleGraph buf,
            MatrixInfo mInfo,
            int count
        ) {
            if (loc.board == null || buf.cells == null || buf.connect == null) {
                Debug.LogError("BadParams");
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
                    var size = new Vector2Int(loc.board.GetLength(1), loc.board.GetLength(0));
                    for (var next = loc.pos + dir; IsOnBoard(size, next); next += dir) {
                        var nextOpt = loc.board[next.x, next.y];
                        if (nextOpt.IsSome()) {
                            var nextColor = nextOpt.Peel().color;
                            bool isBadDir = false;
                            for (int k = 0; k < count; k++) {
                                for (var cell = next + dir; IsOnBoard(size, cell); cell += dir) {
                                    if (loc.board[cell.x, cell.y].IsSome()) break;
                                    if (buf.cells[k] == cell) {
                                        for (int l = 0; l < count; l++) {
                                            if (buf.connect[k, l] == mInfo.markerType) {
                                                isBadDir = true;
                                            }
                                        }

                                        if (k == 0) {
                                            isBadDir = false;
                                            if (buf.connect[0, mInfo.index.x] == mInfo.markerType) {
                                                isBadDir = true;
                                            }
                                        }
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
                                        for (int k = 0; k < count; k++) {
                                            buf.connect[0,k] = 0;
                                        }
                                        wasUsualMove = false;
                                    }

                                    mInfo.needAttack = true;
                                    mInfo.index.y = count;
                                    count++;

                                    for (int k = 0; k < count; k++) {
                                        if (buf.cells[k] == next) {
                                            mInfo.index.y = k;
                                            count--;
                                            break;
                                        }
                                    }

                                    buf.connect[mInfo.index.x, mInfo.index.y] = mInfo.markerType;
                                    buf.cells[mInfo.index.y] = next;
                                    var oldInd = mInfo.index;
                                    var oldPos = loc.pos;

                                    var newInd = new Vector2Int(mInfo.index.y, 0);
                                    if (next == buf.cells[0]) break;
                                    if (buf.connect[next.x, next.y] != 0) break;
                                    loc.pos = next;
                                    mInfo.index = newInd;

                                    count = GetPossibleSubPath(loc, kind, ch, buf, mInfo, count);
                                    mInfo.index = oldInd;
                                    loc.pos = oldPos;
                                    if (mInfo.index.x == 0) {
                                        bool changeMarker = false;
                                        for (int k = 0; k < count; k++) {
                                            if (buf.cells[k] == next - dir) {
                                                changeMarker = true;
                                                break;
                                            }
                                        }

                                        if (changeMarker || ch.type == ChType.Checker) {
                                            mInfo.markerType++;
                                        }
                                    }
                                } else if (!mInfo.needAttack) {
                                    wasUsualMove = true;
                                    mInfo.index.y = count;
                                    buf.cells[mInfo.index.y] = next;
                                    buf.connect[mInfo.index.x, mInfo.index.y] = mInfo.markerType;
                                    count++;
                                    if (mInfo.index.x == 0) {
                                        bool changeMarker = false;
                                        for (int k = 0; k < count; k++) {
                                            if (buf.cells[k] == next - dir) {
                                                changeMarker = true;
                                                break;
                                            }
                                        }
                                        if (changeMarker) mInfo.markerType++;
                                    }
                                }
                            }

                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }
            return count;
        }

        public static void ShowMatrix(PossibleGraph buf) {
            if (buf.cells == null || buf.connect == null) {
                Debug.LogError("BadParams");
                return;
            }

            var nodes = "";
            foreach (var a in buf.cells) {
                nodes += a.ToString() + "   ";
            }

            Debug.Log(nodes);
            var matrix = "";
            for (int i = 0; i < buf.connect.GetLength(1); i++) {
                matrix = "";
                for (int j = 0; j < buf.connect.GetLength(0); j++) {
                    matrix += "        " + buf.connect[i,j].ToString();
                }
                Debug.Log(matrix);
            }
        }

        public static List<List<Vector2Int>> GetAllPaths(
            PossibleGraph buf,
            List<Vector2Int> path,
            List<List<Vector2Int>> paths,
            int index,
            int marker,
            int cellCount
        ) {
            if (buf.cells == null || buf.connect == null || path == null || paths == null) {
                Debug.Log("BadParams");
                return null;
            }

            var isLastCell = true;
            for (int i = 0; i < cellCount; i++) {
                if (buf.connect[index, i] != 0 && buf.connect[index, i] == marker) {
                    path.Add(buf.cells[i]);
                    if (i == 0) break;
                    isLastCell = false;
                    GetAllPaths(buf, path, paths, i, marker, cellCount);
                    if (index == 0) {
                        marker++;
                        path.Clear();
                    }
                }
            }

            if (isLastCell) paths.Add(new List<Vector2Int>(path));
            if (path.Count != 0) path.RemoveAt(path.Count - 1);

            return paths;
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}