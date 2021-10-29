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
        Move,
        ChangeMove
    }

    public struct Checker {
        public Type type;
        public Color color;
    }

    public struct CheckerMoves {
        public bool isNeedAttack;
        public List<Vector2Int> moves;
    }

    public class Controller : MonoBehaviour {
        private Resources res;

        private bool isNeedAttack;
        private Option<Checker>[,] board = new Option<Checker>[8, 8];
        private GameObject[,] boardObj = new GameObject[8, 8];
        private Dictionary<Vector2Int, CheckerMoves> checkerMoves;
        private List<Vector2Int> sentenced = new List<Vector2Int>();
        private Vector2Int selectedChecker;
        private JsonObject jsonObject;
        private Color whoseMove;
        private Action action;

        private void Awake() {
            res = gameObject.GetComponentInParent<Resources>();
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }
            if (res.blackChecker == null) {
                Debug.LogError("NoCheckers");
                this.enabled = false;
                return;
            }
            if (res.whiteChecker == null) {
                Debug.LogError("NoCheckers");
                this.enabled = false;
                return;
            }
            if (res.cellTransform == null) {
                Debug.LogError("NoCellSize");
                this.enabled = false;
                return;
            }
            if (res.storageHighlightCells == null) {
                Debug.LogError("NoStorageHighlightCells");
                this.enabled = false;
                return;
            }
            if (res.highlightCell == null) {
                Debug.LogError("NoHighlightCells");
                this.enabled = false;
                return;
            }
            if (res.boardTransform == null) {
                Debug.LogError("NoBoardPos");
                this.enabled = false;
                return;
            }
            if (res.boardSize == null) {
                Debug.LogError("NoBoardSize");
                this.enabled = false;
                return;
            }
            if (res.gameMenu == null) {
                Debug.LogError("NoGameMenu");
                this.enabled = false;
                return;
            }
            if (res.directions == null) {
                Debug.LogError("NoDirectionsList");
                this.enabled = false;
                return;
            }
        }

        private void Start() {
            FillBoard(board);
            SpawnCheckers(board);
        }

        private void Update() {
            if (checkerMoves == null) {
                checkerMoves = new Dictionary<Vector2Int, CheckerMoves>();
                var maxLength = 1;
                var xDir = 1;
                for (int i = 0; i < board.GetLength(0); i++) {
                    for (int j = 0; j < board.GetLength(1); j++) {
                        var nextCell = new Vector2Int();
                        var cellOpt = board[i, j];
                        if (cellOpt.IsNone() || cellOpt.Peel().color != whoseMove) {
                            continue;
                        }

                        var cell = cellOpt.Peel();
                        if (cell.type == Type.King) {
                            var max = Mathf.Max(res.boardSize.x, res.boardSize.y);
                            maxLength = max;
                        } else if (cell.type == Type.Checker && cell.color == Color.White) {
                            xDir = -1;
                        }

                        var pos = new Vector2Int(i, j);
                        var checkerMoves = new CheckerMoves();
                        var attackCells = new List<Vector2Int>();
                        var moveCells = new List<Vector2Int>();
                        foreach (var dir in res.directions) {
                            var length = 0;
                            for (int k = 1; k <= maxLength; k++) {
                                nextCell = pos + dir * k;
                                if (!IsOnBoard(res.boardSize, nextCell)) {
                                    break;
                                }
                                length++;
                                if (board[nextCell.x, nextCell.y].IsSome()) {
                                    break;
                                }
                            }
                            if (length == 0) {
                                continue;
                            }

                            var lastCell = pos + dir * length;
                            var lastCellOpt = board[lastCell.x, lastCell.y];
                            if (lastCellOpt.IsNone()) {
                                if (cell.type == Type.Checker && dir.x != xDir) {
                                    continue;
                                }
                                moveCells.AddRange(GetCells(pos, dir, length));
                            }

                            if ((lastCellOpt.IsSome() && lastCellOpt.Peel().color != cell.color)) {
                                length = 0;
                                for (int k = 1; k <= maxLength; k++) {
                                    nextCell = lastCell + dir * k;
                                    if (!IsOnBoard(res.boardSize, nextCell)) {
                                        break;
                                    }
                                    if (board[nextCell.x, nextCell.y].IsSome()) {
                                        break;
                                    }
                                    length++;
                                }

                                if (length != 0 && !sentenced.Contains(lastCell)) {
                                    checkerMoves.isNeedAttack = true;
                                    isNeedAttack = true;
                                    attackCells.AddRange(GetCells(lastCell, dir, length));
                                }
                            }
                        }

                        if (attackCells.Count != 0) {
                            checkerMoves.moves = attackCells;
                        } else {
                            checkerMoves.moves = moveCells;
                        }
                        this.checkerMoves.Add(pos, checkerMoves);
                    }
                }
            }

            if (!Input.GetMouseButtonDown(0)) {
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) {
                return;
            }

            DestroyHighlightCells(res.storageHighlightCells.transform);
            var targetPos = ConvertToBoardPoint(hit.point);
            var checkerOpt = board[targetPos.x, targetPos.y];
            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                action = Action.Select;
                selectedChecker = targetPos;
            }
            var checker = checkerOpt.Peel();

            if (action == Action.Select) {
                var currentInfo = checkerMoves[targetPos];
                if (isNeedAttack && !currentInfo.isNeedAttack) {
                    action = Action.None;
                    return;
                }
                HighlightCells(currentInfo.moves);
                action = Action.Move;
            } else if(action == Action.Move) {
                var currentInfo = checkerMoves[selectedChecker];
                if (!IsPossibleMove(currentInfo.moves, targetPos)) {
                    return;
                }

                var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));

                var secondMove = false;
                Vector2Int? sentenced = null;
                var dif = targetPos - selectedChecker;
                var dir = new Vector2Int(dif.x / Mathf.Abs(dif.x), dif.y / Mathf.Abs(dif.y));
                board[targetPos.x, targetPos.y] = board[selectedChecker.x, selectedChecker.y];
                board[selectedChecker.x, selectedChecker.y] = Option<Checker>.None();
                for (int i = 1; i < Mathf.Abs(dif.x); i++) {
                    var cell = selectedChecker + dir * i;
                    if (board[cell.x, cell.y].IsSome()) {
                        sentenced = cell;
                        this.sentenced.Add(cell);

                        foreach (var moveDir in res.directions) {
                            cell = targetPos + moveDir;
                            if (this.sentenced.Contains(cell)) {
                                continue;
                            }
                            if (IsOnBoard(boardSize, cell) && board[cell.x, cell.y].IsSome()) {
                                var enemyСhecker = board[cell.x, cell.y].Peel();
                                var currentChecker = board[targetPos.x, targetPos.y];
                                if (enemyСhecker.color != currentChecker.Peel().color) {
                                    cell = cell + moveDir;
                                    var isOnboard = IsOnBoard(boardSize, cell) ;
                                    if (isOnboard && board[cell.x, cell.y].IsNone()) {
                                        secondMove = true;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }

                RelocateChecker(boardObj, selectedChecker, targetPos, sentenced);
                if (CheckPromotion(targetPos, whoseMove, res.boardSize)) {
                    CheckerPromotion(targetPos, whoseMove);
                }

                if (sentenced.HasValue) {
                    this.sentenced.Add(sentenced.Value);
                }
                if (secondMove) {
                    selectedChecker = targetPos;
                    action = Action.Move;
                    checkerMoves = null;
                    return;
                }
                foreach (var pos in this.sentenced) {
                    board[pos.x, pos.y] = Option<Checker>.None();
                    Destroy(boardObj[pos.x, pos.y]);
                }
                this.sentenced.Clear();
                action = Action.ChangeMove;
            }

            if (action == Action.ChangeMove) {
                IsGameOver(board, whoseMove);
                whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                checkerMoves = null;
                isNeedAttack = false;
                action = Action.None;
            }
        }

        public List<Vector2Int> GetCells(Vector2Int pos, Vector2Int dir, int length) {
            var moveCells = new List<Vector2Int>();
            for (int i = 1; i <= length; i++) {
                var cell = pos + dir * i;
                moveCells.Add(cell);
            }

            return moveCells;
        }

        private ControllerErrors RelocateChecker(
            GameObject[,] boardObj,
            Vector2Int from,
            Vector2Int to,
            Vector2Int? sentensed
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

                    var checker = board[i, j].Peel();
                    if (checker.color != color) {
                        continue;
                    }
                    var maxLength = 1;
                    if (checker.type == Type.King) {
                        maxLength = Mathf.Max(board.GetLength(1), board.GetLength(0));
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
            var boardPos = res.boardTransform.transform.position;
            foreach (var pos in possibleMoves) {
                SpawnObject(res.highlightCell, pos, res.storageHighlightCells.transform);
            }

            return ControllerErrors.None;
        }

        private bool IsPossibleMove(List<Vector2Int> possibleMoves, Vector2Int selectPos) {
            foreach (var move in possibleMoves) {
                if (move == selectPos) {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int ConvertToBoardPoint(Vector3 selectedPoint) {
            var inversePoint = res.boardTransform.InverseTransformPoint(selectedPoint);
            var cellLoc = res.cellTransform.localPosition;
            var cellSize = res.cellTransform.localScale;
            var floatVec = (inversePoint + new Vector3(-cellLoc.x, 0, cellLoc.z)) / cellSize.x;
            var point = new Vector2Int(Mathf.Abs((int)(floatVec.z)), Mathf.Abs((int)floatVec.x));

            return point;
        }

        private Vector3 ConvertToWorldPoint(Vector2Int boardPoint) {
            var offset = res.cellTransform.localScale / 2f;
            var floatVec = new Vector3(boardPoint.x, 0.4f, boardPoint.y);
            var cellLoc = res.cellTransform.localPosition;
            var cellSize = res.cellTransform.localScale;
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
                    Destroy(boardObj[i, j]);
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
                        boardObj[i, j] = SpawnObject(prefab, pos, res.boardTransform.transform);
                    }
                }
            }

            return ControllerErrors.None;
        }

        private void DestroyHighlightCells(Transform parent) {
            var sentencedHighlight = new List<Transform>();
            foreach (Transform child in parent) {
                sentencedHighlight.Add(child);
            }
            foreach (Transform child in sentencedHighlight) {
                child.parent = null;
                Destroy(child.gameObject);
            }
        }

        private GameObject SpawnObject(
            GameObject prefab,
            Vector2Int spawnPos,
            Transform parent
        ) {
            var spawnWorldPos = ConvertToWorldPoint(spawnPos);
            return Instantiate(prefab, spawnWorldPos, Quaternion.identity, parent);
        }

        public void OpenMenu() {
            if (res.gameMenu.activeSelf == true) {
                res.gameMenu.SetActive(false);
                this.enabled = true;
            } else {
                res.gameMenu.SetActive(true);
                this.enabled = false;
            }
        }

        public void Save() {
            jsonObject = JsonObject.Mk(new List<CheckerInfo>());
            for (int i = 0; i < board.GetLength(0); i++) {
                for (int j = 0; j < board.GetLength(1); j++) {
                    var board = this.board[i,j];
                    if (board.IsSome()) {
                        jsonObject.checkerInfos.Add(CheckerInfo.Mk(board.Peel(), i, j));
                    }
                }
            }
            SaveLoad.WriteJson(SaveLoad.GetJsonType<JsonObject>(jsonObject), "NewGame.json");
        }

        public void Load(string path) {
            whoseMove = Color.White;
            checkerMoves = null;
            board = new Option<Checker>[8,8];
            DestroyHighlightCells(res.storageHighlightCells.transform);
            var gameInfo = SaveLoad.ReadJson(path, jsonObject);
            foreach (var checkerInfo in gameInfo.checkerInfos) {
                board[checkerInfo.x, checkerInfo.y] = Option<Checker>.Some(checkerInfo.checker);
            }
            SpawnCheckers(board);
            res.gameMenu.SetActive(false);
            this.enabled = true;
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