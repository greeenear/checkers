using System.Collections.Generic;
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

    public struct ChLoc {
        public int[,] board;
        public Vector2Int pos;
    }

    public struct PossibleGraph {
        public Vector2Int[] cells;
        public int[,] connect;
        public int[] marks;
        public EnemyInfo[] enemies;
    }

    public struct EnemyInfo {
        public Vector2Int pos;
        public int mark;
    }


    public static class Checkers {
        public static int FILLED = 1;
        public static int WHITE = 2;
        public static int KING = 4;

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

            var ch = GetCell(board, pos);
            if (ch % 2 == 0) {
                Debug.LogError("GetPossiblePaths: start position is empty or out of board");
                return -1;
            }
            var color = ch & WHITE;
            var chType = ch & KING;

            var size = 0;
            size = AddNode(pos, graph, size);
            if (size < 0) {
                Debug.LogError("GetPossiblePaths: cant added node");
                return -1;
            }

            var xDir = 1;
            if ((ch & WHITE) > 0) {
                xDir = -1;
            }

            var mark = 1;
            var needAttack = false;
            for (int i = -1; i <= 1 && !needAttack; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);

                    var length = 0;
                    if (chType > 0) {
                        length = GetMaxApt(board, pos, dir, 0);
                    }

                    var enemyPos = loc.pos + dir * (length + 1);
                    var filled = GetMaxApt(board, enemyPos - dir, dir, 1);
                    var any = GetMaxApt(board, enemyPos - dir, dir, 2);
                    
                    var enemyIsWhite = GetCell(board, enemyPos) & WHITE;

                    needAttack = filled == 1 && any > 1 && enemyIsWhite != color;
                    if (needAttack) break;

                    if ((dir.x != xDir && chType == 0)) continue;

                    if (chType == 0 && filled == 0 && any > 0) length = 1;
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
                size = GetAttackPaths(loc, kind, ch, graph, 1, 0, 1).Item1;
                loc.board[loc.pos.x, loc.pos.y] = ch;
            }

            return size;
        }

        private static (int, int) GetAttackPaths(
            ChLoc loc,
            ChKind kind,
            int ch,
            PossibleGraph graph,
            int size,
            int enCount,
            int mark
        ) {
            var board = loc.board;
            var pos = loc.pos;
            var connect = graph.connect;
            var cells = graph.cells;
            var marks = graph.marks;
            var enemies = graph.enemies;

            if (board == null || cells == null || connect == null || marks == null) {
                Debug.LogError("GetAttackPaths: incorrect parameters");
                return (-1, -1);
            }

            if (cells.Length < 1) {
                Debug.LogError("GetAttackPaths: cells empty");
                return (-1, -1);
            }
            var posIndex = Array.IndexOf(cells, pos);

            if (posIndex < 0) {
                Debug.LogError("GetAttackPaths: incorrect position");
                return (-1, -1);
            }

            var chType = ch & KING;

            var xDir = 1;
            if ((ch & WHITE) > 0) {
                xDir = -1;
            }

            for (int i = -1; i <= 1; i += 2) {
                for (int j = -1; j <= 1; j += 2) {
                    var dir = new Vector2Int(i, j);

                    if (kind == ChKind.English && dir.x != xDir && chType == 0) {
                        continue;
                    }
                    var emptyLen = 0;
                    if (chType > 0) {
                        emptyLen = GetMaxApt(loc.board, loc.pos, dir, 0);
                    }

                    var enemyPos = loc.pos + dir * (emptyLen + 1);
                    var filled = GetMaxApt(board, enemyPos - dir, dir, 1);
                    var any = GetMaxApt(board, enemyPos - dir, dir, 2);

                    if (filled != 1 || any < 2) continue;

                    var chIsWhite = (ch & WHITE) > 0;
                    var enIsWhite = (GetCell(board, enemyPos) & WHITE) > 0;

                    if (chIsWhite == enIsWhite) continue;

                    emptyLen = GetMaxApt(board, enemyPos, dir, 0);

                    var maxEmptyLen = emptyLen;
                    if (chType == 0 || kind == ChKind.English) maxEmptyLen = 1;

                    emptyLen = Mathf.Clamp(emptyLen, 0, maxEmptyLen);

                    var newEnInfo =  new EnemyInfo { pos = enemyPos, mark = mark };
                    var enInd = Array.IndexOf(enemies, newEnInfo, 0, enCount);
                    if (enInd < 0) {
                        enemies[enCount] = newEnInfo;
                        enCount++;
                    } else {
                        continue;
                    }

                    for (int k = 0; k < emptyLen; k++) {
                        var attackPos = enemyPos + dir * (k + 1);
                        int attackPosInd = Array.IndexOf(cells, attackPos, 0, size);

                        if (attackPosInd < 0) {
                            attackPosInd = size;
                            size = AddNode(attackPos, graph, size);

                            if (size < 0) return (-1, -1);
                        }
                        marks[attackPosInd] |= mark;
                        connect[posIndex, attackPosInd] |= mark;

                        var newLoc = new ChLoc { board = board, pos = attackPos };
                        var curSize = GetAttackPaths(newLoc, kind, ch, graph, size, enCount, mark);
                        size = curSize.Item1;
                        enCount = curSize.Item2;
                        if (size < 0) return (-1, -1);
                    }

                    if (posIndex == 0) {
                        mark = mark << 1;
                    }
                }
            }

            return (size, enCount);
        }

        private static int GetMaxApt(int[,] board, Vector2Int pos, Vector2Int dir, int type) {
            int len = 0;
            for (var p = pos + dir ; ; p += dir, ++len) {
                var cell = GetCell(board, p);

                if (cell != 0 && type == 0) break;

                if (((cell & type) == 0 || cell == -1) && type == 1) break;

                if (cell == -1 && type == 2) break;
            }

            return len;
        }

        public static int GetCell(int[,] board, Vector2Int index) {
            if (board == null) {
                Debug.LogError("GetCell: board is null");
                return 0;
            }

            int checker = 0;

            var boardSize = new Vector2Int(board.GetLength(0), board.GetLength(1));
            if (index.x < 0 || index.x >= boardSize.x || index.y < 0 || index.y >= boardSize.y) {
                checker = -1;
            }

            if (checker != -1) {
                checker = board[index.x, index.y];
            }

            return checker;
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