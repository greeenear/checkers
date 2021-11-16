using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
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
        public static Action successfulSaving;
        public static Action unsuccessfulSaving;
        public GameObject storageHighlightCells;
        public GameObject mainMenu;
        public GameObject saveTemplatesStorage;
        public Button saveBut;
        public GameObject loadMenu;

        public GameObject boardImage10x10;
        public GameObject boardImage8x8;
        public RawImage whiteCheckerImage;
        public RawImage blackCheckerImage;
        public GameObject emptyCell;

        private Resources res;
        private BoardInfo boardInfo;
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
            if (res.whiteChecker == null) {
                Debug.LogError("NoCheckers");
                this.enabled = false;
                return;
            }
            if (storageHighlightCells == null) {
                Debug.LogError("NoStorageHighlightCells");
                this.enabled = false;
                return;
            }
            if (res.highlightCell == null) {
                Debug.LogError("NoHighlightCells");
                this.enabled = false;
                return;
            }
            if (loadMenu == null) {
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
            boardInfo = res.board8x8;
            map.board = new Option<Checker>[boardInfo.boardSize.x, boardInfo.boardSize.y];
            map.obj = new GameObject[boardInfo.boardSize.x, boardInfo.boardSize.y];
            sentenced = new HashSet<Vector2Int>();
            saveBut.onClick.AddListener(() => Save(
                    Path.Combine(
                        Application.persistentDataPath,
                        Guid.NewGuid().ToString() + ".save"
                    )
                )
            );
        }

        private void Update() {
            if (allCheckerMoves == null) {
                allCheckerMoves = new Dictionary<Vector2Int, Dictionary<Vector2Int, bool>>();
                var size = boardInfo.boardSize;
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

            if (!Input.GetMouseButtonDown(0)) return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            DestroyHighlightCells(storageHighlightCells.transform);
            var cliсkPos = ConvertToBoardPoint(hit.point);

            var checkerOpt = map.board[cliсkPos.x, cliсkPos.y];

            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                if (!allCheckerMoves.ContainsKey(cliсkPos)) return;
                selected = Option<Vector2Int>.Some(cliсkPos);

                HighlightCells(allCheckerMoves[cliсkPos], IsNeedAttack(allCheckerMoves));
            } else if (selected.IsSome()) {
                var curPos = selected.Peel();
                if (map.board[curPos.x, curPos.y].IsNone()) return;
                var curCh = map.board[curPos.x, curPos.y].Peel();
                var origCurCh = curCh;

                var curChMoves = allCheckerMoves[curPos];
                if (!curChMoves.ContainsKey(cliсkPos)) {
                    selected = Option<Vector2Int>.None();
                    return;
                }

                var isClickAttack = curChMoves[cliсkPos];
                if (!isClickAttack && IsNeedAttack(allCheckerMoves)) return;

                map.board[cliсkPos.x, cliсkPos.y] = map.board[curPos.x, curPos.y];
                map.board[curPos.x, curPos.y] = Option<Checker>.None();
                allCheckerMoves.Clear();

                var worldPos = ConvertToWorldPoint(cliсkPos);
                map.obj[curPos.x, curPos.y].transform.position = worldPos;
                map.obj[cliсkPos.x, cliсkPos.y] = map.obj[curPos.x, curPos.y];

                var edgeBoard = 0;
                if (curCh.color == Color.Black) {
                    edgeBoard = boardInfo.boardSize.x - 1;
                }


                var dir = cliсkPos - curPos;
                var nDir = new Vector2Int(dir.x / Mathf.Abs(dir.x), dir.y / Mathf.Abs(dir.y));
                for (var next = curPos + nDir; next != cliсkPos; next += nDir) {
                    if (map.board[next.x, next.y].IsSome()) {
                        sentenced.Add(next);
                    }
                }

                var onEdgeBoard = cliсkPos.x == edgeBoard;
                if (onEdgeBoard && !(chKind == ChKind.International && sentenced.Count != 0)) {
                    var king = new Checker { type = Type.King, color = whoseMove };
                    map.board[cliсkPos.x, cliсkPos.y] = Option<Checker>.Some(king);
                    var reverse = Quaternion.Euler(180, 0, 0);
                    map.obj[cliсkPos.x, cliсkPos.y].transform.rotation = reverse;
                    curCh = king;
                }

                var secondMoveInfos = new Dictionary<Vector2Int, bool>();
                var secondMove = chKind != ChKind.Pool || !onEdgeBoard;
                var anySentenced = sentenced.Count != 0;
                if (anySentenced && secondMove) {
                    var xDir = 1;
                    if (curCh.color == Color.White) {
                        xDir = -1;
                    }

                    var size = boardInfo.boardSize;
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
                }  else {
                    if (onEdgeBoard && chKind == ChKind.International) {
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
            var boardPos = boardInfo.boardTransform.transform.position;
            foreach (var pos in moves) {
                if (attack && pos.Value || !attack && !pos.Value) {
                    var spawnWorldPos = ConvertToWorldPoint(pos.Key);
                    var parent = storageHighlightCells.transform;
                    Instantiate(res.highlightCell, spawnWorldPos, Quaternion.identity, parent);
                }
            }

            return ControllerErrors.None;
        }

        private Vector2Int ConvertToBoardPoint(Vector3 selectedPoint) {
            var inversePoint = boardInfo.boardTransform.InverseTransformPoint(selectedPoint);
            var pos = boardInfo.cellTransform.localPosition;
            var size = boardInfo.cellTransform.localScale;
            var floatVec = (inversePoint + new Vector3(-pos.x, 0, pos.z)) / size.x;
            var point = new Vector2Int(Mathf.Abs((int)(floatVec.z)), Mathf.Abs((int)floatVec.x));

            return point;
        }

        public void Load(string path) {
            if (mainMenu.activeSelf) {
                path = Path.Combine(Application.streamingAssetsPath, path);
            }
            string input;
            try {
                var fstream = File.OpenRead(path);
                var bytes = new byte[fstream.Length];
                fstream.Read(bytes, 0, bytes.Length);
                input = System.Text.Encoding.Default.GetString(bytes);
            } catch (Exception err) {
                Debug.LogError("CantLoad");
                Debug.LogError(err.ToString());
                return;
            }

            var parseRes = CSV.Parse(input);
            if (parseRes.error != CSV.ErrorType.None) {
                Debug.LogError(parseRes.error.ToString());
            }

            if (int.TryParse(parseRes.rows[parseRes.rows.Count - 1][3], out int result)) {
                chKind = (ChKind)result;
                if (chKind.ToString() == "International") {
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
                            whoseMove = (Color)result;
                        }
                        break;
                    }
                    var color = Color.White;
                    var type = Type.Checker;
                    if (int.TryParse(parseRes.rows[i][j], out int res)) {
                        if (res % 2 != 0) color = Color.Black;
                        if (res > 1) type = Type.King;
                        var checker = new Checker { color =color, type = type };
                        map.board[i, j] = Option<Checker>.Some(checker);
                    }
                }
            }

            DestroyHighlightCells(storageHighlightCells.transform);

            SpawnCheckers(map.board);
            allCheckerMoves = null;
            sentenced.Clear();
            selected = Option<Vector2Int>.None();
            loadMenu.SetActive(false);
            enabled = true;
        }

        public void Save(string path) {
            var rows = new List<List<string>>();
            var fileName = path.Replace(Application.persistentDataPath, "").Replace(".csv", "");
            for (int i = 0; i < map.board.GetLength(1); i++) {
                rows.Add(new List<string>());
                for (int j = 0; j < map.board.GetLength(0); j++) {
                    if (map.board[i, j].IsNone()) {
                        rows[i].Add("-");
                    }

                    if (map.board[i, j].IsSome()) {
                        var checker = map.board[i, j].Peel();
                        if (checker.color == Color.Black) {
                            rows[i].Add("1");
                        } else if (checker.color == Color.White) {
                            rows[i].Add("0");
                        }
                    }
                }
            }
            rows.Add(
                new List<string>() {
                    "WhoseMove",
                    ((int)whoseMove).ToString(),
                    "ChKind",
                    ((int)chKind).ToString(),
                    "name",
                    fileName
                }
            );

            string output = CSV.Generate(rows);
            try {
                File.WriteAllText(path, output);
                successfulSaving?.Invoke();
            } catch (Exception err) {
                unsuccessfulSaving?.Invoke();
                Debug.LogError(err.ToString());
                return;
            }

            loadMenu.SetActive(false);
            this.enabled = true;
        }

        private void FillLoadMenu() {
            string[] allfiles;
            try {
                allfiles = Directory.GetFiles(Application.persistentDataPath, "*.save");
            } catch (Exception err) {
                Debug.LogError("NoDirectory");
                Debug.LogError(err.ToString());
                return;
            }

            foreach (string filename in allfiles) {
                var curObj = Instantiate(
                    res.loadTemplate,
                    new Vector3(),
                    Quaternion.identity,
                    saveTemplatesStorage.transform
                );

                var fstream = File.OpenRead(filename);
                var bytes = new byte[fstream.Length];
                fstream.Read(bytes, 0, bytes.Length);
                string input = System.Text.Encoding.Default.GetString(bytes);
                var parseRes = CSV.Parse(input);

                var imageBoardPrefab = boardImage10x10;
                if (parseRes.rows.Count < 10) {
                    imageBoardPrefab = boardImage8x8;
                }

                string saveDate = "";
                string whoseMoveInfo = "";
                string checkerKindInfo = "";

                var parent = curObj.transform.GetChild(5).transform;
                var boardGrid = Instantiate(imageBoardPrefab, parent).transform.GetChild(0);
                foreach (var row in parseRes.rows) {
                    foreach (var cell in row) {
                        if (cell == "-") {
                            Instantiate(emptyCell, boardGrid);
                        } else if (cell == "0") {
                            Instantiate(whiteCheckerImage, boardGrid);
                        } else if (cell == "1") {
                            Instantiate(blackCheckerImage, boardGrid);
                        }
                    }
                    if (row[0] == "WhoseMove") {
                        if (int.TryParse(row[1], out int res)) {
                            whoseMoveInfo += "Move: " + ((Color)res).ToString();
                        }
                    }
                    if (row[2] == "ChKind") {
                        if (int.TryParse(row[3], out int res)) {
                            checkerKindInfo += "Checker Kind: " + ((ChKind)res).ToString();
                        }
                    }
                    if (row[4] == "name") {
                        saveDate += "Save Date: " + row[5].Replace(@"\", "");
                    }
                }

                foreach (Transform child in curObj.transform) {
                    if (child.gameObject.TryGetComponent(out Button but)) {
                        if (but.name == "Load") {
                            but.onClick.AddListener(() => Load(filename));
                        } else {
                            but.onClick.AddListener(() => {
                                    File.Delete(filename);
                                    Destroy(curObj);
                                }
                            );
                        }
                    } else if (child.gameObject.TryGetComponent(out Text text)) {
                        if (text.name == "Date") {
                            text.text = saveDate;
                        } else if (text.name == "WhoseMove"){
                            text.text = whoseMoveInfo;
                        } else if (text.name == "Kind") {
                            text.text = checkerKindInfo;
                        }
                    }
                }
            }
        }

        private Vector3 ConvertToWorldPoint(Vector2Int boardPoint) {
            var size = boardInfo.cellTransform.localScale;
            var floatVec = new Vector3(boardPoint.x, 0.1f, boardPoint.y);
            var cellLoc = boardInfo.cellTransform.localPosition;
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
                        var spawnWorldPos = ConvertToWorldPoint(pos);
                        map.obj[i, j] = Instantiate(
                            prefab,
                            spawnWorldPos,
                            Quaternion.identity,
                            boardInfo.boardTransform
                        );
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

        public void OpenMenu() {
            foreach (Transform child in saveTemplatesStorage.transform) {
                Destroy(child.gameObject);
            }
            FillLoadMenu();

            if (loadMenu.activeSelf == true) {
                loadMenu.SetActive(false);
                this.enabled = true;
            } else {
                loadMenu.SetActive(true);
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