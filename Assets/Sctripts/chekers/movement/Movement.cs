using System.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using option;
using rules;
using board;

namespace movement {
    public enum MovementErrors {
        None,
        BoardIsNull,
        CheckerIsNone,
        CantGetLength, 
        CantGetMovements
    }


    public static class Movement {
        public static (List<CheckerMovement>, MovementErrors) GetCheckersMovement(
            Option<Checker>[,] board,
            Vector2Int pos,
            rules.Checker checker
        ) {
            if (board == null) {
                return (null, MovementErrors.BoardIsNull);
            }
            List<CheckerMovement> checkerMovements = new List<CheckerMovement>();
            switch (checker.type) {
                case rules.Type.Checker:
                    int dir = 1;
                    if (checker.color == rules.Color.White) {
                        dir = -1;
                    }
                    var (movements, err) = GetMovementsByType(pos, 1, board, (i) => i == dir);
                    if (err != MovementErrors.None) {
                        return (checkerMovements, MovementErrors.CantGetMovements);
                    }
                    checkerMovements.AddRange(movements);
                    break;
                case rules.Type.King:
                    (movements, err) = GetMovementsByType(pos, 8,board, (i) => true);
                    if (err != MovementErrors.None) {
                        return (checkerMovements, MovementErrors.CantGetMovements);
                    }
                    checkerMovements.AddRange(movements);
                    break;
            }

            return (checkerMovements, MovementErrors.None);
        }

        public static (List<CheckerMovement>, MovementErrors) GetMovementsByType(
            Vector2Int pos,
            int startLength,
            Option<Checker>[,] board,
            Func<int, bool> comparator
        ) {
            var move = MovementType.Move;
            var attack = MovementType.Attack;
            List<CheckerMovement> movements = new List<CheckerMovement>();
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i ==0 || j == 0) {
                        continue;
                    }
                    var dir = new Vector2Int(i, j);
                    var (length, err2) = Board.GetLength(board, Linear.Mk(dir, startLength), pos);
                    if (err2 != BoardErrors.None) {
                        return (null, MovementErrors.CantGetLength);
                    }

                    var fixMovement = FixedMovement.Mk(Linear.Mk(dir, length), pos);
                    var (fixLength, err3) = Rules.GetFixedLength(board, fixMovement, attack);
                    if (err3 != RulesErrors.None) {
                        return (null, MovementErrors.CantGetLength);
                    }
                    movements.Add(CheckerMovement.MkFull(dir, pos, attack, fixLength));
                    if (comparator(i)){
                        (fixLength, err3) = Rules.GetFixedLength(board, fixMovement, move);
                        if (err3 != RulesErrors.None) {
                            return (null, MovementErrors.CantGetLength);
                        }
                        movements.Add(CheckerMovement.MkFull(dir, pos, move, fixLength));
                    }
                }
            }
            return (movements, MovementErrors.None);
        }
    }
}
