using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;
using checkers;
using movement;
using rules;
using move;

namespace controller {
    enum ControllerErrors {
        None,
        BoardIsNull,
        GameObjectIsNull,
        ListIsNull,
        CantRelocateChecker
    }
    enum Action {
        None,
        Select,
        Move
    }

    public class Controller : MonoBehaviour {
        public Transform cellSize;
        public GameObject whiteChecker;
        public GameObject blackChecker;
        private Option<Checker>[,] board = new Option<Checker>[8, 8];
        private GameObject[,] boardObj = new GameObject[8, 8];
        private List<MoveInfo> moveInfos = new List<MoveInfo>();
        private rules.Color whoseMove;
        private Action action;
        public GameObject storageHighlightCells;
        public GameObject highlightCell;

        private void Start() {
            Checkers.FillingBoard(board);
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
            var selectedPosFloat = hit.point - (transform.position - cellSize.lossyScale * 4);
            var selectedPos = new Vector2Int((int)selectedPosFloat.x, (int)selectedPosFloat.z);
            var pieceOpt = board[selectedPos.x, selectedPos.y];
            if (action == Action.None && pieceOpt.IsSome() && pieceOpt.Peel().color == whoseMove) {
                action = Action.Select;
            }

            switch (action) {
                case Action.Select:
                    DestroyChildrens(storageHighlightCells.transform);
                    moveInfos.Clear();
                    var (checkerMovement, err) = Movement.GetCheckersMovement(
                        board,
                        selectedPos,
                        pieceOpt.Peel()
                    );
                    if (err != MovementErrors.None) {
                        Debug.LogError("CantGetCheckersMovement");
                        return;
                    }
                    var (moves, getMovesErr) = move.Move.GetMoveInfos(board, checkerMovement);
                    if (getMovesErr != MoveErrors.None) {
                        Debug.LogError("CantGetMoves");
                        return;
                    }
                    moveInfos = moves;
                    var highlightCellsErr = HighlightCells(moveInfos);
                    if (highlightCellsErr != ControllerErrors.None) {
                        Debug.LogError("CantHighlightCells");
                        return;
                    }
                    action = Action.Move;
                    break;
                case Action.Move:
                    var moveInfo = СompareMoveInfo(moveInfos, selectedPos);
                    if (!moveInfo.HasValue) {
                        action = Action.None;
                        DestroyChildrens(storageHighlightCells.transform);
                        moveInfos.Clear();
                        break;
                    }
                    Move(moveInfo.Value);
                    DestroyChildrens(storageHighlightCells.transform);
                    moveInfos.Clear();
                    action = Action.None;
                    whoseMove = (rules.Color)((int)(whoseMove + 1) % (int)rules.Color.Count);
                    break;
            }
        }

        private ControllerErrors Move(MoveInfo moveInfo) {
            if (moveInfo.sentenced.HasValue) {
                var sentencedPos = moveInfo.sentenced.Value;
                Destroy(boardObj[sentencedPos.x, sentencedPos.y]);
                board[sentencedPos.x, sentencedPos.y] = Option<Checker>.None();
            }
            move.Move.CheckerMove(board, moveInfo);
            var err = RelocateChecker(moveInfo, boardObj);
            if (err != ControllerErrors.None) {
                return ControllerErrors.CantRelocateChecker;
            }

            return ControllerErrors.None;
        }

        private ControllerErrors RelocateChecker(MoveInfo move, GameObject[,] boardObj) {
            if (boardObj == null) {
                return ControllerErrors.BoardIsNull;
            }
            var from = move.moveDate.from;
            var to = move.moveDate.to;
            var boardPos = gameObject.transform.position;
            boardObj[from.x, from.y].transform.position = new Vector3(
                to.x + boardPos.x - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2,
                boardPos.y + cellSize.lossyScale.x / 2,
                to.y + boardPos.z - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2
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
                        if (checker.color == rules.Color.White) {
                            obj = whiteChecker;
                        } else {
                            obj = blackChecker;
                        }
                        var pos = new Vector2Int(i, j);
                        boardObj[i, j] = ObjectSpawner(obj, pos, gameObject.transform);
                    }
                }
            }

            return ControllerErrors.None;
        }


        private void DestroyChildrens(Transform parent) {
            foreach (Transform child in parent) {
                Destroy(child.gameObject);
            }
        }

        private GameObject ObjectSpawner(
            GameObject gameObject,
            Vector2Int spawnPos,
            Transform parentTransform
        ) {
            var boardPos = transform.position;

            var spawnWorldPos = new Vector3(
                spawnPos.x + boardPos.x - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2,
                boardPos.y + cellSize.lossyScale.x / 2,
                spawnPos.y + boardPos.z - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2
            );
            return Instantiate(gameObject, spawnWorldPos, Quaternion.identity, parentTransform);
        }

        private MoveInfo? СompareMoveInfo(List<MoveInfo> moveInfos, Vector2Int selectPos) {
            foreach (var info in moveInfos) {
                if (info.moveDate.to == selectPos) {
                    return info;
                }
            }
            return null;
        }

        private ControllerErrors HighlightCells(List<MoveInfo> possibleMoves) {
            if (possibleMoves == null) {
                return ControllerErrors.ListIsNull;
            }
            var parentTransform = storageHighlightCells.transform;
            var boardPos = gameObject.transform.position;
            foreach (var pos in possibleMoves) {
                ObjectSpawner(highlightCell, pos.moveDate.to, parentTransform);
            }

            return ControllerErrors.None;
        }
    }
}