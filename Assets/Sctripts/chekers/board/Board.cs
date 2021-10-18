using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;

namespace board {
    public enum BoardErrors {
        None,
        BoardIsNull
    }

    public struct Linear {
        public Vector2Int dir;
        public int length;

        public static Linear Mk(Vector2Int dir, int length) {
            return new Linear { dir = dir, length = length };
        }
    }

    public struct FixedMovement {
        public Linear linear;
        public Vector2Int pos;

        public static FixedMovement Mk(Linear linear, Vector2Int pos) {
            return new FixedMovement { linear = linear, pos = pos };
        }
    }

    public static class Board {
        public static (int, BoardErrors) GetLength<T>(
            Option<T>[,] board,
            Linear linear,
            Vector2Int pos
        ) {
            if (board == null) {
                return (0, BoardErrors.BoardIsNull);
            }
            int length = 0;
            var boardSize = new Vector2Int (board.GetLength(1), board.GetLength(0));
            for (int i = 1; i <= linear.length; i++) {
                var cell = pos + linear.dir * i;
                if (!IsOnBoard(boardSize, cell)) {
                    break;
                }
                length ++;
                if (board[cell.x, cell.y].IsSome()) {
                    break;
                }
            }
            return (length, BoardErrors.None);
        }

        public static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x > boardSize.x || pos.y < 0 || pos.y > boardSize.y) {
                return false;
            }
            return true;
        }
    }

}
