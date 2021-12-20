using System.Security.Cryptography.X509Certificates;
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

    public struct GraphEdge {
        public Vector2Int start;
        public Vector2Int end;
        public bool isAttack;

        public static GraphEdge Mk(Vector2Int Start, Vector2Int End, bool isAttack) {
            return new GraphEdge { start = Start, end = End, isAttack = isAttack };
        }
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

    public static class Movement {
        public static List<GraphEdge> GetMovesTree(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return null;
            }
            var boardClone = (Option<Checker>[,])board.Clone();

            var chOpt = board[pos.x, pos.y];
            if (chOpt.IsNone()) return null;
            var ch = chOpt.Peel();
            var mark = new HashSet<Vector2Int>();
            var node = new Node();
            node.cell.pos = pos;
            var a = CalcNodes(board, pos, kind, ch, mark, node);

            return new List<GraphEdge>();
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

        public static List<List<Vector2Int>> GeneratePaths(Node node, List<Vector2Int> path) {
            var paths = new List<List<Vector2Int>>();
            path.Add(node.cell.pos);
            foreach (var childNode in node.child) {
                paths.AddRange(GeneratePaths(childNode, path));
            }
            if (node.child.Count == 0) {
                var newPath = new List<Vector2Int>(path);
                paths.Add(newPath);
            }

            path.RemoveAt(path.Count - 1);
            return paths;
        }

        public static void GetMoveFromPath() {

        }

        public static void Move(Option<Checker>[,] board, Vector2Int from, Vector2Int to) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }

            board[to.x, to.y] = board[from.x, from.y];
            board[from.x, from.y] = Option<Checker>.None();
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }

    }
}