using UnityEngine;
using System.Collections.Generic;
using rules;
using option;
using movement;
using board;

namespace move {
    public enum MoveErrors {
        None,
        BoardIsNull,
        ListIsNull,
        CantGetLength,
        CantGetCheckersMovement,
        CantGetCellsAfterAttack
    }

    public struct MoveDate {
        public Vector2Int from;
        public Vector2Int to;

        public static MoveDate Mk(Vector2Int from, Vector2Int to) {
            return new MoveDate { from = from, to = to };
        }
    }

    public struct MoveInfo {
        public MoveDate moveDate;
        public bool secondMove;
        public Vector2Int? sentenced;

        public static MoveInfo Mk(MoveDate moveDate) {
            return new MoveInfo{ moveDate = moveDate };
        }
    }

    public static class Move {
        public static MoveErrors CheckerMove(Option<Checker>[,] board, MoveInfo info) {
            if (board == null) {
                return MoveErrors.None;
            }
            var moveDate = info.moveDate;
            board[moveDate.to.x, moveDate.to.y] = board[moveDate.from.x, moveDate.from.y];
            board[moveDate.from.x, moveDate.from.y] = Option<Checker>.None();
            return MoveErrors.None;
        }

        public static (List<MoveInfo>, MoveErrors) GetMoveInfos(
            Option<Checker>[,] board,
            List<CheckerMovement> checkerMovements
        ) {
            if (board == null) {
                return (null, MoveErrors.BoardIsNull);
            }
            if (checkerMovements == null) {
                return (null, MoveErrors.ListIsNull);
            }

            var moves = new List<Vector2Int>();
            var moveInfos = new List<MoveInfo>();
            var (isNeedAttack, err) = CheckNeedAttack(board, checkerMovements);

            foreach (var checkerMovement in checkerMovements) {
                var movement = checkerMovement.movement;
                var startPos = checkerMovement.movement.pos;
                var length = checkerMovement.movement.linear.length;
                Vector2Int? sentenced = null;

                var checkerOpt = board[movement.pos.x , movement.pos.y];
                if (checkerOpt.IsNone()) {
                    continue;
                }
                if (!isNeedAttack) {
                    moves = GetMoveCells(movement.linear.dir, startPos, length);
                }
                if (checkerMovement.type == MovementType.Attack && length != 0) {
                    startPos = movement.pos + movement.linear.dir * length;
                    var (cellsAfterAttack, err2) = GetCellsAfterAttack(
                        board,
                        startPos,
                        checkerOpt.Peel(),
                        checkerMovement
                    );
                    sentenced = startPos;
                    if (err2 != MoveErrors.None) {
                        return (null, MoveErrors.CantGetCellsAfterAttack);
                    }
                    moves = cellsAfterAttack;
                }
                foreach(var move in moves) {
                    var moveInfo = MoveInfo.Mk(MoveDate.Mk(movement.pos, move));
                    if (sentenced.HasValue) {
                        moveInfo.sentenced = sentenced;
                    }
                    moveInfos.Add(moveInfo);
                }
            }

            return (moveInfos, MoveErrors.None);
        }

        public static (bool, MoveErrors) CheckNeedAttack(
            Option<Checker>[,] board,
            List<CheckerMovement> checkerMovements
        ) {
            if (board == null) {
                return (false, MoveErrors.BoardIsNull);
            }

            foreach (var checkerMove in checkerMovements) {
                var movement = checkerMove.movement.linear;
                var pos = checkerMove.movement.pos;

                if (checkerMove.type == MovementType.Attack && movement.length != 0) {
                    var startPos = pos + movement.dir * movement.length;
                    var checkerOpt = board[pos.x, pos.y];
                    if (checkerOpt.IsNone()) {
                        return (false, MoveErrors.None);
                    }
                    var checker = checkerOpt.Peel();
                    var (cells, err) = GetCellsAfterAttack(board, startPos, checker, checkerMove);
                    if (err != MoveErrors.None) {
                        return (false, MoveErrors.CantGetCheckersMovement);
                    }
                    if (cells.Count != 0) {
                        return (true, MoveErrors.None);
                    }
                }
            }

            return (false, MoveErrors.None);
        }

        public static (List<Vector2Int>, MoveErrors) GetCellsAfterAttack(
            Option<Checker>[,] board,
            Vector2Int attackPos,
            Checker checker,
            CheckerMovement attackMovement
        ) {
            List<Vector2Int> moves = new List<Vector2Int>();
            var boardSize = new Vector2Int(board.GetLength(1) - 1, board.GetLength(0) - 1);
            var nextCell = attackPos + attackMovement.movement.linear.dir;
            if (Board.IsOnBoard(boardSize, nextCell) && board[nextCell.x, nextCell.y].IsNone()) {
                var (nextMovements, err) = Movement.GetCheckersMovement(board, attackPos, checker);
                if (err != MovementErrors.None) {
                    return (null, MoveErrors.CantGetCheckersMovement);
                }
                foreach (var movement in nextMovements) {
                    if (movement.movement.linear.dir == attackMovement.movement.linear.dir) {
                        var length = 1;
                        moves = GetMoveCells(movement.movement.linear.dir, attackPos, length);
                        break;
                    }
                }
            }

            return (moves, MoveErrors.None);
        }

        public static List<Vector2Int> GetMoveCells(Vector2Int dir, Vector2Int pos, int length) {
            var moveCells = new List<Vector2Int>();
            for (int i = 1; i <= length; i++) {
                var cell = pos + dir * i;
                moveCells.Add(cell);
            }

            return moveCells;
        }

    }
}