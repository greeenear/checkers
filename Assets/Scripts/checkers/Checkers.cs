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
        public static int GetPossiblePaths(ChLocation loc, ChKind kind, PossibleGraph buf) {
            if (loc.board == null || buf.cells == null || buf.connect == null) {
                Debug.LogError("BadParams");
                return -1;
            }
            var bordSize = new Vector2Int(loc.board.GetLength(1),loc.board.GetLength(1));
            if (!IsOnBoard(bordSize, loc.pos)) {
                Debug.LogError("BadPos");
                return -1;
            }

            if (loc.pos == new Vector2Int(5,2)) {
                for (int i = -1; i <= 1; i++) {
                    for (int j = -1; j <= 1; j++) {
                        if (i == 0 || j == 0) continue;
                        var dir = new Vector2Int(i, j);
                        var newVector = GetLastPos(loc, dir);
                        Debug.Log(newVector);
                    }
                }
            }

            var chOpt = loc.board[loc.pos.x, loc.pos.y];
            if (chOpt.IsNone()) return -1;
            var ch = chOpt.Peel();
            var matrixInfo = new MatrixInfo {
                index = Vector2Int.zero,
                needAttack = false,
                markerType = 1
            };
            buf.cells[0] = loc.pos;

            var cellSize = GetPossibleSubPath(loc, kind, ch, buf, matrixInfo, 1);

            return cellSize;
        }


        private static Vector2Int GetLastPos(ChLocation loc, Vector2Int dir) {
            var lastPos = new Vector2Int(-1, -1);
            if (loc.board == null) {
                Debug.LogError("BoardIsNull");
                return lastPos;
            }

            var size = new Vector2Int(loc.board.GetLength(1), loc.board.GetLength(0));
            if (!IsOnBoard(size, loc.pos)) {
                Debug.LogError("BadPos");
                return lastPos;
            }

            var chOpt = loc.board[loc.pos.x, loc.pos.y];
            if (chOpt.IsNone()) return lastPos;
            var ch = chOpt.Peel();

            for (var next = loc.pos + dir; IsOnBoard(size, next); next += dir) {
                lastPos = next;
                if (loc.board[next.x, next.y].IsSome()) {
                    break;
                }
            }

            return lastPos;
        }

        private static bool StopCondition(Checker ch, ChKind kind) {
            if (ch.type == ChType.Checker || kind == ChKind.English) {
                return true;
            }

            return false;
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