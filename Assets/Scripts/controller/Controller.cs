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

    public struct Checker {
        public Type type;
        public Color color;
    }

    public struct Map {
        public Option<Checker>[,] board;
        public GameObject[,] obj;
    }

    public class Controller : MonoBehaviour {
        private Resources res;
        private Map map;
        private bool isGameOver;

        public Dictionary<Vector2Int, Dictionary<Vector2Int, bool>> checkerMoves;
        public HashSet<Vector2Int> sentenced;

        private Option<Vector2Int> selectedOpt;
        private Color whoseMove;

        private GameState saveInfo;

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
            map.board = new Option<Checker>[res.boardSize.x, res.boardSize.y];
            map.obj = new GameObject[res.boardSize.x, res.boardSize.y];
            sentenced = new HashSet<Vector2Int>();
            Load("NewGame.json");
            SpawnCheckers(map.board);
        }

        private void Update() {
            if (checkerMoves == null) {
                isGameOver = true;
                checkerMoves = new Dictionary<Vector2Int, Dictionary<Vector2Int, bool>>();
                for (int i = 0; i < map.board.GetLength(0); i++) {
                    for (int j = 0; j < map.board.GetLength(1); j++) {
                        var cellOpt = map.board[i, j];
                        if (cellOpt.IsNone() || cellOpt.Peel().color != whoseMove) {
                            continue;
                        }
                        isGameOver = false;
                        var xDir = 1;
                        var curChecker = cellOpt.Peel();
                        if (curChecker.type == Type.Checker && curChecker.color == Color.White) {
                            xDir = -1;
                        }

                        var pos = new Vector2Int(i, j);
                        var checkerMoves = new Dictionary<Vector2Int, bool>();
                        foreach (var dir in res.directions) {
                            var nextCell = pos + dir;
                            while (IsOnBoard(res.boardSize, nextCell)) {
                                var nextCellOpt = fullBoard.board[nextCell.x, nextCell.y];
                                if (nextCellOpt.IsSome()) {
                                    if (gameInfo.sentenced.Contains(nextCell)) {
                                        break;
                                    }

                                    if (nextCellOpt.Peel().color != curChecker.color) {
                                        nextCell += dir;
                                        while (IsOnBoard(res.boardSize, nextCell)) {
                                            if (fullBoard.board[nextCell.x, nextCell.y].IsSome()) {
                                                break;
                                            }

                                            checkerMoves.Add(nextCell, true);
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
                                    checkerMoves.Add(nextCell, false);
                                }

                                nextCell += dir;
                                if (curChecker.type == Type.Checker) {
                                    break;
                                }
                            }
                        }
                        this.checkerMoves.Add(pos, checkerMoves);
                    }
                }

                if (isGameOver) {
                    res.gameMenu.SetActive(true);
                    this.enabled = false;
                    return;
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
            var selectedPos = ConvertToBoardPoint(hit.point);
            var checkerOpt = fullBoard.board[selectedPos.x, selectedPos.y];
            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                playerAction = PlayerAction.Select;
                selectedChecker = selectedPos;
            }
            var checker = checkerOpt.Peel();

            var board = fullBoard.board;
            if (playerAction == PlayerAction.Select) {
                if (!gameInfo.checkerMoves.ContainsKey(selectedPos)) {
                    Debug.LogError("NoKey");
                    playerAction = PlayerAction.None;
                    return;
                }

                var currentInfo = gameInfo.checkerMoves[selectedPos];
                HighlightCells(currentInfo, gameInfo.isNeedAttack);
                playerAction = PlayerAction.Move;
            } else if (playerAction == PlayerAction.Move) {
                var currentInfo = gameInfo.checkerMoves[selectedChecker];
                if (!currentInfo.ContainsKey(selectedPos)) {
                    return;
                }
                if (!currentInfo[selectedPos] && gameInfo.isNeedAttack) {
                    return;
                }

                board[selectedPos.x, selectedPos.y] = board[selectedChecker.x, selectedChecker.y];
                board[selectedChecker.x, selectedChecker.y] = Option<Checker>.None();
                gameInfo.checkerMoves.Clear();

                var secondMoveInfos = new Dictionary<Vector2Int, bool>();
                var dif = selectedPos - selectedChecker;
                var dir = new Vector2Int(dif.x / Mathf.Abs(dif.x), dif.y / Mathf.Abs(dif.y));
                var next = selectedChecker + dir;

                while (next != selectedPos) {
                    if (board[next.x, next.y].IsSome()) {
                        gameInfo.sentenced.Add(next);

                        foreach (var moveDir in res.directions) {
                            next = selectedPos + moveDir;
                            if (gameInfo.sentenced.Contains(next)) {
                                continue;
                            }

                            if (IsOnBoard(res.boardSize, next) && board[next.x, next.y].IsSome()) {
                                var enemyСhecker = board[next.x, next.y].Peel();
                                if (enemyСhecker.color != whoseMove) {
                                    next = next + moveDir;
                                    var isOnboard = IsOnBoard(res.boardSize, next);
                                    if (isOnboard && board[next.x, next.y].IsNone()) {
                                        secondMoveInfos.Add(next, true);
                                    }
                                }
                            }
                        }
                        break;
                    }
                    next += dir;
                }

                var pos = ConvertToWorldPoint(selectedPos);
                fullBoard.boardObj[selectedChecker.x, selectedChecker.y].transform.position = pos;
                var newLoc = fullBoard.boardObj[selectedChecker.x, selectedChecker.y];
                fullBoard.boardObj[selectedPos.x, selectedPos.y] = newLoc;

                if (selectedPos.x == 0 && whoseMove == Color.White
                || selectedPos.x == res.boardSize.x - 1 && whoseMove == Color.Black) {
                    fullBoard.board[selectedPos.x, selectedPos.y] = Option<Checker>.None();
                    var king = new Checker {type = Type.King, color = whoseMove };
                    fullBoard.board[selectedPos.x, selectedPos.y] = Option<Checker>.Some(king);

                    var target = Quaternion.Euler(180, 0, 0);
                    fullBoard.boardObj[selectedPos.x, selectedPos.y].transform.rotation = target;
                }

                if (secondMoveInfos.Count != 0) {
                    gameInfo.checkerMoves.Add(selectedPos, secondMoveInfos);
                    HighlightCells(secondMoveInfos, true);
                    selectedChecker = selectedPos;
                    playerAction = PlayerAction.Move;
                    return;
                }
                foreach (var sentencedPos in gameInfo.sentenced) {
                    fullBoard.board[sentencedPos.x, sentencedPos.y] = Option<Checker>.None();
                    Destroy(fullBoard.boardObj[sentencedPos.x, sentencedPos.y]);
                }
                playerAction = PlayerAction.ChangeMove;
            }

            if (playerAction == PlayerAction.ChangeMove) {
                whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                gameInfo.sentenced.Clear();
                gameInfo.checkerMoves = null;
                gameInfo.isNeedAttack = false;
                playerAction = PlayerAction.None;
            }
        }

        public static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }

            return true;
        }

        private ControllerErrors HighlightCells(Dictionary<Vector2Int, bool> moves, bool isNeedAttack) {
            if (moves == null) {
                Debug.LogError("ListIsNull");
                return ControllerErrors.ListIsNull;
            }
            var boardPos = res.boardTransform.transform.position;
            foreach (var pos in moves) {
                if (isNeedAttack && pos.Value || !isNeedAttack && !pos.Value) {
                    SpawnObject(res.highlightCell, pos.Key, res.storageHighlightCells.transform);
                }
            }

            return ControllerErrors.None;
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
            saveInfo = GameState.Mk(new List<CheckerInfo>(), whoseMove);
            for (int i = 0; i < fullBoard.board.GetLength(0); i++) {
                for (int j = 0; j < fullBoard.board.GetLength(1); j++) {
                    var board = fullBoard.board[i,j];
                    if (board.IsSome()) {
                        saveInfo.checkerInfos.Add(CheckerInfo.Mk(board.Peel(), i, j));
                    }
                }
            }
            SaveLoad.WriteJson(SaveLoad.GetJsonType<GameState>(saveInfo), "");
        }

        public void Load(string path) {
            var loadInfo = SaveLoad.ReadJson(path, saveInfo);
            whoseMove = loadInfo.whoseMove;
            fullBoard.board = new Option<Checker>[8,8];
            DestroyHighlightCells(res.storageHighlightCells.transform);
            foreach (var checkerInfo in loadInfo.checkerInfos) {
                var checker = Option<Checker>.Some(checkerInfo.checker);
                fullBoard.board[checkerInfo.x, checkerInfo.y] = checker;
            }
            SpawnCheckers(fullBoard.board);
            gameInfo.checkerMoves = null;
            gameInfo.isNeedAttack = false;
            gameInfo.sentenced.Clear();
            res.gameMenu.SetActive(false);
            enabled = true;
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
                        board[i, j + 1] = Option<Checker>.Some(new Checker { color = color });
                    } else {
                        board[i, j] = Option<Checker>.Some(new Checker { color = color });
                    }
                }
            }

            return ControllerErrors.None;
        }
    }
}