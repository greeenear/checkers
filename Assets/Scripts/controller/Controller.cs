using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;

namespace controller {
    public enum ControllerErrors {
        None,
        BoardIsNull,
        GameObjectIsNull,
        ListIsNull,
        CantRelocateChecker,
        CantGetMoves,
        CantGetCheckerMovements,
        CantGetLength,
        CantGetFixMovement,
        CantGetFixMovements,
        CantCheckGameStatus,
        CantCheckNeedAttack,
        NoSuchColor
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

    public enum Action {
        None,
        Select,
        Move
    }

    public struct Checker {
        public Type type;
        public Color color;
    }

    public enum MovemenType {
        Move,
        Attack
    }

    public class Controller : MonoBehaviour {
        private Resources res;

        public Transform cellPos;
        private Option<Checker>[,] board = new Option<Checker>[8, 8];
        private GameObject[,] boardObj = new GameObject[8, 8];
        private List<Vector2Int> moves = new List<Vector2Int>();
        private Vector2Int selectedChecker;
        private List<Vector2Int> attackPositions = new List<Vector2Int>();

        private Color whoseMove;
        private Action action;

        private void Awake() {
            res = gameObject.GetComponentInParent<Resources>();
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }
        }

        private void Start() {
            FillBoard(board);
            SpawnCheckers(board);
        }

        private void Update() {
            if (!Input.GetMouseButtonDown(0)) {
                return;
            }
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out hit, 100f)) {
                return;
            }

            var selectedPos = ConvertToBoardPoint(hit.point);
            var checkerOpt = board[selectedPos.x, selectedPos.y];
            foreach (Transform child in res.storageHighlightCells.transform) {
                Destroy(child.gameObject);
            }
            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                action = Action.Select;
                selectedChecker = selectedPos;
            }
            var checker = checkerOpt.Peel();

            var maxLength = 1;
            if (checker.type == Type.King) {
                var max = Mathf.Max(board.GetLength(1), board.GetLength(0));
                maxLength = max;
            }

            if (action == Action.Select) {
                moves.Clear();
                for (int i = -1; i <= 1; i++) {
                    for (int j = -1; j <= 1; j++) {
                        if (i == 0 || j == 0) {
                            continue;
                        }
                        var dir = new Vector2Int(i, j);
                        var (length, err) = GetLengthToObject(board, selectedPos, dir, maxLength);
                        if (err != ControllerErrors.None) {
                            Debug.LogError($"CantRelocateChecker {err.ToString()}");
                            return;
                        }

                        moves.AddRange(GetCells(selectedPos, dir, length));
                    }
                }
                HighlightCells(moves);
                action = Action.Move;

            } else if (action == Action.Move) {
                if (!IsPossibleMove(moves, selectedPos)) {
                    return;
                }

                MoveChecker(board, selectedChecker, selectedPos);
                whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                action = Action.None;
            }
        }

        public static List<Vector2Int> GetCells(Vector2Int pos, Vector2Int dir, int length) {
            var moveCells = new List<Vector2Int>();
            for (int i = 1; i <= length; i++) {
                var cell = pos + dir * i;
                moveCells.Add(cell);
            }

            return moveCells;
        }

        private (int, ControllerErrors) GetLengthToObject(
            Option<Checker>[,] board,
            Vector2Int pos,
            Vector2Int dir,
            int maxLength
        ) {
            int length = 0;
            var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));

            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (0, ControllerErrors.BoardIsNull);
            }
            for (int i = 1; i <= maxLength; i++) {
                var cell = pos + dir * i;

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

        private ControllerErrors MoveChecker(
            Option<Checker>[,] board,
            Vector2Int from,
            Vector2Int to
        ) {
            if (board == null) {
                return ControllerErrors.BoardIsNull;
            }
            board[to.x, to.y] = board[from.x, from.y];
            board[from.x, from.y] = Option<Checker>.None();
            RelocateChecker(boardObj, from, to);
            return ControllerErrors.None;
        }

        private (List<Vector2Int>, ControllerErrors) GetAttackPositions(
            Option<Checker>[,] board,
            Color color
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (null, ControllerErrors.BoardIsNull);
            }

            var checkerList = new List<Vector2Int>();
            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j++) {
                    if (board[i, j].IsSome() && board[i, j].Peel().color == color) {
                        var pos = new Vector2Int(i, j);
                    }
                }
            }

            return (checkerList, ControllerErrors.None);
        }

        private ControllerErrors RelocateChecker(
            GameObject[,] boardObj,
            Vector2Int from,
            Vector2Int to
        ) {
            if (boardObj == null) {
                Debug.LogError("BoardIsNull");
                return ControllerErrors.BoardIsNull;
            }
            var pos = ConvertToWorldPoint(to);
            boardObj[from.x, from.y].transform.position = pos;
            boardObj[to.x, to.y] = boardObj[from.x, from.y];

            return ControllerErrors.None;
        }

        private bool CheckPromotion(Vector2Int to, Color color, Vector2Int boardSize) {
            if (to.x == 0 && color == Color.White) {
                return true;
            }
            if (to.x == boardSize.x - 1 && color == Color.Black) {
                return true;
            }

            return false;
        }

        private ControllerErrors CheckerPromotion(Vector2Int pos, Color color) {
            board[pos.x, pos.y] = Option<Checker>.None();
            var king = new Checker {type = Type.King, color = color };
            board[pos.x, pos.y] = Option<Checker>.Some(king);

            var target = Quaternion.Euler(180, 0, 0);
            boardObj[pos.x, pos.y].transform.rotation = target;

            return ControllerErrors.None;
        }

        private (bool, ControllerErrors) IsGameOver(Option<Checker>[,] board, Color color) {
            var possibleMoves = new List<Vector2Int>();
            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j++) {
                    if (board[i, j].IsNone()) {
                        continue;
                    }
                    if (board[i, j].Peel().color != color) {
                        continue;
                    }
                }
            }
            if (possibleMoves.Count == 0) {
                return (true, ControllerErrors.None);
            } else {
                return (false, ControllerErrors.None);
            }
        }

        public static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }

            return true;
        }

        private ControllerErrors HighlightCells(List<Vector2Int> possibleMoves) {
            if (possibleMoves == null) {
                Debug.LogError("ListIsNull");
                return ControllerErrors.ListIsNull;
            }
            var boardPos = res.boardPos.transform.position;
            foreach (var pos in possibleMoves) {
                SpawnObject(res.highlightCell, pos, res.storageHighlightCells.transform);
            }

            return ControllerErrors.None;
        }

        private bool IsPossibleMove(List<Vector2Int> possibleMoves, Vector2Int selectPos) {
            foreach (var move in moves) {
                if (move == selectPos) {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int ConvertToBoardPoint(Vector3 selectedPoint) {
            var inversePoint = res.boardPos.InverseTransformPoint(selectedPoint);
            var cellLoc = cellPos.localPosition;
            var cellSize = res.cellSize.localScale;
            var floatVec = (inversePoint + new Vector3(-cellLoc.x, 0, cellLoc.z)) / cellSize.x;
            var point = new Vector2Int(Mathf.Abs((int)(floatVec.z)), Mathf.Abs((int)floatVec.x));

            return point;
        }

        private Vector3 ConvertToWorldPoint(Vector2Int boardPoint) {
            var offset = res.cellSize.localScale / 2f;
            var floatVec = new Vector3(boardPoint.x, 0.4f, boardPoint.y);
            var cellLoc = this.cellPos.localPosition;
            var cellSize = res.cellSize.localScale;
            var point = cellSize.x * floatVec - new Vector3(cellLoc.x, 0, cellLoc.z) + offset;

            return point;
        }

        private ControllerErrors SpawnCheckers(Option<Checker>[,] board) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return ControllerErrors.BoardIsNull;
            }
            for (int i = 0; i < board.GetLength(0); i++) {
                for (int j = 0; j < board.GetLength(1); j++) {
                    if (board[i, j].IsSome()) {
                        var checker = board[i, j].Peel();
                        GameObject prefab;
                        if (checker.color == Color.White) {
                            prefab = res.whiteChecker;
                        } else if (checker.color == Color.Black) {
                            prefab = res.blackChecker;
                        } else {
                            Debug.LogError("NoSuchColor");
                            return ControllerErrors.NoSuchColor;
                        }
                        var pos = new Vector2Int(i, j);
                        boardObj[i, j] = SpawnObject(prefab, pos, res.boardPos.transform);
                    }
                }
            }

            return ControllerErrors.None;
        }

        private GameObject SpawnObject(
            GameObject prefab,
            Vector2Int spawnPos,
            Transform parent
        ) {
            var spawnWorldPos = ConvertToWorldPoint(spawnPos);
            return Instantiate(prefab, spawnWorldPos, Quaternion.identity, parent);
        }

        public ControllerErrors FillBoard(Option<Checker>[,] board) {
            if (board == null) {
                Debug.LogError($"BoardIsNull");
                return ControllerErrors.BoardIsNull;
            }
            var color = Color.Black;
            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j = j + 2) {
                    if (i == 3 || i == 4) {
                        color = Color.White;
                        break;
                    }

                    if (i % 2 == 0) {
                        board[i, j + 1] = Option<Checker>.Some(new Checker { color = color});
                    } else {
                        board[i, j] = Option<Checker>.Some(new Checker { color = color });
                    }
                }
            }

            return ControllerErrors.None;
        }
    }
}