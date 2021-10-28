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

    public struct CheckerInfo {
        public bool isNeedAttack;
        public List<Vector2Int> moves;
    }

    public struct MoveRes {
        public Vector2Int? sentensed;
        public bool secondMove;
    }

    public class Controller : MonoBehaviour {
        private Resources res;

        private Option<Checker>[,] board = new Option<Checker>[8, 8];
        private GameObject[,] boardObj = new GameObject[8, 8];
        private Dictionary<Vector2Int, CheckerInfo> checkerInfos;
        private bool isNeedAttack;
        private Vector2Int selectedChecker;
        private List<Vector2Int> directions = new List<Vector2Int>();

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
        }

        private void Start() {
            FillBoard(board);
            SpawnCheckers(board);
            directions.Add(new Vector2Int(1, 1));
            directions.Add(new Vector2Int(1, -1));
            directions.Add(new Vector2Int(-1, 1));
            directions.Add(new Vector2Int(-1, -1));
        }

        private void Update() {
            if (!Input.GetMouseButtonDown(0)) {
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) {
                return;
            }

            var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));

            if (checkerInfos == null) {
                checkerInfos = new Dictionary<Vector2Int, CheckerInfo>();
                var maxLength = 1;
                var xDir = 1;
                for (int i = 0; i < board.GetLength(0); i++) {
                    for (int j = 0; j < board.GetLength(1); j++) {
                        var cellOpt = board[i, j];
                        if (cellOpt.IsNone()) {
                            continue;
                        }

                        if (cellOpt.IsSome() && cellOpt.Peel().color != whoseMove) {
                            continue;
                        }

                        var cell = cellOpt.Peel();
                        if (cell.type == Type.King) {
                            var max = Mathf.Max(boardSize.x, boardSize.y);
                            maxLength = max;
                        } else if (cell.type == Type.Checker && cell.color == Color.White) {
                            xDir = -1;
                        }

                        var pos = new Vector2Int(i, j);
                        var checkerInfo = new CheckerInfo();
                        var moves = new List<Vector2Int>();
                        foreach (var dir in directions) {
                            var (length, err) = GetLengthToObject(board, pos, dir, maxLength);
                            if (err != ControllerErrors.None) {
                                Debug.LogError($"CantGetLengthToObject {err.ToString()}");
                                return;
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
                                moves.AddRange(GetCells(pos, dir, length));
                            }
                            if ((lastCellOpt.IsSome() && lastCellOpt.Peel().color != cell.color)) {
                                (length, err) = GetLength(board, dir, lastCell, maxLength);
                                if (err != ControllerErrors.None) {
                                    Debug.LogError($"CantGetLength {err.ToString()}");
                                    return;
                                }
                                if (length != 0) {
                                    checkerInfo.isNeedAttack = true;
                                    isNeedAttack = true;
                                    moves.AddRange(GetCells(lastCell, dir, length));
                                }
                            }
                        }

                        checkerInfo.moves = moves;
                        checkerInfos.Add(pos, checkerInfo);
                    }
                }
            }

            DestroyHighlightCells(res.storageHighlightCells.transform);

            var selectedPos = ConvertToBoardPoint(hit.point);
            var checkerOpt = board[selectedPos.x, selectedPos.y];

            if (checkerOpt.IsSome() && checkerOpt.Peel().color == whoseMove) {
                action = Action.Select;
                selectedChecker = selectedPos;
            }
            var checker = checkerOpt.Peel();

            switch (action) {
                case Action.Select:
                    var currentInfo = checkerInfos[selectedPos];
                    if (isNeedAttack && !currentInfo.isNeedAttack) {
                        action = Action.None;
                        return;
                    }
                    HighlightCells(currentInfo.moves);
                    action = Action.Move;
                    break;
                case Action.Move:
                    currentInfo = checkerInfos[selectedChecker];
                    if (!IsPossibleMove(currentInfo.moves, selectedPos)) {
                        return;
                    }

                    var (res, moveCheckerErr) = MoveChecker(board, selectedChecker, selectedPos);
                    if (moveCheckerErr != ControllerErrors.None) {
                        Debug.LogError($"CantMoveChecker {moveCheckerErr.ToString()}");
                        return;
                    }
                    RelocateChecker(boardObj, selectedChecker, selectedPos, res.sentensed);
                    if (CheckPromotion(selectedPos, whoseMove, boardSize)) {
                        CheckerPromotion(selectedPos, whoseMove);
                    }
                    if (res.secondMove) {
                        selectedChecker = selectedPos;
                        action = Action.Move;
                        checkerInfos = null;
                        return;
                    }

                    IsGameOver(board, whoseMove);
                    whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                    var (positions, getAttackPosErr) = GetAttackPositions(board, whoseMove);
                    if (getAttackPosErr != ControllerErrors.None) {
                        Debug.LogError($"CantGetAttackPositions {getAttackPosErr.ToString()}");
                        return;
                    }

                    checkerInfos = null;
                    isNeedAttack = false;
                    action = Action.None;
                    break;
            }
        }

        public (bool, ControllerErrors) CheckNeedAttack(Option<Checker>[,] board, Vector2Int pos) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (false, ControllerErrors.BoardIsNull);
            }
            if (board[pos.x, pos.y].IsNone()) {
                return (false, ControllerErrors.None);
            }
            var checker = board[pos.x, pos.y].Peel();
            var maxLength = 1;
            if (checker.type == Type.King) {
                var max = Mathf.Max(board.GetLength(1), board.GetLength(0));
                maxLength = max;
            }
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    var dir = new Vector2Int(i, j);
                    if (i == 0 || j == 0) {
                        continue;
                    }
                    var (length, err) = GetLengthToObject(board, pos, dir, maxLength);
                    if (err != ControllerErrors.None) {
                        Debug.LogError($"CantGetLengthToObject {err.ToString()}");
                        return (false, ControllerErrors.CantGetLength);
                    }
                    var cell = pos + dir * length;
                    var lastCellOpt = board[cell.x, cell.y];
                    if (lastCellOpt.IsSome() && lastCellOpt.Peel().color != checker.color) {
                        cell = cell + dir;
                        var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));
                        if (IsOnBoard(boardSize, cell) && board[cell.x, cell.y].IsNone()) {
                            return (true, ControllerErrors.None);
                        }
                    }
                }
            }

            return (false, ControllerErrors.None);
        }

        public (int, ControllerErrors) GetLength(
            Option<Checker>[,] board,
            Vector2Int dir,
            Vector2Int pos,
            int maxLength
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (0, ControllerErrors.BoardIsNull);
            }
            var (length, err) = GetLengthToObject(board, pos, dir, maxLength);
            if (err != ControllerErrors.None) {
                Debug.LogError($"CantGetLengthToObject {err.ToString()}");
                return (0, ControllerErrors.CantGetLength);
            }
            if (length == 0) {
                return (length, ControllerErrors.None);
            }

            var lastCell = pos + dir * length;
            var checkerOpt = board[lastCell.x, lastCell.y];
            if (checkerOpt.IsSome()) {
                length--;
            }

            return (length, ControllerErrors.None);
        }

        public List<Vector2Int> GetCells(Vector2Int pos, Vector2Int dir, int length) {
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

        private (List<Vector2Int>, ControllerErrors) GetAttackPositions(
            Option<Checker>[,] board,
            Color color
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (null, ControllerErrors.BoardIsNull);
            }

            var positions = new List<Vector2Int>();
            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j++) {
                    if (board[i, j].IsSome() && board[i, j].Peel().color == color) {
                        var pos = new Vector2Int(i, j);
                        var (isNeedAttack, err) = CheckNeedAttack(board, pos);
                        if (err != ControllerErrors.None) {
                            Debug.LogError($"CantCheckNeedAttack {err.ToString()}");
                            return (null, ControllerErrors.CantCheckNeedAttack);
                        }

                        if (isNeedAttack) {
                            positions.Add(pos);
                        }
                    }
                }
            }

            return (positions, ControllerErrors.None);
        }

        private (MoveRes, ControllerErrors) MoveChecker(
            Option<Checker>[,] board,
            Vector2Int from,
            Vector2Int to
        ) {
            var moveRes = new MoveRes();
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (moveRes, ControllerErrors.BoardIsNull);
            }

            var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));

            var vecDif = to - from;
            var dir = new Vector2Int(vecDif.x/Mathf.Abs(vecDif.x), vecDif.y/Mathf.Abs(vecDif.y));
            board[to.x, to.y] = board[from.x, from.y];
            board[from.x, from.y] = Option<Checker>.None();
            for (int i = 1; i < Mathf.Abs(vecDif.x); i++) {
                var cell = from + dir * i;
                if (board[cell.x, cell.y].IsSome()) {
                    moveRes.sentensed = cell;
                    board[cell.x, cell.y] = Option<Checker>.None();

                    var (isNeedAttack, isNeedAttackErr) = CheckNeedAttack(board, to);
                    if (isNeedAttackErr != ControllerErrors.None) {
                        Debug.LogError($"CantCheckNeedAttack {isNeedAttackErr.ToString()}");
                        return(moveRes, ControllerErrors.CantCheckNeedAttack);
                    }

                    if (isNeedAttack) {
                        moveRes.secondMove = true;
                    }
                    break;
                }
            }

            return (moveRes, ControllerErrors.None);
        }

        public void GetPossibleMoves(
            Option<Checker>[,] board,
            Vector2Int pos,
            Vector2Int? badDir,
            List<Vector2Int> possibleMoves
        ) {
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i == 0 || j == 0) {
                        continue;
                    }
                    var dir = new Vector2Int(i, j);
                    if (badDir.HasValue && badDir == dir) {
                        continue;
                    }
                    var cell = pos + dir;
                    var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));
                    if (IsOnBoard(boardSize, cell) && board[cell.x, cell.y].IsSome() 
                    && board[cell.x, cell.y].Peel().color != board[pos.x, pos.y].Peel().color) {
                        cell = cell + dir;
                        if (IsOnBoard(boardSize, cell) && board[cell.x, cell.y].IsNone()) {
                            possibleMoves.Add(cell);
                            GetPossibleMoves(board, cell, dir * -1, possibleMoves);
                        }
                    }
                }
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
            if (sentensed.HasValue) {
                Destroy(boardObj[sentensed.Value.x, sentensed.Value.y]);
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