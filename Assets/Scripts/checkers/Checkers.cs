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

    public struct Buffer {
        public int [,] conect;
        public Vector2Int [] cells;
        public int cellCount;
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
        public static int GetPossiblePaths(
            ChLocation loc,
            ChKind kind,
            Buffer buf
        ) {
            if (loc.board == null) {
                Debug.LogError("BoardIsNull");
                return -1;
            }

            var chOpt = loc.board[loc.pos.x, loc.pos.y];
            if (chOpt.IsNone()) return -1;
            var ch = chOpt.Peel();
            loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.None();
            var matrixInfo = new MatrixInfo {
                index = Vector2Int.zero,
                needAttack = false,
                markerType = 1
            };
            buf.cells[0] = loc.pos;

            var cellSize = GetPossibleSubPath(loc, kind, ch, buf, matrixInfo);
            loc.board[loc.pos.x, loc.pos.y] = Option<Checker>.Some(ch);

            return cellSize;
        }

        public static void ShowMatrix(Buffer buf) {
            var nodes = "";
            foreach (var a in buf.cells) {
                nodes += a.ToString() + "   ";
            }

            Debug.Log(nodes);
            var matrix = "";
            for (int i = 0; i < buf.conect.GetLength(1); i++) {
                matrix = "";
                for (int j = 0; j < buf.conect.GetLength(0); j++) {
                    matrix += "        " + buf.conect[i,j].ToString();
                }
                Debug.Log(matrix);
            }
        }

        public static List<List<Vector2Int>> GetAllPaths(
            Buffer buf,
            List<Vector2Int> path,
            List<List<Vector2Int>> paths,
            int index,
            int marker
        ) {
            var isLastCell = true;
            for (int i = 0; i < buf.cellCount; i++) {
                if (buf.conect[index, i] != 0 && buf.conect[index, i] == marker) {
                    path.Add(buf.cells[i]);
                    if (i == 0) break;
                    isLastCell = false;
                    GetAllPaths(buf, path, paths, i, marker);
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

        private static int GetPossibleSubPath(
            ChLocation loc,
            ChKind kind,
            Checker ch,
            Buffer buf,
            MatrixInfo mInfo
        ) {
            if (loc.board == null) {
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
                    var size = new Vector2Int(loc.board.GetLength(1), loc.board.GetLength(0));
                    for (var next = loc.pos + dir; IsOnBoard(size, next); next += dir) {
                        var nextOpt = loc.board[next.x, next.y];
                        if (nextOpt.IsSome()) {
                            var nextColor = nextOpt.Peel().color;
                            bool isBadDir = false;
                            for (int k = 0; k < buf.cellCount; k++) {
                                for (var newNext = next + dir; IsOnBoard(size, newNext); newNext += dir) {
                                    if (loc.board[newNext.x, newNext.y].IsSome()) break;
                                    if (buf.cells[k] == newNext) {
                                        for (int l = 0; l < buf.cellCount; l++) {
                                            if (buf.conect[k, l] == mInfo.markerType) isBadDir = true;
                                        }
                                    }
                                    if (k == 0) {
                                        isBadDir = false;
                                        if (buf.conect[0, mInfo.index.x] == mInfo.markerType) {
                                            isBadDir = true;
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
                                        for (int k = 0; k < buf.cellCount; k++) {
                                            buf.conect[0,k] = 0;
                                        }
                                        wasUsualMove = false;
                                    }

                                    mInfo.needAttack = true;
                                    mInfo.index.y = buf.cellCount;
                                    buf.cellCount++;

                                    for (int k = 0; k < buf.cellCount; k++) {
                                        if (buf.cells[k] == next) {
                                            mInfo.index.y = k;
                                            buf.cellCount--;
                                            break;
                                        }
                                    }

                                    buf.conect[mInfo.index.x, mInfo.index.y] = mInfo.markerType;
                                    buf.cells[mInfo.index.y] = next;
                                    var oldInd = mInfo.index;
                                    var oldPos = loc.pos;

                                    var newInd = new Vector2Int(mInfo.index.y, 0);
                                    if (next == buf.cells[0]) break;
                                    if (buf.conect[next.x, next.y] != 0) break;
                                    loc.pos = next;
                                    mInfo.index = newInd;

                                    buf.cellCount = GetPossibleSubPath(loc, kind, ch, buf, mInfo);
                                    mInfo.index = oldInd;
                                    loc.pos = oldPos;
                                    if (mInfo.index.x == 0) {
                                        mInfo.markerType++;
                                    }
                                } else if (!mInfo.needAttack) {
                                    wasUsualMove = true;
                                    mInfo.index.y = buf.cellCount;
                                    buf.cells[mInfo.index.y] = next;
                                    buf.conect[mInfo.index.x, mInfo.index.y] = mInfo.markerType;
                                    buf.cellCount++;
                                    if (mInfo.index.x == 0) {
                                        mInfo.markerType++;
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
            return buf.cellCount;
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}