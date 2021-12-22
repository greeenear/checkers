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

    public struct Checker {
        public ChType type;
        public ChColor color;
    }

    public struct Node {
        public CellInfo cell;
        public List<Node> child;

        public static Node Mk(CellInfo cell, List<Node> child) {
            return new Node { cell = cell, child = child };
        }
    }

    public struct CellInfo {
        public Vector2Int pos;
        public bool isAttack;
        
        public static CellInfo Mk(Vector2Int pos, bool isAttack) {
            return new CellInfo { pos = pos, isAttack = isAttack };
        }
    }

    public static class Checkers {
        public static Node GetMovesTree(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind
        ) {
            var node = new Node();
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return node;
            }

            var chOpt = board[pos.x, pos.y];
            if (chOpt.IsNone()) return node;
            var ch = chOpt.Peel();
            node.cell.pos = pos;
            var tree = CalcNodes(board, pos, kind, ch, new HashSet<Vector2Int>(), node);

            return tree;
        }

        public static Node GetNodeFromTree(Node tree, Vector2Int pos) {
            if (tree.cell.pos == pos) return tree;
            if (tree.child.Count == 0) return new Node();

            foreach (var child in tree.child) {
                if (child.cell.pos == pos) {
                    return child;
                } else {
                    tree = GetNodeFromTree(child, pos);
                }
            }

            return tree;
        }

        public static void ShowTree(Node tree, Vector2Int pos) {
            foreach (var child in tree.child) {
                Debug.Log(child.cell.pos);
                ShowTree(child, pos);
            }
        }

        public static bool CheckNeedAttack(Node tree) {
            foreach (var child in tree.child) {
                if (child.cell.isAttack) return true;
                CheckNeedAttack(child);
            }

            return false;
        }

        public static Node GetAttackingTree(Node tree) {
            for (int i = 0; i < tree.child.Count; i++) {
                if (!tree.child[i].cell.isAttack) {
                    tree.child.Remove(tree.child[i]);
                    i--;
                    continue;
                }
                GetAttackingTree(tree.child[i]);
            }

            return tree;
        }

        public static void Move(Option<Checker>[,] board, Vector2Int from, Vector2Int to) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }

            var chOpt = board[from.x, from.y];
            if (chOpt.IsNone()) return;
            var ch = chOpt.Peel();

            board[to.x, to.y] = Option<Checker>.Some(ch);
            board[from.x, from.y] = Option<Checker>.None();
        }

        public static void RemoveChecker(Option<Checker>[,] board, Vector2Int pos) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }

            if (IsOnBoard(new Vector2Int(board.GetLength(0), board.GetLength(1)), pos)) {
                board[pos.x, pos.y] = Option<Checker>.None();
            }
        }

        private static Node CalcNodes(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Checker ch,
            HashSet<Vector2Int> marked,
            Node node
        ) {
            if (node.child == null) {
                node.child = new List<Node>();
            }

            if (board == null) {
                Debug.LogError("BoardIsNull");
                return new Node();
            }

            var xDir = 1;
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j == 0) continue;

                    var dir = new Vector2Int(i, j);
                    var chFound = false;
                    var size = new Vector2Int(board.GetLength(1), board.GetLength(0));
                    for (var next = pos + dir; IsOnBoard(size, next); next += dir) {
                        var nextOpt = board[next.x, next.y];
                        if (nextOpt.IsSome()) {
                            var nextColor = nextOpt.Peel().color;
                            bool isMarked = false;
                            if (marked.Contains(next)) {
                                isMarked = true;
                            }
                            if (isMarked || chFound || nextColor == ch.color) {
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
                                    marked.Add(next - dir);
                                    var nextCell = CellInfo.Mk(next, true);
                                    var newNode = Node.Mk(nextCell, new List<Node>());
                                    node.child.Add(newNode);
                                    CalcNodes(board, next, kind, ch, marked, newNode);
                                } else if (marked.Count == 0) {
                                    var nextCell = CellInfo.Mk(next, false);
                                    node.child.Add(Node.Mk(nextCell, new List<Node>()));
                                }
                            }
                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }

            marked.Clear();
            return node;
        }

        public static Option<CellInfo>[,] CalcNodes2(//переименовать
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            Checker ch,
            int a,
            Option<CellInfo>[,] matrix,//переименовать
            Vector2Int badDir
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return null;
            }

            var xDir = 1;
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j == 0) continue;

                    var count = 0;
                    var dir = new Vector2Int(i, j);
                    var chFound = false;
                    var size = new Vector2Int(board.GetLength(1), board.GetLength(0));
                    for (var next = pos + dir; IsOnBoard(size, next); next += dir) {
                        var nextOpt = board[next.x, next.y];
                        if (nextOpt.IsSome()) {
                            var nextColor = nextOpt.Peel().color;
                            bool isMarked = false;//новое имя
                            if (dir == -badDir) isMarked = true;

                            if (isMarked || chFound || nextColor == ch.color) {
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
                                    var nextCell = CellInfo.Mk(next, true);
                                    Debug.Log(nextCell.pos + "=");
                                    matrix[a, a + count] = Option<CellInfo>.Some(nextCell);
                                    count++;
                                    CalcNodes2(board, next, kind, ch, a = a + count, matrix, dir);
                                } else {
                                    var nextCell = CellInfo.Mk(next, false);
                                    Debug.Log(nextCell.pos + "-");
                                    count++;
                                    matrix[a, a + count] = Option<CellInfo>.Some(nextCell);
                                }
                            }
                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }

            return matrix;
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }

    }
}