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

        private ControllerErrors GetCheckerMovements(Option<Checker>[,] board, Vector2Int pos) {
            var checkerOpt = board[pos.x, pos.y];
            if (checkerOpt.IsNone()) {
                return ControllerErrors.None;
            }
            var checker = checkerOpt.Peel();

            switch (checker.type) {
                case Type.Checker:
                    break;
                case Type.King:
                    break;
            }

            return ControllerErrors.None;
        }
        private void GetCheckerMovementByType(Func<int, int, bool> comparator) {
            for (int i = -1; i < 1; i++) {
                for (int j = -1; j < 2; j++) {
                    if (comparator(i,j)) {
                        
                    }
                }
            }
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