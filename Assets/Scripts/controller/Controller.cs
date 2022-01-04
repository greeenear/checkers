using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using checkers;
using option;
using UnityEngine.Events;
using UnityEditor;
using ai;

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

        private Dictionary<Vector2Int, Dictionary<Vector2Int, bool>> allCheckerMoves;
        private Dictionary<Vector2Int, (PossibleGraph, int)> allCheckersMatrix;
        private HashSet<Vector2Int> sentenced;
        private Option<Vector2Int> selected;
        private Option<Vector2Int> lastPos;
        private bool secondMove;

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

            boardInfo = res.board8x8;
            map.board = new Option<Checker>[boardInfo.boardSize.x, boardInfo.boardSize.y];
            map.obj = new GameObject[boardInfo.boardSize.x, boardInfo.boardSize.y];
            sentenced = new HashSet<Vector2Int>();
        }

        // private void Update() {
        //     if (res == null) {
        //         Debug.LogError("CantGetResources");
        //         this.enabled = false;
        //         return;
        //     }
        //     if (allCheckerMoves == null) {
        //         allCheckerMoves = new Dictionary<Vector2Int, Dictionary<Vector2Int, bool>>();
        //         var size = boardInfo.boardSize;
        //         for (int i = 0; i < map.board.GetLength(0); i++) {
        //             for (int j = 0; j < map.board.GetLength(1); j++) {

        //                 var cellOpt = map.board[i, j];
        //                 if (cellOpt.IsNone()) continue;
        //                 var curCh = cellOpt.Peel();

        //                 var xDir = 1;
        //                 if (curCh.color == ChColor.White) {
        //                     xDir = -1;
        //                 }

        //                 var pos = new Vector2Int(i, j);
        //                 var checkerMoves = new Dictionary<Vector2Int, bool>();
        //                 foreach (var dir in res.directions) {
        //                     var chFound = false;
        //                     for (var next = pos + dir; IsOnBoard(size, next); next += dir) {
        //                         var nextOpt = map.board[next.x, next.y];
        //                         if (nextOpt.IsSome()) {
        //                             var nextColor = nextOpt.Peel().color;
        //                             var isSentenced = sentenced.Contains(next);
        //                             if (isSentenced || chFound || nextColor == curCh.color) break;

        //                             chFound = true;
        //                         } else {
        //                             var wrongMove = curCh.type == ChType.Checker && dir.x != xDir;
        //                             switch (chKind) {
        //                                 case ChKind.Pool:
        //                                 case ChKind.Russian:
        //                                 case ChKind.International:
        //                                     wrongMove = wrongMove && !chFound;
        //                                     break;
        //                             }

        //                             if (!wrongMove) {
        //                                 checkerMoves.Add(next, chFound);
        //                             }

        //                             if (curCh.type == ChType.Checker || chKind == ChKind.English) {
        //                                 break;
        //                             }
        //                         }
        //                     }
        //                 }
        //                 allCheckerMoves.Add(pos, checkerMoves);
        //             }
        //         }
        //     }

        //     if (IsGameOver()) {
        //         onGameOver?.Invoke();
        //         this.enabled = false;
        //         return;
        //     }

        //     if (!Input.GetMouseButtonDown(0)) return;

        //     var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //     if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        //     DestroyHighlightCells(storageHighlightCells.transform);
        //     var clickPos = ConvertToBoardPoint(hit.point);

        //     var checkerOpt = map.board[clickPos.x, clickPos.y];

        //     var isAttack = IsNeedAttack(allCheckerMoves);
        //     if (checkerOpt.IsSome()) {
        //         if (!allCheckerMoves.ContainsKey(clickPos)) return;
        //         var curMoves = allCheckerMoves[clickPos];
        //         var isDifColor = checkerOpt.Peel().color != whoseMove;
        //         var cantAttack = isAttack && !HasAttack(curMoves);

        //         if (curMoves.Count == 0 || cantAttack || isDifColor) {
        //             foreach (var checker in allCheckerMoves) {
        //                 if (checker.Value.Count != 0) {
        //                     var curChOpt = map.board[checker.Key.x, checker.Key.y];
        //                     if (curChOpt.IsNone() || curChOpt.Peel().color != whoseMove) continue;

        //                     if (isAttack && !HasAttack(checker.Value)) {
        //                         continue;
        //                     }

        //                     var parent = storageHighlightCells.transform;
        //                     var pos = ConvertToWorldPoint(checker.Key) - new Vector3(0, 0.1f, 0);
        //                     if (res.highlightCh == null) {
        //                         Debug.LogError("NoHighlightCh");
        //                     } else {
        //                         Instantiate(res.highlightCh, pos, Quaternion.identity, parent);
        //                     }
        //                 }
        //             }
        //         }
        //         if (checkerOpt.Peel().color != whoseMove) return;

        //         selected = Option<Vector2Int>.Some(clickPos);
        //         HighlightCells(allCheckerMoves[clickPos], isAttack);
        //     } else if (selected.IsSome()) {
        //         var curPos = selected.Peel();
        //         if (map.board[curPos.x, curPos.y].IsNone()) return;
        //         var curCh = map.board[curPos.x, curPos.y].Peel();
        //         var origCurCh = curCh;

        //         var curChMoves = allCheckerMoves[curPos];
        //         if (!curChMoves.ContainsKey(clickPos)) {
        //             selected = Option<Vector2Int>.None();
        //             return;
        //         }

        //         var isClickAttack = curChMoves[clickPos];
        //         if (!isClickAttack && isAttack) return;

        //         map.board[clickPos.x, clickPos.y] = map.board[curPos.x, curPos.y];
        //         map.board[curPos.x, curPos.y] = Option<Checker>.None();
        //         allCheckerMoves.Clear();

        //         var worldPos = ConvertToWorldPoint(clickPos);
        //         map.obj[curPos.x, curPos.y].transform.position = worldPos;
        //         map.obj[clickPos.x, clickPos.y] = map.obj[curPos.x, curPos.y];

        //         var edgeBoard = 0;
        //         if (curCh.color == ChColor.Black) {
        //             edgeBoard = boardInfo.boardSize.x - 1;
        //         }

        //         var dir = clickPos - curPos;
        //         var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
        //         for (var next = curPos + nDir; next != clickPos; next += nDir) {
        //             if (map.board[next.x, next.y].IsSome()) {
        //                 sentenced.Add(next);
        //             }
        //         }

        //         var onEdgeBoard = clickPos.x == edgeBoard;
        //         if (onEdgeBoard && !(chKind == ChKind.International && sentenced.Count != 0)) {
        //             var king = new Checker { type = ChType.King, color = whoseMove };
        //             map.board[clickPos.x, clickPos.y] = Option<Checker>.Some(king);
        //             var reverse = Quaternion.Euler(180, 0, 0);
        //             map.obj[clickPos.x, clickPos.y].transform.rotation = reverse;
        //             curCh = king;
        //         }

        //         var secondMoveInfos = new Dictionary<Vector2Int, bool>();
        //         var secondMove = chKind != ChKind.Pool || !onEdgeBoard;
        //         var anySentenced = sentenced.Count != 0;
        //         if (anySentenced && secondMove) {
        //             var xDir = 1;
        //             if (curCh.color == ChColor.White) {
        //                 xDir = -1;
        //             }

        //             var size = boardInfo.boardSize;
        //             foreach (var moveDir in res.directions) {
        //                 var last = clickPos + moveDir;
        //                 var chFound = false;
        //                 for (last = clickPos + moveDir; IsOnBoard(size, last); last += moveDir) {
        //                     var nextOpt = map.board[last.x, last.y];
        //                     if (nextOpt.IsSome()) {
        //                         var nextColor = nextOpt.Peel().color;
        //                         var isSentenced = sentenced.Contains(last);

        //                         if (isSentenced || chFound || nextColor == curCh.color) break;
        //                         chFound = true;
        //                     } else {
        //                         var wrongMove = curCh.type == ChType.Checker && dir.x != xDir;
        //                         switch (chKind) {
        //                             case ChKind.Pool:
        //                             case ChKind.Russian:
        //                             case ChKind.International:
        //                                 wrongMove = wrongMove && !chFound;
        //                                 break;
        //                         }

        //                         if (!wrongMove && chFound) {
        //                             secondMoveInfos.Add(last, chFound);
        //                         }

        //                         if (curCh.type == ChType.Checker || chKind == ChKind.English) {
        //                             break;
        //                         }
        //                     }
        //                 }
        //             }
        //         }

        //         if (secondMoveInfos.Count != 0) {
        //             allCheckerMoves.Add(clickPos, secondMoveInfos);
        //             HighlightCells(secondMoveInfos, true);
        //             selected = Option<Vector2Int>.Some(clickPos);
        //         }  else {
        //             if (onEdgeBoard && chKind == ChKind.International) {
        //                 var king = new Checker { type = ChType.King, color = whoseMove };
        //                 map.board[clickPos.x, clickPos.y] = Option<Checker>.Some(king);
        //                 var reverse = Quaternion.Euler(180, 0, 0);
        //                 map.obj[clickPos.x, clickPos.y].transform.rotation = reverse;
        //                 curCh = king;
        //             }

        //             foreach (var sentencedPos in sentenced) {
        //                 map.board[sentencedPos.x, sentencedPos.y] = Option<Checker>.None();
        //                 Destroy(map.obj[sentencedPos.x, sentencedPos.y]);
        //             }

        //             selected = Option<Vector2Int>.None();
        //             sentenced.Clear();
        //             allCheckerMoves = null;
        //             whoseMove = (ChColor)((int)(whoseMove + 1) % (int)ChColor.Count);
        //         }
        //     }
        // }

        private void Update() {
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }

            if (allCheckersMatrix == null) {
                allCheckersMatrix = new Dictionary<Vector2Int, (PossibleGraph, int)>();
                for (int i = 0; i < map.board.GetLength(0); i++) {
                    for (int j = 0; j < map.board.GetLength(1); j++) {
                        var cellOpt = map.board[i, j];
                        if (cellOpt.IsNone()) continue;
                        var curCh = cellOpt.Peel();

                        var pos = new Vector2Int(i, j);
                        var matrix = new int[20,20];
                        var nodes = new Vector2Int[20];
                        var marks = new int[20];

                        var buffer = new checkers.PossibleGraph {
                            connect = matrix,
                            cells = nodes,
                            marks = marks
                        };

                        var loc = new ChLocation { board = map.board, pos = pos };
                        var count = Checkers.GetPossiblePaths(loc, chKind, buffer);
                        allCheckersMatrix.Add(pos, (buffer, count));
                    }
                }
            }

            if (!Input.GetMouseButtonDown(0)) return;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
            var needAttack = IsNeedAttack(allCheckersMatrix);

            var clickPos = ConvertToBoardPoint(hit.point);
            var checkerOpt = map.board[clickPos.x, clickPos.y];
            if (!secondMove) DestroyHighlightCells(storageHighlightCells.transform);

            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove && !secondMove) {
                if (!allCheckersMatrix.ContainsKey(clickPos)) return;
                selected = Option<Vector2Int>.Some(clickPos);
                lastPos = Option<Vector2Int>.Some(clickPos);
                var buf = allCheckersMatrix[clickPos];
                if (needAttack && !HasAttack(buf.Item1, buf.Item2)) {
                    selected = Option<Vector2Int>.None();
                    return;
                }

                Checkers.ShowMatrix(buf.Item1);
                HighlightCells(buf.Item1, clickPos);
            } else if (selected.IsSome()) {
                var curPos = selected.Peel();
                var lPos = lastPos.Peel();

                var buf = allCheckersMatrix[curPos];

                var curPosInd = Array.IndexOf<Vector2Int>(buf.Item1.cells, lPos);
                if (curPosInd == -1) return;

                var isBadPos = true;
                var count = buf.Item2;
                for (int i = 0; i < count; i++) {
                    if (buf.Item1.connect[curPosInd, i] != 0 && buf.Item1.cells[i] == clickPos) {
                        isBadPos = false;
                    }
                }
                if (isBadPos) return;

                map.board[clickPos.x, clickPos.y] = map.board[lPos.x, lPos.y];
                map.board[lPos.x, lPos.y] = Option<Checker>.None();
                var worldPos = ConvertToWorldPoint(clickPos);
                map.obj[lPos.x, lPos.y].transform.position = worldPos;
                map.obj[clickPos.x, clickPos.y] = map.obj[lPos.x, lPos.y];

                var dir = clickPos - lPos;
                var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                for (var next = lPos + nDir; next != clickPos; next += nDir) {
                    if (map.board[next.x, next.y].IsSome()) sentenced.Add(next);
                }

                curPosInd = Array.IndexOf<Vector2Int>(buf.Item1.cells, clickPos);
                var nextMove = false;
                for (int i = 0; i < buf.Item2; i++) {
                    if (buf.Item1.connect[curPosInd, i] != 0) {
                        nextMove = true;
                    }
                }
                if (!nextMove) {
                    secondMove = false;
                    allCheckersMatrix = null;
                    selected = Option<Vector2Int>.None();
                    foreach (var sent in sentenced) {
                        Destroy(map.obj[sent.x, sent.y]);
                        map.board[sent.x, sent.y] = Option<Checker>.None();
                    }
                    sentenced.Clear();
                    DestroyHighlightCells(storageHighlightCells.transform);
                    whoseMove = (ChColor)((int)(whoseMove + 1) % (int)ChColor.Count);
                    return;
                }

                secondMove = true;
                DestroyHighlightCells(storageHighlightCells.transform);
                HighlightCells(buf.Item1, clickPos);
                lastPos = Option<Vector2Int>.Some(clickPos);
            }
        }

        private void HighlightCells(checkers.PossibleGraph buffer, Vector2Int targetPos) {
            var index = Array.IndexOf<Vector2Int>(buffer.cells, targetPos);
            for (int k = 0; k < buffer.connect.GetLength(1); k++) {
                if (buffer.connect[index, k] != 0) {
                    var cellPos = buffer.cells[k];
                    var boardPos = boardInfo.boardTransform.transform.position;
                    var spawnWorldPos = ConvertToWorldPoint(cellPos);
                    var parent = storageHighlightCells.transform;

                    Instantiate(
                        res.highlightCell,
                        spawnWorldPos,
                        Quaternion.identity,
                        parent
                    );
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

            allCheckersMatrix = null;
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

        private bool IsNeedAttack(Dictionary<Vector2Int, (PossibleGraph, int)> checkersMoves) {
            foreach (var moves in checkersMoves) {
                var graph = moves.Value.Item1;
                var count = moves.Value.Item2;
                if (HasAttack(graph, count)) return true;
            }

            return false;
        }

        private bool HasAttack(checkers.PossibleGraph movesInfo, int count) {
            for (int i = 0; i < count; i++) {
                if (movesInfo.connect[0, i] != 0) {
                    if (Mathf.Abs((movesInfo.cells[i].x - movesInfo.cells[0].x)) > 1) {
                        return true;
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
            if (allCheckerMoves == null) {
                return false;
            }

            foreach (var checker in allCheckerMoves) {
                var chOpt = map.board[checker.Key.x, checker.Key.y];
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