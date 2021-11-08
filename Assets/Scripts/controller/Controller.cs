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

    public enum ChKind {
        Russian,
        English,
        Pool,
        International
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
        private ChKind chKind;

        private Dictionary<Vector2Int, Dictionary<Vector2Int, bool>> allCheckerMoves;
        private HashSet<Vector2Int> sentenced;
        private Option<Vector2Int> selected;

        private Color whoseMove;

        private void Awake() {
            res = gameObject.GetComponentInParent<Resources>();
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }
            res.InitializeBoard(chKind.ToString());
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
            Load("StartGame.csv");
        }

        private void Update() {
            if (allCheckerMoves == null) {
                allCheckerMoves = new Dictionary<Vector2Int, Dictionary<Vector2Int, bool>>();
                var size = res.boardSize;
                for (int i = 0; i < map.board.GetLength(0); i++) {
                    for (int j = 0; j < map.board.GetLength(1); j++) {

                        var cellOpt = map.board[i, j];
                        if (cellOpt.IsNone() || cellOpt.Peel().color != whoseMove) continue;

                        var curCh = cellOpt.Peel();

                        var xDir = 1;
                        if (curCh.color == Color.White) {
                            xDir = -1;
                        }

                        var pos = new Vector2Int(i, j);
                        var checkerMoves = new Dictionary<Vector2Int, bool>();
                        foreach (var dir in res.directions) {
                            var chFound = false;
                            for (var next = pos + dir; IsOnBoard(size, next); next += dir) {
                                var nextOpt = map.board[next.x, next.y];
                                if (nextOpt.IsSome()) {
                                    var nextColor = nextOpt.Peel().color;
                                    var isSentenced = sentenced.Contains(next);
                                    if (isSentenced || chFound || nextColor == curCh.color) break;

                                    chFound = true;
                                } else {
                                    var wrongMove = curCh.type == Type.Checker && dir.x != xDir;
                                    switch (chKind) {
                                        case ChKind.Pool:
                                        case ChKind.Russian:
                                        case ChKind.International:
                                            wrongMove = wrongMove && !chFound;
                                            break;
                                    }

                                    if (!wrongMove) {
                                        checkerMoves.Add(next, chFound);
                                    }

                                    if (curCh.type == Type.Checker || chKind == ChKind.English) {
                                        break;
                                    }
                                }
                            }
                        }
                        allCheckerMoves.Add(pos, checkerMoves);
                    }
                }
            }

            if (allCheckerMoves.Count == 0) {
                res.gameMenu.SetActive(true);
                this.enabled = false;
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            DestroyHighlightCells(res.storageHighlightCells.transform);
            var cliсkPos = ConvertToBoardPoint(hit.point);
            var checkerOpt = map.board[cliсkPos.x, cliсkPos.y];

            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                if (!allCheckerMoves.ContainsKey(cliсkPos)) return;
                selected = Option<Vector2Int>.Some(cliсkPos);

                HighlightCells(allCheckerMoves[cliсkPos], IsNeedAttack(allCheckerMoves));
            } else if (selected.IsSome()) {
                var curPos = selected.Peel();
                var curCh = map.board[curPos.x, curPos.y].Peel();
                var curChMoves = allCheckerMoves[curPos];
                if (!curChMoves.ContainsKey(cliсkPos)) return;

                if (!curChMoves[cliсkPos] && IsNeedAttack(allCheckerMoves)) return;

                map.board[cliсkPos.x, cliсkPos.y] = map.board[curPos.x, curPos.y];
                map.board[curPos.x, curPos.y] = Option<Checker>.None();
                allCheckerMoves.Clear();

                var worldPos = ConvertToWorldPoint(cliсkPos);
                map.obj[curPos.x, curPos.y].transform.position = worldPos;
                map.obj[cliсkPos.x, cliсkPos.y] = map.obj[curPos.x, curPos.y];

                var edgeBoard = 0;
                if (curCh.color == Color.Black) {
                    edgeBoard = res.boardSize.x - 1;
                }


                var dir = cliсkPos - curPos;
                var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                for (var next = curPos + nDir; next != cliсkPos; next += nDir) {
                    if (map.board[next.x, next.y].IsSome()) {
                        sentenced.Add(next);
                    }
                }

                var onEdgeBoard = cliсkPos.x == edgeBoard;
                if (onEdgeBoard && !(chKind == ChKind.International || sentenced.Count != 0)) {
                    var king = new Checker { type = Type.King, color = whoseMove };
                    map.board[cliсkPos.x, cliсkPos.y] = Option<Checker>.Some(king);
                    var reverse = Quaternion.Euler(180, 0, 0);
                    map.obj[cliсkPos.x, cliсkPos.y].transform.rotation = reverse;
                    curCh = king;
                }

                var secondMoveInfos = new Dictionary<Vector2Int, bool>();
                var noSecondMove = chKind == ChKind.Pool && onEdgeBoard;
                if (sentenced.Count != 0 && !noSecondMove) {
                    var xDir = 1;
                    if (curCh.color == Color.White) {
                        xDir = -1;
                    }
                    var size = res.boardSize;
                    foreach (var moveDir in res.directions) {
                        var last = cliсkPos + moveDir;
                        var chFound = false;
                        for (last = cliсkPos + moveDir; IsOnBoard(size, last); last += moveDir) {
                            var nextOpt = map.board[last.x, last.y];
                            if (nextOpt.IsSome()) {
                                var nextColor = nextOpt.Peel().color;
                                var isSentenced = sentenced.Contains(last);

                                if (isSentenced || chFound || nextColor == curCh.color) break;
                                chFound = true;
                            } else {
                                var wrongMove = curCh.type == Type.Checker && dir.x != xDir;
                                switch (chKind) {
                                    case ChKind.Pool:
                                    case ChKind.Russian:
                                    case ChKind.International:
                                        wrongMove = wrongMove && !chFound;
                                        break;
                                }

                                if (!wrongMove && chFound) {
                                    secondMoveInfos.Add(last, chFound);
                                }

                                if (curCh.type == Type.Checker || chKind == ChKind.English) {
                                    break;
                                }
                            }
                        }
                    }
                }

                if (secondMoveInfos.Count != 0) {
                    allCheckerMoves.Add(cliсkPos, secondMoveInfos);
                    HighlightCells(secondMoveInfos, true);
                    selected = Option<Vector2Int>.Some(cliсkPos);
                    return;
                } else if (onEdgeBoard && chKind == ChKind.International) {
                    var king = new Checker { type = Type.King, color = whoseMove };
                    map.board[cliсkPos.x, cliсkPos.y] = Option<Checker>.Some(king);
                    var reverse = Quaternion.Euler(180, 0, 0);
                    map.obj[cliсkPos.x, cliсkPos.y].transform.rotation = reverse;
                    curCh = king;
                }

                foreach (var sentencedPos in sentenced) {
                    map.board[sentencedPos.x, sentencedPos.y] = Option<Checker>.None();
                    Destroy(map.obj[sentencedPos.x, sentencedPos.y]);
                }

                whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                selected = Option<Vector2Int>.None();
                sentenced.Clear();
                allCheckerMoves = null;
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
            var pos = res.cellTransform.localPosition;
            var size = res.cellTransform.localScale;
            var floatVec = (inversePoint + new Vector3(-pos.x - 0.1f, 0, pos.z + 0.1f)) / size.x;
            var point = new Vector2Int(Mathf.Abs((int)(floatVec.z)), Mathf.Abs((int)floatVec.x));

            return point;
        }

        public void Load(string path) {
            map.board = new Option<Checker>[res.boardSize.x, res.boardSize.y];
            var board = map.board;
            string input;
            try {
                input = File.ReadAllText(chKind.ToString() + path);
            }
            catch (Exception err) {
                Debug.LogError("CantLoad");
                Debug.LogError(err.ToString());
                return;
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
                    if (parseRes.rows[i][j] == "0") {
                        continue;
                    } else if (parseRes.rows[i][j] == "1") {
                        checker = new Checker { color = Color.White };
                    } else if (parseRes.rows[i][j] == "2") {
                        checker = new Checker { color = Color.Black };
                    } else if (parseRes.rows[i][j] == "3") {
                        checker = new Checker { color = Color.White, type = Type.King };
                    }
                    board[i, j] = Option<Checker>.Some(checker);
                }
            }

            DestroyHighlightCells(res.storageHighlightCells.transform);
            SpawnCheckers(map.board);
            allCheckerMoves = null;
            sentenced.Clear();
            selected = Option<Vector2Int>.None();
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
                }
            );
            string output = CSV.Generate(rows);
            try {
                File.WriteAllText(path, output);
            }
            catch (Exception err) {
                Debug.LogError(err.ToString());
            }
        }

        public void SetGameRules(int type) {
            chKind = (ChKind)type;
        }

        private Vector3 ConvertToWorldPoint(Vector2Int boardPoint) {
            var size = res.cellTransform.localScale;

            var floatVec = new Vector3(boardPoint.x, 0.1f, boardPoint.y);
            var cellLoc = res.cellTransform.localPosition;
            var point = size.x * floatVec - new Vector3(cellLoc.x, 0, cellLoc.z) + size / 2f;

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