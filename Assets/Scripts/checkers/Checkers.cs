using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        public int [,] matrix;
        public Vector2Int [] nodes;
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
        public static int? GetPossiblePaths(
            ChLocation loc,
            ChKind kind,
            Buffer buf
        ) {
            if (loc.board == null) {
                Debug.LogError("BoardIsNull");
                return null;
            }

            var chOpt = loc.board[loc.pos.x, loc.pos.y];
            if (chOpt.IsNone()) return null;
            var ch = chOpt.Peel();
            var matrixInfo = new MatrixInfo {
                index = Vector2Int.zero,
                needAttack = false,
                markerType = 1
            };
            buf.nodes[0] = loc.pos;

            int startSize = 1;
            var mSize = GetPossibleSubPath(loc, kind, ch, buf, matrixInfo, ref startSize);

            if (!mSize.HasValue) {
                Debug.LogError("badSize");
            }

            return mSize;
        }

        public static void ShowMatrix(Buffer buf) {
            var nodes = "";
            foreach (var a in buf.nodes) {
                nodes += a.ToString() + "   ";
            }

            Debug.Log(nodes);
            var matrix = "";
            for (int i = 0; i < buf.matrix.GetLength(1); i++) {
                matrix = "";
                for (int j = 0; j < buf.matrix.GetLength(0); j++) {
                    matrix += "        " + buf.matrix[i,j].ToString();
                }
                Debug.Log(matrix);
            }
        }

        public static List<List<Vector2Int>> GetAllPaths(
            Buffer buf,
            int mSize,
            List<Vector2Int> path,
            List<List<Vector2Int>> paths,
            int index,
            int marker
        ) {
            for (int i = 0; i < mSize; i++) {
                if (buf.matrix[index, i] != 0 && buf.matrix[index, i] == marker) {
                    Debug.Log(buf.matrix[index, i]);
                    path.Add(buf.nodes[i]);
                    paths.AddRange(GetAllPaths(buf, mSize, path, paths, i, marker));
                    marker++;
                }
            }
            paths.Add(path);
            return paths;
        }

        private static int? GetPossibleSubPath(
            ChLocation loc,
            ChKind kind,
            Checker ch,
            Buffer buf,
            MatrixInfo mInfo,
            ref int mSize
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
                            for (int k = 0; k < mSize; k++) {
                                if (buf.nodes[k] == next + dir) {
                                    for (int l = 0; l < mSize; l++) {
                                        if (buf.matrix[k, l] == mInfo.markerType) isBadDir = true;
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
                                        for (int k = 0; k < mSize; k++) {
                                            buf.matrix[0,k] = 0;
                                        }
                                        wasUsualMove = false;
                                    }

                                    mInfo.needAttack = true;
                                    mInfo.index.y = mSize;
                                    mSize++;

                                    for (int k = 0; k < mSize; k++) {
                                        if (buf.nodes[k] == next) {
                                            mInfo.index.y = k;
                                            mSize--;
                                            break;
                                        }
                                    }

                                    buf.matrix[mInfo.index.x, mInfo.index.y] = mInfo.markerType;
                                    buf.nodes[mInfo.index.y] = next;
                                    var oldInd = mInfo.index;
                                    var oldPos = loc.pos;

                                    var newInd = new Vector2Int(mInfo.index.y, 0);
                                    loc.pos = next;
                                    mInfo.index = newInd;

                                    GetPossibleSubPath(loc, kind, ch, buf, mInfo, ref mSize);
                                    mInfo.index = oldInd;
                                    loc.pos = oldPos;
                                } else if (!mInfo.needAttack) {
                                    wasUsualMove = true;
                                    mInfo.index.y = mSize;
                                    buf.nodes[mInfo.index.y] = next;
                                    buf.matrix[mInfo.index.x, mInfo.index.y] = mInfo.markerType;
                                    mSize++;
                                }
                            }
                            if (mInfo.index.x == 0) {
                                mInfo.markerType++;
                            }

                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }
            return mSize;
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}