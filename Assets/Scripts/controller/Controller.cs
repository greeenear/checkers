using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using checkers;
using ai;
using option;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace controller {
    public enum GameMode {
        PlayerVsPlayer,
        PlayerVsPC
    }

    public struct Map {
        public int[,] board;
        public GameObject[,] obj;
    }

    public struct SaveInfo {
        public string fileName;
        public DateTime saveDate;
        public ChKind checkerKind;
        public int whoseMove;
        public int[,] board;
    }

    public class Controller : MonoBehaviour {
        public GameMode gameMode;
        public UnityEvent onGameOver;
        public UnityEvent onUnsuccessfulSaving;
        public UnityEvent onSuccessfulSaving;
        public Resources res;
        public GameObject storageHighlightCells;
        public Camera camera;

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
        private ChPath bestAiPath;

        private int whoseMove;
        [SerializeField]
        private ARRaycastManager raycastManager;
        private List<ARRaycastHit> hits = new List<ARRaycastHit>();

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
            map.board = new int[boardInfo.boardSize.x, boardInfo.boardSize.y];
            map.obj = new GameObject[boardInfo.boardSize.x, boardInfo.boardSize.y];
            sentenced = new HashSet<Vector2Int>();

            for (int i = 0; i < res.maxCheckerCount; i++) {
                var graph = new PossibleGraph {
                    connect = new int[res.maxBufSize, res.maxBufSize],
                    cells = new Vector2Int[res.maxBufSize],
                    marks = new int[res.maxBufSize],
                    enemies = new EnemyInfo[res.maxBufSize]
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
                        if (curCell % 2 == 0) continue;
                        var pos = new Vector2Int(i, j);

                        var loc = new ChLoc { board = map.board, pos = pos };
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
                if (gameMode == GameMode.PlayerVsPC && whoseMove == 0) {
                    var allPaths = AIController.GetAIPaths(
                        possibleGraphs,
                        bufSize,
                        whoseMove,
                        map.board
                    );
                    bestAiPath = AIController.GetBestPath(allPaths, map.board);

                    for (int i = 0; i < bestAiPath.path.Count - 1; i++)
                    {
                        var curPos = bestAiPath.path[i];
                        var nextPos = bestAiPath.path[i + 1];
                        Debug.Log(curPos + " " + nextPos);
                        map.board[nextPos.x, nextPos.y] = map.board[curPos.x, curPos.y];
                        map.board[curPos.x, curPos.y] = 0;

                        var worldPos = ConvertToWorldPoint(nextPos);
                        map.obj[curPos.x, curPos.y].transform.localPosition = worldPos;
                        map.obj[nextPos.x, nextPos.y] = map.obj[curPos.x, curPos.y];

                        var dir = nextPos - curPos;
                        var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                        for (var next = curPos + nDir; next != nextPos; next += nDir) {
                            if (map.board[next.x, next.y] != 0) {
                                if (!sentenced.Contains(next)) sentenced.Add(next);
                            }
                        }
                    }

                    foreach (var sent in sentenced) {
                        Destroy(map.obj[sent.x, sent.y]);
                        map.board[sent.x, sent.y] = 0;
                    }

                    sentenced.Clear();
                    whoseMove = (whoseMove + 2) % 4;
                    needRefreshBuffer = true;
                }
            }

            Vector2Int clickPos = new Vector2Int();
            #if UNITY_EDITOR || UNITY_STANDALONE_WIN
                if (!Input.GetMouseButtonDown(0)) return;
                var ray = camera.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
                clickPos = ConvertToBoardPoint(hit.point);
            #endif

            #if UNITY_ANDROID && !UNITY_EDITOR
                Touch touch;
                if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began) {
                    return;
                }
                if (Physics.Raycast(camera.ScreenPointToRay(touch.position), out RaycastHit hit2)) {
                    Debug.Log(hit2.point);
                    clickPos = ConvertToBoardPoint(hit2.point);
                }
                // if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began) {
                //     return;
                // }
                // raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon);
                // if (hits.Count > 0) {
                //     Debug.Log(hits[0].pose.position);
                //     clickPos = ConvertToBoardPoint(hits[0].pose.position);
                //     hits.Clear();
                // }
            #endif

            var needAttack = IsNeedAttack(possibleGraphs, bufSize);

            var cell = Checkers.GetCell(map.board, clickPos);

            if (!secondMove) DestroyHighlightCells(storageHighlightCells.transform);

            var color = cell & Checkers.WHITE;

            if (cell % 2 != 0 && color == whoseMove && !secondMove) {
                var graph = new PossibleGraph();
                var size = 0;

                var isBadPos = true;
                var hasMove = false;
                for (int i = 0; i < checkersCount; i++) {
                    if (possibleGraphs[i].cells[0] == clickPos) {
                        Debug.Log(bufSize[i]);
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
                                var isFilled = curCell % 2 != 0;
                                if (isFilled && (curCell & (int)whoseMove) == 0) break;

                                var point = curGraph.cells[0];
                                var pos = ConvertToWorldPoint(point) - new Vector3(0, 0.1f, 0);
                                if (res.highlightCh == null) {
                                    Debug.LogError("NoHighlightCh");
                                } else {
                                    var parent = storageHighlightCells.transform;
                                    var obj = Instantiate(res.highlightCh, pos, Quaternion.identity, parent);
                                    obj.transform.localPosition = pos;
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
                        curMark = graph.connect[curInd, i];
                        if ((graph.marks[i] & curMark) > 0) {
                            graph.marks[i] -= curMark;
                            isBadPos = false;
                            break;
                        }
                    }
                }
                if (isBadPos) return;

                var dir = clickPos - lPos;
                var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                for (var next = lPos + nDir; next != clickPos; next += nDir) {
                    if (map.board[next.x, next.y] != 0) {
                        if (!sentenced.Contains(next)) {
                            sentenced.Add(next);
                            break;
                        }
                        return;
                    }
                }
                badDir = nDir;

                map.board[clickPos.x, clickPos.y] = map.board[lPos.x, lPos.y];
                map.board[lPos.x, lPos.y] = 0;

                var worldPos = ConvertToWorldPoint(clickPos);
                map.obj[lPos.x, lPos.y].transform.localPosition = worldPos;
                map.obj[clickPos.x, clickPos.y] = map.obj[lPos.x, lPos.y];

                var edgeBoard = 0;
                var ch = map.board[clickPos.x, clickPos.y];
                if (ch != 0 && (ch & Checkers.WHITE) == 0) {
                    edgeBoard = boardInfo.boardSize.x - 1;
                }

                if (clickPos.x == edgeBoard) {
                    var king = Checkers.KING + (int)whoseMove + 1;
                    map.board[clickPos.x, clickPos.y] = king;
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
                        map.board[sent.x, sent.y] = 0;
                    }

                    sentenced.Clear();
                    whoseMove = (whoseMove + 2) % 4;
                    curMark = 0;
                    badDir = Vector2Int.zero;
                    return;
                }

                secondMove = true;
                HighlightCells(graph, count, clickPos);
                lastPos = Option<Vector2Int>.Some(clickPos);
            }
        }

        private void Move(Vector2Int clickPos) {
            
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

                    var b = Instantiate(res.highlightCell, spawnWorldPos, Quaternion.identity, parent);
                    b.transform.localPosition = spawnWorldPos;
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
            map.board = new int[boardInfo.boardSize.x, boardInfo.boardSize.y];
            for (int i = 0; i < parseRes.rows.Count; i++) {
                for (int j = 0; j < parseRes.rows[0].Count; j++) {
                    if (parseRes.rows[i][j] == "WhoseMove") {
                        if (int.TryParse(parseRes.rows[i][j + 1], out result)) {
                            whoseMove = result;
                        }
                        break;
                    }
                    var color = 2;
                    var type = 0;
                    if (int.TryParse(parseRes.rows[i][j], out int res)) {
                        if (res % 2 != 0) color = 0;
                        if (res > 1) type = 4;
                        int checker = color + type + 1;
                        map.board[i, j] = checker;
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
            #if UNITY_EDITOR || UNITY_STANDALONE_WIN
                path = System.IO.Path.Combine(Application.streamingAssetsPath, path);
                Load(path);
                return;
            #elif UNITY_ANDROID
                map.obj = new GameObject[boardInfo.boardSize.x, boardInfo.boardSize.y];
                map.board = new int[boardInfo.boardSize.x, boardInfo.boardSize.y];
                whoseMove = 2;
                chKind = ChKind.Russian;
                for (int i = 0; i < 8; i++) {
                    for (int j = 0; j < 8; j++) {
                        if (i < 3 && (j + i) % 2 != 0)
                        {
                            var type = 0;
                            var color = 0;
                            int checker = color + type + 1;
                            map.board[i, j] = checker;
                        }
                        if (i > 4 && (j + i) % 2 != 0)
                        {
                            var type = 0;
                            var color = 2;
                            int checker = color + type + 1;
                            map.board[i, j] = checker;
                        }

                    }
                }

                DestroyHighlightCells(storageHighlightCells.transform);
                needRefreshBuffer = true;
                sentenced.Clear();
                selected = Option<Vector2Int>.None();
                SpawnCheckers(map.board);
                return;
            #endif
        }

        public void Save() {
            var path = System.IO.Path.Combine(Application.persistentDataPath, Guid.NewGuid() + ".save");

            if (map.board == null) {
                return;
            }

            var rows = new List<List<string>>();
            for (int i = 0; i < map.board.GetLength(1); i++) {
                rows.Add(new List<string>());
                for (int j = 0; j < map.board.GetLength(0); j++) {
                    string cellInf = "-";
                    if (map.board[i, j] != 0) {
                        var checker = map.board[i, j];
                        switch (checker)
                        {
                            case 3:
                                checker = 0;
                                break;
                            case 5:
                                checker = 3;
                                break;
                            case 7:
                                checker = 2;
                                break;
                        }
                        cellInf = checker.ToString();
                    }
                    rows[i].Add(cellInf);
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

            saveInfo.board = new int[10, 10];
            if (parseRes.rows.Count < 10) {
                saveInfo.board = new int[8, 8];
            }
            for (int i = 0; i < parseRes.rows.Count; i++) {
                if (parseRes.rows[i][0] == "WhoseMove") {
                    if (int.TryParse(parseRes.rows[i][1], out int res)) {
                        saveInfo.whoseMove = res;
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
                        var color = 2;
                        var type = 0;
                        if (res % 2 != 0) color = 0;
                        if (res > 1) type = 4;

                        var checker = (int)type + (int)color + 1;
                        saveInfo.board[i,j] = checker;
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
            var offset = boardInfo.boardTransform.localPosition;
            var size = boardInfo.cellTransform.localScale;
            var floatVec = new Vector3(boardPoint.x, 0.1f, boardPoint.y);
            var cellLoc = boardInfo.cellTransform.localPosition;
            var point = size.x * floatVec - new Vector3(cellLoc.x, 0, cellLoc.z) + size / 2f;
            point.x += offset.x;
            point.z += offset.z;
            point.y += offset.y;

            return point;
        }

        private Vector2Int ConvertToBoardPoint(Vector3 selectedPoint) {
            var inversePoint = boardInfo.boardTransform.InverseTransformPoint(selectedPoint);
            var pos = boardInfo.cellTransform.localPosition;
            var size = boardInfo.cellTransform.localScale;
            var floatVec = (inversePoint + new Vector3(pos.x, 0, pos.z)) / size.x;
            var point = new Vector2Int(Mathf.Abs((int)(floatVec.x)), Mathf.Abs((int)floatVec.z));

            return point;
        }

        private bool SpawnCheckers(int[,] board) {
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
                    if (board[i, j] == 0) {
                        continue;
                    }

                    var checker = board[i, j];
                    var pref = res.blackChecker;
                    if ((checker & Checkers.WHITE) > 0) {
                        pref = res.whiteChecker;
                    }

                    var spawnWorldPos = ConvertToWorldPoint(new Vector2Int(i, j));
                    var parent = boardInfo.boardTransform;
                    map.obj[i, j] = Instantiate(pref, spawnWorldPos, Quaternion.identity, parent);
                    map.obj[i, j].transform.localPosition = spawnWorldPos;
                    if ((checker & Checkers.KING) > 0) {
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

                var ch = map.board[graph.cells[0].x, graph.cells[0].y];
                if (HasAttack(graph, count) && ch != 0 && (ch & (int)whoseMove) > 0) {
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
                        if (map.board[next.x, next.y] != 0) return true;
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
                var ch = map.board[pos.x, pos.y];
                if (ch == 0) continue;

                if ((ch & (int)whoseMove) > 0) {
                    return false;
                }
            }

            return true;
        }
    }
}