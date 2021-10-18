using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;
using board;

namespace rules {
    public enum RulesErrors {
        None,
        BoardIsNull
    }

    public struct Checker {
        public Color color;
        public Type type;
    }

    public enum Type {
        Checker,
        King
    }

    public enum Color {
        White,
        Black,
        Count
    }

    public enum MovementType {
        Move,
        Attack
    }

    public struct CheckerMovement {
        public FixedMovement movement;
        public MovementType type;

        public static CheckerMovement MkFull(
            Vector2Int dir,
            Vector2Int pos,
            MovementType type,
            int length
        ) {
            var linear = Linear.Mk(dir, length);
            var fixedMovement = FixedMovement.Mk(linear, pos);
            return new CheckerMovement { movement = fixedMovement, type = type };
        }
    }

    public static class Rules {
        public static (int, RulesErrors) GetFixedLength(
            Option<Checker>[,] board,
            FixedMovement movement,
            MovementType type
        ) {
            if (board == null) {
                return (0, RulesErrors.BoardIsNull);
            }
            var lastPos = movement.pos + movement.linear.dir * movement.linear.length;
            var optChecker = board[movement.pos.x, movement.pos.y];
            if (optChecker.IsNone()) {
                return (0, RulesErrors.None);
            }
            var checker = optChecker.Peel();
            var last = board[lastPos.x, lastPos.y];
            var length = movement.linear.length;
            if (length == 0) {
                return (length, RulesErrors.None);
            }
            if (type == MovementType.Attack) {
                if (board[lastPos.x, lastPos.y].IsSome()) {
                    if (checker.color == last.Peel().color) {
                        length = 0;
                    }
                }
                if (last.IsNone()) {
                    length--;
                }
            } else if (type == MovementType.Move) {
                if (last.IsSome()) {
                    length--;
                }
            }

            return (length, RulesErrors.None);
        }
    }
}
