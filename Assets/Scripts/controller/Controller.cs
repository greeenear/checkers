using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using checkers;
using option;
using UnityEngine.Events;
using UnityEditor;

namespace controller {
    public struct Map {
        public Option<Checker>[,] board;
        public GameObject[,] obj;
    }

    public struct SaveInfo {
        public string fileName;
        public DateTime saveDate;
        public ChKind checkerKind;
        public ChColor whoseMove;
        public Option<Checker>[,] board;
    }

    public class Controller : MonoBehaviour {
        public UnityEvent onGameOver;
        public UnityEvent onUnsuccessfulSaving;
        public UnityEvent onSuccessfulSaving;
        public Resources res;
        public GameObject storageHighlightCells;

        private BoardInfo boardInfo;
        private Map map;
        private ChKind chKind;

        private bool needRefreshBuffer;
        private int checkersCount;
        private PossibleGraph[] possibleGraphs;
        private int[] bufSize;

        private HashSet<Vector2Int> sentenced;
        private Option<Vector2Int> selected;
        private int curMark;
        private Option<Vector2Int> lastPos;
        private bool secondMove;
        private Vector2Int badDir;

        private ChColor whoseMove;

        private void Awake() {
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }

            if (res.board8x8.boardTransform == null || res.board8x8.cellTransform == null) {
                Debug.LogError("NoBoard");
                this.enabled = false;
                return;
            }

            possibleGraphs = new PossibleGraph[res.maxCheckerCount];
            bufSize = new int[res.maxCheckerCount];

            boardInfo = res.board8x8;
            map.board = new Option<Checker>[boardInfo.boardSize.x, boardInfo.boardSize.y];
            map.obj = new GameObject[boardInfo.boardSize.x, boardInfo.boardSize.y];
            sentenced = new HashSet<Vector2Int>();

            for (int i = 0; i < res.maxCheckerCount; i++) {
                var graph = new PossibleGraph {
                    connect = new int[res.maxBufSize, res.maxBufSize],
                    cells = new Vector2Int[res.maxBufSize],
                    marks = new int[res.maxBufSize]
                };
                possibleGraphs[i] = graph;
            }
        }

        private void Update() {
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }

            if (needRefreshBuffer) {
                needRefreshBuffer = false;
                checkersCount = 0;
                for (int i = 0; i < map.board.GetLength(0); i++) {
                    for (int j = 0; j < map.board.GetLength(1); j++) {
                        var curCell = Checkers.GetCell(map.board, new Vector2Int(i, j));
                        if (curCell.type == CellTy.Empty) continue;
                        var pos = new Vector2Int(i, j);

                        var loc = new ChLocation { board = map.board, pos = pos };
                        var buffer = possibleGraphs[checkersCount];
                        var count = Checkers.GetPossiblePaths(loc, chKind, buffer);
                        if (count == -1) {
                            Debug.LogError("CantGetPossiblePaths");
                            return;
                        }

                        bufSize[checkersCount] = count;
                        checkersCount++;
                    }
                }

                if (IsGameOver()) {
                    onGameOver?.Invoke();
                }
            }

            if (!Input.GetMouseButtonDown(0)) return;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
            var needAttack = IsNeedAttack(possibleGraphs, bufSize);

            var clickPos = ConvertToBoardPoint(hit.point);
            var cell = Checkers.GetCell(map.board, clickPos);

            if (!secondMove) DestroyHighlightCells(storageHighlightCells.transform);

            if (cell.type == CellTy.Filled && cell.ch.color == whoseMove && !secondMove) {
                var graph = new PossibleGraph();
                var size = 0;

                var isBadPos = true;
                var hasMove = false;
                for (int i = 0; i < checkersCount; i++) {
                    if (possibleGraphs[i].cells[0] == clickPos) {
                        isBadPos = false;
                        graph = possibleGraphs[i];
                        size = bufSize[i];
                        for (int j = 0; j < bufSize[i]; j++) {
                            if (graph.connect[0, j] != 0) {
                                if (needAttack && !HasAttack(graph, size)) break;
                                hasMove = true;
                            }
                        }
                    }
                }

                if (!hasMove) {
                    for (int i = 0; i < checkersCount; i++) {
                        for (int j = 0; j < bufSize[i]; j++) {
                            var curGraph = possibleGraphs[i];
                            if (curGraph.connect[0, j] != 0) {
                                if (needAttack && !HasAttack(curGraph, bufSize[i])) break;

                                var curCell = Checkers.GetCell(map.board, curGraph.cells[0]);
                                var isFilled = curCell.type == CellTy.Filled;
                                if (isFilled && curCell.ch.color != whoseMove) break;

                                var point = curGraph.cells[0];
                                var pos = ConvertToWorldPoint(point) - new Vector3(0, 0.1f, 0);
                                if (res.highlightCh == null) {
                                    Debug.LogError("NoHighlightCh");
                                } else {
                                    var parent = storageHighlightCells.transform;
                                    Instantiate(res.highlightCh, pos, Quaternion.identity, parent);
                                }
                                break;
                            }
                        }
                    }
                }

                if (isBadPos) return;

                selected = Option<Vector2Int>.Some(clickPos);
                lastPos = Option<Vector2Int>.Some(clickPos);

                if (needAttack && !HasAttack(graph, size)) {
                    selected = Option<Vector2Int>.None();
                    return;
                }

                Checkers.ShowMatrix(graph);
                HighlightCells(graph, size, clickPos);
            } else if (selected.IsSome()) {
                var curPos = selected.Peel();
                var lPos = lastPos.Peel();

                var graph = new PossibleGraph();
                var count = 0;
                for (int i = 0; i < checkersCount; i++) {
                    if (possibleGraphs[i].cells[0] == curPos) {
                        graph = possibleGraphs[i];
                        count = bufSize[i];
                    }
                }

                var curInd = Array.IndexOf<Vector2Int>(graph.cells, lPos);
                if (curInd == -1) return;

                var isBadPos = true;
                for (int i = 0; i < count; i++) {
                    if (graph.connect[curInd, i] != 0 && graph.cells[i] == clickPos) {
                        curMark = graph.marks[i];
                        if ((graph.marks[i] & curMark) > 0) {
                            graph.marks[i] -= curMark;
                            isBadPos = false;
                        }
                    }
                }

                var dir = clickPos - lPos;
                var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                for (var next = lPos + nDir; next != clickPos; next += nDir) {
                    if (map.board[next.x, next.y].IsSome()) {
                        if (!sentenced.Contains(next)) {
                            sentenced.Add(next);
                            break;
                        }
                        isBadPos = true;
                    }
                }

                if (isBadPos) return;
                badDir = nDir;

                map.board[clickPos.x, clickPos.y] = map.board[lPos.x, lPos.y];
                map.board[lPos.x, lPos.y] = Option<Checker>.None();

                var worldPos = ConvertToWorldPoint(clickPos);
                map.obj[lPos.x, lPos.y].transform.position = worldPos;
                map.obj[clickPos.x, clickPos.y] = map.obj[lPos.x, lPos.y];


                var edgeBoard = 0;
                var chOpt = map.board[clickPos.x, clickPos.y];
                if (chOpt.IsSome() && chOpt.Peel().color == ChColor.Black) {
                    edgeBoard = boardInfo.boardSize.x - 1;
                }

                if (clickPos.x == edgeBoard) {
                    var king = new Checker { type = ChType.King, color = whoseMove };
                    map.board[clickPos.x, clickPos.y] = Option<Checker>.Some(king);
                    var reverse = Quaternion.Euler(180, 0, 0);
                    map.obj[clickPos.x, clickPos.y].transform.rotation = reverse;
                }

                curInd = Array.IndexOf<Vector2Int>(graph.cells, clickPos);
                var nextMove = false;
                for (int i = 0; i < count; i++) {
                    if (graph.connect[curInd, i] != 0 && (graph.marks[i] & curMark) == curMark) {
                        nextMove = true;
                    }
                }

                DestroyHighlightCells(storageHighlightCells.transform);
                if (!nextMove) {
                    secondMove = false;
                    needRefreshBuffer = true;
                    selected = Option<Vector2Int>.None();
                    foreach (var sent in sentenced) {
                        Destroy(map.obj[sent.x, sent.y]);
                        map.board[sent.x, sent.y] = Option<Checker>.None();
                    }
                    sentenced.Clear();
                    whoseMove = (ChColor)((int)(whoseMove + 1) % (int)ChColor.Count);
                    curMark = 0;
                    return;
                }

                secondMove = true;
                HighlightCells(graph, count, clickPos);
                lastPos = Option<Vector2Int>.Some(clickPos);
            }
        }

        private void HighlightCells(PossibleGraph graph, int count, Vector2Int targetPos) {
            var index = Array.IndexOf<Vector2Int>(graph.cells, targetPos);
            for (int k = 0; k < count; k++) {
                var goodMark = (graph.marks[k] & curMark) > 0 || curMark == 0;
                if (graph.connect[index, k] != 0 && goodMark) {
                    var dir = graph.cells[k] - targetPos;
                    var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                    if (nDir == -badDir) continue;

                    var cellPos = graph.cells[k];
                    var boardPos = boardInfo.boardTransform.transform.position;
                    var spawnWorldPos = ConvertToWorldPoint(cellPos);
                    var parent = storageHighlightCells.transform;

                    Instantiate(res.highlightCell, spawnWorldPos, Quaternion.identity, parent);
                }
            }
        }

        public bool Load(string path) {
            if (path == null) {
                return false;
            }

            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return false;
            }

            if (res.board8x8.boardTransform == null || res.board8x8.cellTransform == null) {
                Debug.LogError("NoBoard");
                this.enabled = false;
                return false;
            }

            if (res.board10x10.boardTransform == null || res.board10x10.cellTransform == null) {
                Debug.LogError("NoBoard");
                this.enabled = false;
                return false;
            }

            string input;
            try {
                input = File.ReadAllText(path);
            } catch (Exception err) {
                Debug.LogError(err.ToString());
                return false;
            }

            var parseRes = CSV.Parse(input);
            if (parseRes.error != CSV.ErrorType.None) {
                Debug.LogError(parseRes.error.ToString());
                return false;
            }

            if (int.TryParse(parseRes.rows[parseRes.rows.Count - 1][3], out int result)) {
                chKind = (ChKind)result;
                if (chKind == ChKind.International) {
                    boardInfo = res.board10x10;
                    res.board8x8.boardTransform.gameObject.SetActive(false);
                    res.board10x10.boardTransform.gameObject.SetActive(true);
                } else {
                    boardInfo = res.board8x8;
                    res.board8x8.boardTransform.gameObject.SetActive(true);
                    res.board10x10.boardTransform.gameObject.SetActive(false);
                }
            }

            foreach (var chObj in map.obj) {
                Destroy(chObj);
            }

            map.obj = new GameObject[boardInfo.boardSize.x, boardInfo.boardSize.y];
            map.board = new Option<Checker>[boardInfo.boardSize.x, boardInfo.boardSize.y];
            for (int i = 0; i < parseRes.rows.Count; i++) {
                for (int j = 0; j < parseRes.rows[0].Count; j++) {
                    if (parseRes.rows[i][j] == "WhoseMove") {
                        if (int.TryParse(parseRes.rows[i][j + 1], out result)) {
                            whoseMove = (ChColor)result;
                        }
                        break;
                    }
                    var color = ChColor.White;
                    var type = ChType.Checker;
                    if (int.TryParse(parseRes.rows[i][j], out int res)) {
                        if (res % 2 != 0) color = ChColor.Black;
                        if (res > 1) type = ChType.King;
                        var checker = new Checker { color = color, type = type };
                        map.board[i, j] = Option<Checker>.Some(checker);
                    }
                }
            }

            DestroyHighlightCells(storageHighlightCells.transform);
            if (!SpawnCheckers(map.board)) {
                this.enabled = false;
                return false;
            }

            needRefreshBuffer = true;
            sentenced.Clear();
            selected = Option<Vector2Int>.None();

            return true;
        }

        public void NewGame(String path) {
            if (path == null) {
                Debug.LogError("CantLoadNewGame");
                return;
            }
            path = Path.Combine(Application.streamingAssetsPath, path);
            Load(path);
        }

        public void Save() {
            var path = Path.Combine(Application.persistentDataPath, Guid.NewGuid() + ".save");

            if (map.board == null) {
                return;
            }

            var rows = new List<List<string>>();
            for (int i = 0; i < map.board.GetLength(1); i++) {
                rows.Add(new List<string>());
                for (int j = 0; j < map.board.GetLength(0); j++) {
                    char cellInf = '-';
                    if (map.board[i, j].IsSome()) {
                        var checker = map.board[i, j].Peel();
                        int typeNum = (int)checker.color + (int)checker.type * 2;
                        cellInf = (char)(48 + typeNum);
                    }
                    rows[i].Add(cellInf.ToString());
                }
            }

            var whoseMoveNow = ((int)whoseMove).ToString();
            var kind = ((int)chKind).ToString();
            rows.Add(new List<string>() { "WhoseMove", whoseMoveNow, "ChKind", kind });

            try {
                File.WriteAllText(path, CSV.Generate(rows));
                onSuccessfulSaving?.Invoke();
            } catch (Exception err) {
                onUnsuccessfulSaving?.Invoke();
                Debug.LogError(err.ToString());
                return;
            }
            this.enabled = true;
        }

        public SaveInfo GetSaveInfo(string filePath) {
            if (filePath == null) {
                Debug.LogError("NoPath");
            }
            var saveInfo = new SaveInfo();
            var parseRes = CSV.Parse(File.ReadAllText(filePath));

            saveInfo.fileName = filePath;
            saveInfo.saveDate = File.GetLastWriteTime(filePath);

            saveInfo.board = new Option<Checker>[10, 10];
            if (parseRes.rows.Count < 10) {
                saveInfo.board = new Option<Checker>[8, 8];
            }
            for (int i = 0; i < parseRes.rows.Count; i++) {
                if (parseRes.rows[i][0] == "WhoseMove") {
                    if (int.TryParse(parseRes.rows[i][1], out int res)) {
                        saveInfo.whoseMove = (ChColor)res;
                    }
                }
                if (parseRes.rows[i][2] == "ChKind") {
                    if (int.TryParse(parseRes.rows[i][3], out int res)) {
                        saveInfo.checkerKind = (ChKind)res;
                    }
                    break;
                }

                for (int j = 0; j < parseRes.rows[i].Count; j++) {
                    if (int.TryParse(parseRes.rows[i][j], out int res)) {
                        var color = ChColor.White;
                        var type = ChType.Checker;
                        if (res % 2 != 0) color = ChColor.Black;
                        if (res > 1) type = ChType.King;

                        var checker = new Checker { type = type, color = color };
                        saveInfo.board[i,j] = Option<Checker>.Some(checker);
                    }
                }
            }

            return saveInfo;
        }

        public List<SaveInfo> GetSavesInfo() {
            var saveInfos = new List<SaveInfo>();
            string[] allfiles;
            try {
                allfiles = Directory.GetFiles(Application.persistentDataPath, "*.save");
            } catch (Exception err) {
                Debug.LogError(err.ToString());
                return null;
            }

            foreach (string fileName in allfiles) {
                saveInfos.Add(GetSaveInfo(fileName));
            }

            return saveInfos;
        }

        public bool DeleteFile(string path) {
            try {
                File.Delete(path);
            } catch (Exception err) {
                Debug.Log(err.ToString());
                return false;
            }

            return true;
        }

        private Vector3 ConvertToWorldPoint(Vector2Int boardPoint) {
            var size = boardInfo.cellTransform.localScale;
            var floatVec = new Vector3(boardPoint.x, 0.1f, boardPoint.y);
            var cellLoc = boardInfo.cellTransform.localPosition;
            var point = size.x * floatVec - new Vector3(cellLoc.x, 0, cellLoc.z) + size / 2f;

            return point;
        }

        private Vector2Int ConvertToBoardPoint(Vector3 selectedPoint) {
            var inversePoint = boardInfo.boardTransform.InverseTransformPoint(selectedPoint);
            var pos = boardInfo.cellTransform.localPosition;
            var size = boardInfo.cellTransform.localScale;
            var floatVec = (inversePoint + new Vector3(-pos.x, 0, pos.z)) / size.x;
            var point = new Vector2Int(Mathf.Abs((int)(floatVec.z)), Mathf.Abs((int)floatVec.x));

            return point;
        }

        private bool SpawnCheckers(Option<Checker>[,] board) {
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return false;
            }

            if (board == null) {
                Debug.LogError("BoardIsNull");
                return false;
            }

            if (res.whiteChecker == false) {
                Debug.LogError("NoWhiteChecker");
                return false;
            }

            if (res.blackChecker == false) {
                Debug.LogError("NoBlackChecker");
                return false;
            }

            for (int i = 0; i < board.GetLength(0); i++) {
                for (int j = 0; j < board.GetLength(1); j++) {
                    if (board[i, j].IsNone()) {
                        continue;
                    }

                    var checker = board[i, j].Peel();
                    var pref = res.whiteChecker;
                    if (checker.color == ChColor.Black) {
                        pref = res.blackChecker;
                    }

                    var spawnWorldPos = ConvertToWorldPoint(new Vector2Int(i, j));
                    var parent = boardInfo.boardTransform;
                    map.obj[i, j] = Instantiate(pref, spawnWorldPos, Quaternion.identity, parent);

                    if (checker.type == ChType.King) {
                        map.obj[i, j].transform.rotation = Quaternion.Euler(180, 0, 0);
                    }
                }
            }

            return true;
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

        private bool IsNeedAttack(PossibleGraph[] checkersMoves, int[] size) {
            for (int i = 0; i < checkersCount; i++) {
                var graph = checkersMoves[i];
                var count = size[i];
                if (graph.cells.GetLength(0) < 1) {
                    Debug.Log("BadLength");
                    return false;
                }

                var chOpt = map.board[graph.cells[0].x, graph.cells[0].y];
                if (HasAttack(graph, count) && chOpt.IsSome() && chOpt.Peel().color == whoseMove) {
                    return true;
                }
            }

            return false;
        }

        private bool HasAttack(checkers.PossibleGraph movesInfo, int count) {
            for (int i = 0; i < count; i++) {
                if (movesInfo.connect[0, i] != 0) {
                    var from = movesInfo.cells[0];
                    var to = movesInfo.cells[i];
                    var dir = to - from;
                    var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                    for (var next = from + nDir; next != to; next += nDir) {
                        if (map.board[next.x, next.y].IsSome()) return true;
                    }
                }
            }

            return false;
        }

        public void ExitGame() {
            Application.Quit();
            #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            #endif
        }

        private bool IsGameOver() {
            foreach (var checker in possibleGraphs) {
                var pos = checker.cells[0];
                var chOpt = map.board[pos.x, pos.y];
                if (chOpt.IsNone()) continue;
                var ch = chOpt.Peel();

                if (ch.color == whoseMove) {
                    return false;
                }
            }

            return true;
        }
    }
}