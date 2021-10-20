using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;

namespace controller {
    enum ControllerErrors {
        None,
        BoardIsNull,
        GameObjectIsNull,
        ListIsNull,
        CantRelocateChecker
    }

    public enum Type {
        Checker,
        King
    }

    public enum Color {
        White,
        Black
    }

    enum Action {
        None,
        Select,
        Move
    }

    public struct Move {
        public Vector2Int from;
        public Vector2Int to;
    }

    public struct Checker {
        public Type type;
        public Color color;
    }

    public struct Linear {
        public Vector2Int dir;
        public Vector2Int start;
        public int length;
    }

    public enum MovemenType {
        Move,
        Attack
    }

    public class Controller : MonoBehaviour {
        private Resources res;
        private Option<Checker>[,] board = new Option<Checker>[8, 8];
        private GameObject[,] boardObj = new GameObject[8, 8];
        private Action action;

        private void Start() {
            res = gameObject.GetComponent<Resources>();
            FillingBoard(board);
            CheckerSpawner(board);
        }

        private void Update() {
            if (!Input.GetMouseButtonDown(0)) {
                return;
            }
            Ray ray;
            RaycastHit hit;
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out hit, 100f)) {
                return;
            }
            var offset = res.cellSize.lossyScale * res.boardSize.x / 2;
            var selectedPosFloat = hit.point - (transform.position - offset);
            var selectedPos = new Vector2Int((int)selectedPosFloat.x, (int)selectedPosFloat.z);
            var pieceOpt = board[selectedPos.x, selectedPos.y];
            if (action == Action.None && pieceOpt.IsSome()) {
                action = Action.Select;
            }

            switch (action) {
                case Action.Select:
                    DestroyChildrens(res.storageHighlightCells.transform);
                    moves.Clear();
                    var (newMoves, getMovesErr) = GetMoves(board, selectedPos);
                    if (getMovesErr !=  ControllerErrors.None) {
                        Debug.LogError("CantGetMoves");
                        return;
                    }
                    moves = newMoves;
                    action = Action.Move;
                    HighlightCells(moves);
                    break;
                case Action.Move:
                    var move = СompareMoveInfo(moves, selectedPos);
                    if (!move.HasValue) {
                        action = Action.None;
                        DestroyChildrens(res.storageHighlightCells.transform);
                        moves.Clear();
                        break;
                    }
                    Move(move.Value);
                    DestroyChildrens(res.storageHighlightCells.transform);
                    moves.Clear();
                    action = Action.None;
                    break;
            }
        }

        private ControllerErrors Move(Move move) {
            var err = RelocateChecker(move, boardObj);
            if (err != ControllerErrors.None) {
                return ControllerErrors.CantRelocateChecker;
            }
            board[move.to.x, move.to.y] = board[move.from.x, move.from.y];
            board[move.from.x, move.from.y] = Option<Checker>.None();

            return ControllerErrors.None;
        }

        private ControllerErrors RelocateChecker(Move move, GameObject[,] boardObj) {
            if (boardObj == null) {
                return ControllerErrors.BoardIsNull;
            }
            var from = move.from;
            var to = move.to;
            var boardPos = gameObject.transform.position;
            var offset = res.boardSize.x / 2 + res.cellSize.lossyScale.x / 2;
            boardObj[from.x, from.y].transform.position = new Vector3(
                to.x + boardPos.x - res.cellSize.lossyScale.x * offset,
                boardPos.y + res.cellSize.lossyScale.x / 2,
                to.y + boardPos.z - res.cellSize.lossyScale.x * offset
            );
            boardObj[to.x, to.y] = boardObj[from.x, from.y];

            return ControllerErrors.None;
        }

        private ControllerErrors CheckerSpawner(Option<Checker>[,] board) {
            if (board == null) {
                return ControllerErrors.BoardIsNull;
            }
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    if (board[i, j].IsSome()) {
                        var checker = board[i, j].Peel();
                        GameObject obj;
                        if (checker.color == Color.White) {
                            obj = res.whiteChecker;
                        } else {
                            obj = res.blackChecker;
                        }
                        var pos = new Vector2Int(i, j);
                        boardObj[i, j] = ObjectSpawner(obj, pos, gameObject.transform);
                    }
                }
            }

            return ControllerErrors.None;
        }

        private (List<CheckerMovement>, ControllerErrors) GetCheckerMovements(
            Option<Checker>[,] board,
            Vector2Int pos
        ) {
            var checkerOpt = board[pos.x, pos.y];
            var move = MovemenType.Move;
            var attack = MovemenType.Attack;
            List<CheckerMovement> movements = new List<CheckerMovement>();
            if (checkerOpt.IsNone()) {
                return (movements, ControllerErrors.None);
            }
            var checker = checkerOpt.Peel();
            switch (checker.type) {
                case Type.Checker:
                    if (checkerOpt.Peel().color == Color.Black) {
                        movements.AddRange(GetMovements(pos, 1, move, (i, j) => i > 0));
                        movements.AddRange(GetMovements(pos, 1, attack, (i, j) => true));
                    }
                    if (checkerOpt.Peel().color == Color.White) {
                        movements.AddRange(GetMovements(pos, 1, move, (i, j) => i < 0));
                        movements.AddRange(GetMovements(pos, 1, attack, (i, j) => true));
                    }
                    break;
                case Type.King:
                    if (checkerOpt.Peel().color == Color.Black) {
                        movements.AddRange(GetMovements(pos, 8, move, (i, j) => true));
                        movements.AddRange(GetMovements(pos, 8, attack, (i, j) => true));
                    }
                    if (checkerOpt.Peel().color == Color.White) {
                        movements.AddRange(GetMovements(pos, 8, move, (i, j) => true));
                        movements.AddRange(GetMovements(pos, 8, attack, (i, j) => true));
                    }
                    break;
            }

            return (movements, ControllerErrors.None);
        }

        private List<CheckerMovement> GetMovements(
            Vector2Int start,
            int length,
            MovemenType type,
            Func<int, int, bool> comparator
        ) {
            List<CheckerMovement> movements = new List<CheckerMovement>();
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i != 0 && j != 0 && comparator(i, j)) {
                        var dir = new Vector2Int(i, j);
                        movements.Add(CheckerMovement.Mk(Linear.Mk(dir, start, length), type));
                    }
                }
            }

            return movements;
        }

        private (List<Move>, ControllerErrors) GetMoves(Option<Checker>[,] board, Vector2Int pos) {
            var (checkerMovements, movementsErr) = GetCheckerMovements(board, pos);
            if (board == null) {
                return(null, ControllerErrors.BoardIsNull);
            }
            var checkerOpt = board[pos.x, pos.y];
            List<Move> moves = new List<Move>();
            if (checkerOpt.IsNone()) {
                return (moves, ControllerErrors.None);
            }
            var checker = checkerOpt.Peel();
            if (movementsErr != ControllerErrors.None) {
                return (null, ControllerErrors.CantGetCheckerMovements);
            }

            var fixCheckerMovements = new List<CheckerMovement>();
            foreach (var movement in checkerMovements) {
                var fixMovement = movement;
                var (length, getLengthErr) = GetLength(board, movement.linear);
                if (getLengthErr != ControllerErrors.None) {
                    return (moves, ControllerErrors.CantGetLength);
                }
                fixMovement.linear.length = length;
                var err = ControllerErrors.None;
                var maxLength = movement.linear.length;
                (fixMovement, err) = GetFixMovement(board, fixMovement, checker.color, maxLength);
                if (err != ControllerErrors.None) {
                    return (moves, ControllerErrors.CantGetFixMovement);
                }
                fixCheckerMovements.Add(fixMovement);
            }

            foreach (var fixMovement in fixCheckerMovements) {
                foreach (var cell in GetMoveCells(fixMovement.linear)) {
                    moves.Add(controller.Move.Mk(pos, cell));
                }
            }

            return (moves, ControllerErrors.None);
        }

        public static List<Vector2Int> GetMoveCells(Linear linear) {
            var moveCells = new List<Vector2Int>();
            for (int i = 1; i <= linear.length; i++) {
                var cell = linear.start + linear.dir * i;
                moveCells.Add(cell);
            }
            return moveCells;
        }

        private (CheckerMovement, ControllerErrors) GetFixMovement(
            Option<Checker>[,] board,
            CheckerMovement movement,
            Color color,
            int maxLength
        ) {
            var lastCell = movement.linear.start + movement.linear.dir * movement.linear.length;
            if (movement.type == MovemenType.Move) {
                if (board[lastCell.x, lastCell.y].IsSome()) {
                    movement.linear.length--;
                }
            }
            if (movement.type == MovemenType.Attack) {
                if (board[lastCell.x, lastCell.y].IsSome()) {
                    movement.linear.length = 0;
                    if (board[lastCell.x, lastCell.y].Peel().color != color) {
                        movement.linear.start = lastCell;
                        var newLinear = movement.linear;
                        newLinear.length = maxLength;
                        var (length, getLengthErr) = GetLength(board, newLinear);
                        lastCell = lastCell + movement.linear.dir * length;
                        if (board[lastCell.x, lastCell.y].IsSome()) {
                            length--;
                        }
                        movement.linear.length = length;
                    }
                } else {
                    movement.linear.length = 0;
                }
            }

            return (movement, ControllerErrors.None);
        }

        private (int, ControllerErrors) GetLength(Option<Checker>[,] board, Linear linear) {
            int length = 0;
            var boardSize = new Vector2Int(board.GetLength(1) - 1, board.GetLength(0) - 1);

            if (board == null) {
                return (0, ControllerErrors.BoardIsNull);
            }
            for (int i = 1; i <= linear.length; i++) {
                var cell = linear.start + linear.dir * i;

                if (!IsOnBoard(boardSize, cell)) {
                    break;
                }
                length++;
                if (board[cell.x, cell.y].IsSome()) {
                    break;
                }
            }

            return (length, ControllerErrors.None);
        }

        public static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x > boardSize.x || pos.y < 0 || pos.y > boardSize.y) {
                return false;
            }
            return true;
        }

        private void DestroyChildrens(Transform parent) {
            foreach (Transform child in parent) {
                Destroy(child.gameObject);
            }
        }

        private ControllerErrors HighlightCells(List<Move> possibleMoves) {
            if (possibleMoves == null) {
                return ControllerErrors.ListIsNull;
            }
            var boardPos = gameObject.transform.position;
            foreach (var pos in possibleMoves) {
                ObjectSpawner(res.highlightCell, pos.to, res.storageHighlightCells.transform);
            }

            return ControllerErrors.None;
        }
        private GameObject ObjectSpawner(
            GameObject gameObject,
            Vector2Int spawnPos,
            Transform parentTransform
        ) {
            var boardPos = transform.position;
            var cellLossyScale = res.cellSize.lossyScale.x;
            var halfBoardSize = res.boardSize.x / 2;
            var spawnWorldPos = new Vector3(
                spawnPos.x + boardPos.x - cellLossyScale * halfBoardSize + cellLossyScale / 2,
                boardPos.y + cellLossyScale / 2,
                spawnPos.y + boardPos.z - cellLossyScale * halfBoardSize + cellLossyScale / 2
            );
            return Instantiate(gameObject, spawnWorldPos, Quaternion.identity, parentTransform);
        }

        public void FillingBoard(Option<Checker>[,] board) {
            FillingLine(board, 0, 1, 1, Color.Black);
            FillingLine(board, 1, 0, 1, Color.Black);
            FillingLine(board, 2, 1, 1, Color.Black);
            FillingLine(board, 5, 0, 1, Color.White);
            FillingLine(board, 6, 1, 1, Color.White);
            FillingLine(board, 7, 0, 1, Color.White);
        }

        private void FillingLine(Option<Checker>[,] board, int x, int start, int skip, Color с) {
            for (int i = start; i < board.GetLength(1); i = i + skip + 1) {
                board[x, i] = Option<Checker>.Some(new Checker { color = с });
            }
        }
    }
}