using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
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
            Load("NewGame.csv");
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
                            var attackFlag = false;
                            var next = pos + dir;
                            while (IsOnBoard(res.boardSize, next)) {
                                var nextOpt = map.board[next.x, next.y];
                                if (nextOpt.IsSome()) {
                                    if (sentenced.Contains(next)) {
                                        break;
                                    }

                                    if (attackFlag || nextOpt.Peel().color == curChecker.color) {
                                        break;
                                    }

                                    next += dir;
                                    var isOnBoard = IsOnBoard(res.boardSize, next) ;
                                    if (!isOnBoard || map.board[next.x, next.y].IsSome()) {
                                        break;
                                    }
                                    attackFlag = true;
                                }

                                if (curChecker.type == Type.King || attackFlag || dir.x == xDir) {
                                    checkerMoves.Add(next, attackFlag);
                                }
                                if (curChecker.type == Type.Checker) {
                                    break;
                                }
                                next += dir;
                            }
                        }
                        this.checkerMoves.Add(pos, checkerMoves);
                    }
                }

                if (isGameOver) {
                    res.gameMenu.SetActive(true);
                    Debug.Log("game over");
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
            var checkerOpt = map.board[selectedPos.x, selectedPos.y];
            var checker = checkerOpt.Peel();

            var board = map.board;
            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                if (!checkerMoves.ContainsKey(selectedPos)) {
                    return;
                }
                selectedOpt = Option<Vector2Int>.Some(selectedPos);

                var currentInfo = checkerMoves[selectedPos];
                HighlightCells(currentInfo, IsNeedAttack(checkerMoves));
            } else if (selectedOpt.IsSome()) {
                var selected = selectedOpt.Peel();
                var currentInfo = checkerMoves[selected];
                if (!currentInfo.ContainsKey(selectedPos)) {
                    return;
                }
                if (!currentInfo[selectedPos] && IsNeedAttack(checkerMoves)) {
                    return;
                }

                board[selectedPos.x, selectedPos.y] = board[selected.x, selected.y];
                board[selected.x, selected.y] = Option<Checker>.None();
                checkerMoves.Clear();

                var secondMoveInfos = new Dictionary<Vector2Int, bool>();
                var dif = selectedPos - selected;
                var dir = new Vector2Int(dif.x / Mathf.Abs(dif.x), dif.y / Mathf.Abs(dif.y));
                var next = selected + dir;

                while (next != selectedPos) {
                    if (board[next.x, next.y].IsSome()) {
                        sentenced.Add(next);
                        foreach (var moveDir in res.directions) {
                            var last = selectedPos + moveDir;

                            while (IsOnBoard(res.boardSize, last)) {
                                if (board[last.x, last.y].IsSome()) {
                                    var enemyСhecker = board[last.x, last.y].Peel();
                                    if (enemyСhecker.color != whoseMove) {
                                        if (sentenced.Contains(last)) {
                                            break;
                                        }

                                        last += moveDir;
                                        var isOnboard = IsOnBoard(res.boardSize, last);
                                        if (isOnboard && board[last.x, last.y].IsNone()) {
                                            secondMoveInfos.Add(last, true);
                                        }
                                    }
                                }

                                last += moveDir;
                            }
                        }
                    }
                    next += dir;
                }


                var pos = ConvertToWorldPoint(selectedPos);
                map.obj[selected.x, selected.y].transform.position = pos;
                var newLoc = map.obj[selected.x, selected.y];
                map.obj[selectedPos.x, selectedPos.y] = newLoc;

                if (selectedPos.x == 0 && whoseMove == Color.White
                || selectedPos.x == res.boardSize.x - 1 && whoseMove == Color.Black) {
                    map.board[selectedPos.x, selectedPos.y] = Option<Checker>.None();
                    var king = new Checker {type = Type.King, color = whoseMove };
                    map.board[selectedPos.x, selectedPos.y] = Option<Checker>.Some(king);

                    var target = Quaternion.Euler(180, 0, 0);
                    map.obj[selectedPos.x, selectedPos.y].transform.rotation = target;
                }

                if (secondMoveInfos.Count != 0) {
                    checkerMoves.Add(selectedPos, secondMoveInfos);
                    HighlightCells(secondMoveInfos, true);
                    selectedOpt = Option<Vector2Int>.Some(selectedPos);
                    return;
                }
                foreach (var sentencedPos in sentenced) {
                    map.board[sentencedPos.x, sentencedPos.y] = Option<Checker>.None();
                    Destroy(map.obj[sentencedPos.x, sentencedPos.y]);
                }
                whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                selectedOpt = Option<Vector2Int>.None();
                sentenced.Clear();
                checkerMoves = null;
            }
        }

        public static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }

            return true;
        }

        private ControllerErrors HighlightCells(Dictionary<Vector2Int, bool> moves, bool attack) {
            if (moves == null) {
                Debug.LogError("ListIsNull");
                return ControllerErrors.ListIsNull;
            }
            var boardPos = res.boardTransform.transform.position;
            foreach (var pos in moves) {
                if (attack && pos.Value || !attack && !pos.Value) {
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

        public void Load(string path) {
            map.board = new Option<Checker>[res.boardSize.x, res.boardSize.y];
            var board = map.board;
            string input = "";
            try {
                input = File.ReadAllText(path);
            }
            catch (Exception err) {
                Debug.LogError("CantLoad");
                Debug.LogError(err.ToString());
            }

            var parseRes = CSV.Parse(input);
            if (parseRes.error != CSV.ErrorType.None) {
                Debug.LogError(parseRes.error.ToString());
            }

            for (int i = 0; i < parseRes.rows.Count; i++) {
                for (int j = 0; j < parseRes.rows[0].Count; j++) {
                    if (parseRes.rows[i][j] == "WhoseMove") {
                        whoseMove = (Color)int.Parse(parseRes.rows[i][j + 1]);
                        break;
                    }
                    var checker = new Checker();
                    if (int.Parse(parseRes.rows[i][j]) == 0) {
                        continue;
                    } else if (int.Parse(parseRes.rows[i][j]) == 1) {
                        checker = new Checker { color = Color.White };
                    } else if (int.Parse(parseRes.rows[i][j]) == 2) {
                        checker = new Checker { color = Color.Black };
                    } else if (int.Parse(parseRes.rows[i][j]) == 3) {
                        checker = new Checker { color = Color.White, type = Type.King };
                    }
                    board[i, j] = Option<Checker>.Some(checker);
                }
            }

            DestroyHighlightCells(res.storageHighlightCells.transform);
            SpawnCheckers(map.board);
            checkerMoves = null;
            sentenced.Clear();
            res.gameMenu.SetActive(false);
            enabled = true;
        }

        public void Save(string path) {
            List<List<string>> rows = new List<List<string>>();
            for (int i = 0; i < map.board.GetLength(1); i++) {
                rows.Add(new List<string>());
                for (int j = 0; j < map.board.GetLength(0); j++) {
                    if(map.board[i, j].IsNone()) {
                        rows[i].Add("0");
                    }

                    if (map.board[i, j].IsSome()) {
                        var checker = map.board[i, j].Peel();
                        if (checker.color == Color.Black) {
                            rows[i].Add("2");
                        } else if (checker.color == Color.White) {
                            rows[i].Add("1");
                        }
                    }
                }
            }
            rows.Add(new List<string>() {
                "WhoseMove",
                ((int)whoseMove).ToString()
            });
            string output = CSV.Generate(rows);
            try {
                File.WriteAllText(path, output);
            }
            catch (Exception err) {
                Debug.LogError(err.ToString());
            }
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
                    Destroy(map.obj[i, j]);
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
                        map.obj[i, j] = SpawnObject(prefab, pos, boardTransform);
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

        private bool IsNeedAttack(Dictionary<Vector2Int, Dictionary<Vector2Int, bool>> checkers) {
            foreach (var checker in checkers) {
                foreach (var move in checker.Value) {
                    if (move.Value) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}