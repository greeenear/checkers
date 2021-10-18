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
            FillingBoard();
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
                    HighlightCells(moveInfos);
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

        private void Move(MoveInfo moveInfo) {
            if (moveInfo.sentenced.HasValue) {
                var sentencedPos = moveInfo.sentenced.Value;
                Destroy(boardObj[sentencedPos.x, sentencedPos.y]);
                board[sentencedPos.x, sentencedPos.y] = Option<Checker>.None();
            }
            move.Move.CheckerMove(board, moveInfo);
            RelocateChecker(moveInfo);
        }

        private void RelocateChecker(MoveInfo move) {
            var from = move.moveDate.from;
            var to = move.moveDate.to;
            var boardPos = gameObject.transform.position;
            boardObj[from.x, from.y].transform.position = new Vector3(
                to.x + boardPos.x - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2,
                boardPos.y + cellSize.lossyScale.x / 2,
                to.y + boardPos.z - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2
            );
            boardObj[to.x, to.y] = boardObj[from.x, from.y];
        }

        private void CheckerSpawner(Option<Checker>[,] board) {
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

        private void HighlightCells(List<MoveInfo> possibleMoves) {
            var parentTransform = storageHighlightCells.transform;
            var boardPos = gameObject.transform.position;
            foreach (var pos in possibleMoves) {
                ObjectSpawner(highlightCell, pos.moveDate.to, parentTransform);
            }
        }

        private void FillingBoard() {
            board[0, 0] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[0, 2] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[0, 4] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[0, 6] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 1] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 3] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 5] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 7] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 0] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 2] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 4] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 6] = Option<Checker>.Some(new Checker { color = rules.Color.Black });

            board[5, 7] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[5, 5] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[5, 3] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[5, 1] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 6] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 4] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 2] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 0] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 7] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 5] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 3] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 1] = Option<Checker>.Some(new Checker { color = rules.Color.White });
        }
    }
}