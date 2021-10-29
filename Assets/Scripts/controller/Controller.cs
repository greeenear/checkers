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

    public enum PlayerAction {
        None,
        Select,
        Move,
        ChangeMove
    }

    public struct Checker {
        public Type type;
        public Color color;
    }

    public struct FullBoard {
        public Option<Checker>[,] board;
        public GameObject[,] boardObj;
    }

    public struct GameInfo {
        public bool isNeedAttack;
        public Dictionary<Vector2Int, List<MoveInfo>> checkerMoves;
    }

    public struct MoveInfo {
        public Vector2Int cell;
        public bool isAttack;
        public static MoveInfo Mk(Vector2Int cell, bool isAttack) {
            return new MoveInfo { cell = cell, isAttack = isAttack };
        }
    }

    public class Controller : MonoBehaviour {
        private Resources res;
        private FullBoard fullBoard;
        private GameInfo gameInfo;

        private HashSet<Vector2Int> sentenced = new HashSet<Vector2Int>();
        private Vector2Int selectedChecker;
        private GameState gameState;
        private Color whoseMove;
        private PlayerAction playerAction;

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
            fullBoard.board = new Option<Checker>[res.boardSize.x, res.boardSize.y];
            fullBoard.boardObj = new GameObject[res.boardSize.x, res.boardSize.y];
            FillBoard(fullBoard.board);
            SpawnCheckers(fullBoard.board);
        }

        private void Update() {
            if (gameInfo.checkerMoves == null) {
                gameInfo.checkerMoves = new Dictionary<Vector2Int, List<MoveInfo>>();
                for (int i = 0; i < fullBoard.board.GetLength(0); i++) {
                    for (int j = 0; j < fullBoard.board.GetLength(1); j++) {
                        var cellOpt = fullBoard.board[i, j];
                        if (cellOpt.IsNone() || cellOpt.Peel().color != whoseMove) {
                            continue;
                        }

                        var xDir = 1;
                        var curChecker = cellOpt.Peel();
                        if (curChecker.type == Type.Checker && curChecker.color == Color.White) {
                            xDir = -1;
                        }

                        var pos = new Vector2Int(i, j);
                        var checkerMoves = new List<MoveInfo>();
                        foreach (var dir in res.directions) {
                            var nextCell = pos + dir;
                            while (IsOnBoard(res.boardSize, nextCell)) {
                                var nextCellOpt = fullBoard.board[nextCell.x, nextCell.y];
                                if (nextCellOpt.IsSome()) {
                                    if (sentenced.Contains(nextCell)) {
                                        break;
                                    }
                                    if (nextCellOpt.Peel().color != curChecker.color) {
                                        nextCell += dir;
                                        while (IsOnBoard(res.boardSize, nextCell)) {
                                            if (fullBoard.board[nextCell.x, nextCell.y].IsSome()) {
                                                break;
                                            }
                                            checkerMoves.Add(MoveInfo.Mk(nextCell, true));
                                            gameInfo.isNeedAttack = true;
                                            nextCell += dir;
                                            if (curChecker.type == Type.Checker) {
                                                break;
                                            }
                                        }
                                    }
                                    break;
                                }

                                if (curChecker.type != Type.Checker || dir.x == xDir) {
                                    checkerMoves.Add(MoveInfo.Mk(nextCell, false));
                                }

                                nextCell += dir;
                                if (curChecker.type == Type.Checker) {
                                    break;
                                }
                            }
                        }

                        gameInfo.checkerMoves.Add(pos, checkerMoves);
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
            var checkerOpt = fullBoard.board[targetPos.x, targetPos.y];
            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                playerAction = PlayerAction.Select;
                selectedChecker = targetPos;
            }
            var checker = checkerOpt.Peel();

            var board = fullBoard.board;
            if (playerAction == PlayerAction.Select) {
                var currentInfo = gameInfo.checkerMoves[targetPos];
                HighlightCells(currentInfo, gameInfo.isNeedAttack);
                playerAction = PlayerAction.Move;
            } else if(playerAction == PlayerAction.Move) {
                var currentInfo = gameInfo.checkerMoves[selectedChecker];
                if (!IsPossibleMove(currentInfo, targetPos, gameInfo.isNeedAttack)) {
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

                RelocateChecker(fullBoard.boardObj, selectedChecker, targetPos, sentenced);
                if (CheckPromotion(targetPos, whoseMove, res.boardSize)) {
                    CheckerPromotion(targetPos, whoseMove);
                }

                if (sentenced.HasValue) {
                    this.sentenced.Add(sentenced.Value);
                }
                if (secondMove) {
                    selectedChecker = targetPos;
                    playerAction = PlayerAction.Move;
                    gameInfo.checkerMoves = null;
                    return;
                }
                foreach (var pos in this.sentenced) {
                    fullBoard.board[pos.x, pos.y] = Option<Checker>.None();
                    Destroy(fullBoard.boardObj[pos.x, pos.y]);
                }
                this.sentenced.Clear();
                playerAction = PlayerAction.ChangeMove;
            }

            if (playerAction == PlayerAction.ChangeMove) {
                IsGameOver(fullBoard.board, whoseMove);
                whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                gameInfo.checkerMoves = null;
                gameInfo.isNeedAttack = false;
                playerAction = PlayerAction.None;
            }
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
            fullBoard.board[pos.x, pos.y] = Option<Checker>.None();
            var king = new Checker {type = Type.King, color = color };
            fullBoard.board[pos.x, pos.y] = Option<Checker>.Some(king);

            var target = Quaternion.Euler(180, 0, 0);
            fullBoard.boardObj[pos.x, pos.y].transform.rotation = target;

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

        private ControllerErrors HighlightCells(List<MoveInfo> possibleMoves, bool isNeedAttack) {
            if (possibleMoves == null) {
                Debug.LogError("ListIsNull");
                return ControllerErrors.ListIsNull;
            }
            var boardPos = res.boardTransform.transform.position;
            foreach (var pos in possibleMoves) {
                if (isNeedAttack && pos.isAttack || !isNeedAttack && !pos.isAttack) {
                    SpawnObject(res.highlightCell, pos.cell, res.storageHighlightCells.transform);
                }
            }

            return ControllerErrors.None;
        }

        private bool IsPossibleMove(List<MoveInfo> moves, Vector2Int pos, bool isNeedAttack) {
            foreach (var move in moves) {
                if (move.cell == pos) {
                    if (isNeedAttack && move.isAttack || !isNeedAttack && !move.isAttack) {
                        return true;
                    }
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
                    Destroy(fullBoard.boardObj[i, j]);
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
                        var boardTransform = res.boardTransform.transform;
                        fullBoard.boardObj[i, j] = SpawnObject(prefab, pos, boardTransform);
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
            gameState = GameState.Mk(new List<CheckerInfo>());
            for (int i = 0; i < fullBoard.board.GetLength(0); i++) {
                for (int j = 0; j < fullBoard.board.GetLength(1); j++) {
                    var board = fullBoard.board[i,j];
                    if (board.IsSome()) {
                        gameState.checkerInfos.Add(CheckerInfo.Mk(board.Peel(), i, j));
                    }
                }
            }
            SaveLoad.WriteJson(SaveLoad.GetJsonType<GameState>(gameState), "");
        }

        public void Load(string path) {
            whoseMove = Color.White;
            fullBoard.board = new Option<Checker>[8,8];
            DestroyHighlightCells(res.storageHighlightCells.transform);
            var gameInfo = SaveLoad.ReadJson(path, gameState);
            foreach (var checkerInfo in gameInfo.checkerInfos) {
                var checker = Option<Checker>.Some(checkerInfo.checker);
                fullBoard.board[checkerInfo.x, checkerInfo.y] = checker;
            }
            SpawnCheckers(fullBoard.board);
            this.gameInfo.checkerMoves = null;
            this.gameInfo.isNeedAttack = false;
            sentenced.Clear();
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