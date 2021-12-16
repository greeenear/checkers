using System.Collections.Generic;
using UnityEngine;
using controller;
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

    public struct MoveInfo {
        public Vector2Int cellPos;
        public bool isAttack;

        public static MoveInfo Mk(Vector2Int cellPos, bool isAttack) {
            return new MoveInfo {cellPos = cellPos, isAttack = isAttack };
        }
    }

    public static class Movement {
        public static List<MoveInfo> GetCheckerMoves(
            Option<Checker>[,] board,
            Vector2Int pos,
            ChKind kind,
            List<Vector2Int> markeds
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return null;
            }

            var chOpt = board[pos.x, pos.y];
            if (chOpt.IsNone()) return null;
            var ch = chOpt.Peel();

            var xDir = 1;
            if (ch.color == ChColor.White) {
                xDir = -1;
            }

            var moves = new List<MoveInfo>();
            var size = new Vector2Int(board.GetLength(1), board.GetLength(0));
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j ==0) {
                        continue;
                    }

                    var dir = new Vector2Int(i, j);
                    var chFound = false;
                    for (var next = pos + dir; IsOnBoard(size, next); next += dir) {
                        var nextOpt = board[next.x, next.y];
                        if (nextOpt.IsSome()) {
                            var nextColor = nextOpt.Peel().color;
                            var isSentenced = markeds.Contains(next);
                            if (isSentenced || chFound || nextColor == ch.color) break;
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
                                    markeds.Add(next - dir);
                                    board[next.x, next.y] = board[pos.x, pos.y];
                                    moves.AddRange(GetCheckerMoves(board, next, kind, markeds));
                                }
                                moves.Add(MoveInfo.Mk(next, chFound));
                            }
                            if (ch.type == ChType.Checker || kind == ChKind.English) {
                                break;
                            }
                        }
                    }
                }
            }

            return moves;
        }

        public static Dictionary<Vector2Int, List<MoveInfo>> GetCheckersMoves(
            Option<Checker>[,] board,
            ChColor color,
            ChKind kind
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return null;
            }

            var allCheckersMoves = new Dictionary<Vector2Int, List<MoveInfo>>();
            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j++) {
                    var chOpt = board[i, j];
                    if (chOpt.IsNone()) continue;
                    var ch = chOpt.Peel();

                    if (ch.color != color) continue;

                    var pos = new Vector2Int(i, j);
                    var boardClone = (Option<Checker>[,])board.Clone();// убрать
                    allCheckersMoves.Add(
                        pos, GetCheckerMoves(
                            boardClone,
                            pos,
                            kind,
                            new List<Vector2Int>()
                        )
                    );
                }
            }

            return allCheckersMoves;
        }

        public static Dictionary<Vector2Int, List<MoveInfo>> AnalyseCheckerMoves(
            Dictionary<Vector2Int,List<MoveInfo>> paths
        ) {
            if (paths == null) {
                Debug.LogError("NoMovesDictionary");
                return null;
            }

            var isNeedAttack = false;
            var newMoves = new Dictionary<Vector2Int, List<MoveInfo>>();
            foreach (var path in paths) {
                foreach (var cell in path.Value) {
                    if (cell.isAttack) {
                        isNeedAttack = true;
                    }

                    if (isNeedAttack && cell.isAttack) {
                        //newMoves.Add(path.Key, );//do something
                    }
                }
            }

            if (isNeedAttack) {
                return newMoves;
            } else {
                return newMoves = paths;
            }
        }

        public static void Move(Option<Checker>[,] board, MoveInfo path, Vector2Int pos) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return;
            }

            var chOpt = board[pos.x, pos.y];
            if (chOpt.IsNone()) return;
            var ch = chOpt.Peel();

            board[path.cellPos.x, path.cellPos.y] = Option<Checker>.Some(ch);
            board[pos.x, pos.y] = Option<Checker>.None();
        }

        private static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }
            return true;
        }
    }
}