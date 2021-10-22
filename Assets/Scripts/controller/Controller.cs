using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;

namespace controller {
    enum ControllerErrors {
        None,
        BoardIsNull,
        GameObjectIsNull,
        ListIsNull,
        CantRelocateChecker,
        CantGetCheckerMovements,
        CantGetLength,
        CantGetFixMovement,
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

    enum Action {
        None,
        Select,
        Move
    }

    public struct Move {
        public Vector2Int from;
        public Vector2Int to;

        public static Move Mk(Vector2Int from, Vector2Int to) {
            return new Move{ from = from, to = to };
        }
    }


    public struct Checker {
        public Type type;
        public Color color;
    }

    public struct Linear {
        public Vector2Int dir;
        public Vector2Int start;
        public MovemenType type;
        public int length;

        public static Linear Mk(Vector2Int dir, Vector2Int start, int length, MovemenType type) {
            return new Linear { dir = dir, start = start, length = length, type = type };
        }
    }

    public enum MovemenType {
        Move,
        Attack
    }

    public class Controller : MonoBehaviour {
        private Resources res;
        public Transform cellPos;
        private Option<Checker>[,] board = new Option<Checker>[8, 8];
        private GameObject[,] boardObj = new GameObject[8, 8];
        private List<Move> moves = new List<Move>();
        private Color whoseMove;
        private Action action;

        private void Awake() {
            res = gameObject.GetComponentInParent<Resources>();
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }
        }

        private void Start() {
            FillBoard(board);
            SpawnCheckers(board);
        }

        private void Update() {
            if (!Input.GetMouseButtonDown(0)) {
                return;
            }
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out hit, 100f)) {
                return;
            }

            var selectedPos = ConvertToPointBoard(hit.point);
            var pieceOpt = board[selectedPos.x, selectedPos.y];
            if (pieceOpt.IsSome() && pieceOpt.Peel().color == whoseMove) {
                action = Action.Select;
            }

            switch (action) {
                case Action.Select:
                    DestroyChildren(res.storageHighlightCells.transform);
                    moves.Clear();
                    var (newMoves, getMovesErr) = GetMoves(board, selectedPos);
                    if (getMovesErr !=  ControllerErrors.None) {
                        Debug.LogError($"CantGetMoves {getMovesErr.ToString()}");
                        return;
                    }
                    moves = newMoves;
                    action = Action.Move;
                    var highlightErr = HighlightCells(moves);
                    if (highlightErr != ControllerErrors.None) {
                        Debug.LogError($"CantHighlightCells {highlightErr.ToString()}");
                    }
                    break;
                case Action.Move:
                    var move = GetPossibleMove(moves, selectedPos);
                    if (!move.HasValue) {
                        action = Action.None;
                        moves.Clear();
                        break;
                    }

                    var (moveRes, moveErr) = MoveChecker(board, move.Value);
                    if (moveErr != ControllerErrors.None) {
                        Debug.LogError($"CantMove {moveErr.ToString()}");
                        return;
                    }

                    attackPositions.Clear();
                    if (moveRes.attackAgain == true) {
                        attackPositions.Add(move.Value.to);
                        action = Action.None;
                        return;
                    }

                    moves.Clear();
                    whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                    var (newAttackPositions, attackErr) = GetAttackPositions(board, whoseMove);
                    if (attackErr != ControllerErrors.None) {
                        Debug.LogError($"CantGetAttackPositions {attackErr.ToString()}");
                    }

                    attackPositions = newAttackPositions;
                    action = Action.None;
                    break;
            }
        }

        private (MoveResult, ControllerErrors) MoveChecker(Option<Checker>[,] board, Move move) {
            var moveRes = new MoveResult();
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (moveRes, ControllerErrors.BoardIsNull);
            }

            Vector2Int? sentenced = null;
            var pieceOpt = board[move.from.x, move.from.y];
            if (pieceOpt.IsNone()) {
                return (moveRes, ControllerErrors.None);
            }
            var color = pieceOpt.Peel().color;

            var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));

            var vecDif = move.to - move.from;
            var dir = new Vector2Int(vecDif.x/Mathf.Abs(vecDif.x), vecDif.y/Mathf.Abs(vecDif.y));
            for (int i = 1; i <= Mathf.Abs(vecDif.x); i++) {
                var cell = move.from + dir * i;
                if (board[cell.x, cell.y].IsSome()) {
                    sentenced = cell;
                    break;
                }
            }
            board[move.to.x, move.to.y] = board[move.from.x, move.from.y];
            board[move.from.x, move.from.y] = Option<Checker>.None();

            var relocateCheckerErr = RelocateChecker(move, boardObj, sentenced);
            if (relocateCheckerErr != ControllerErrors.None) {
                Debug.LogError($"CantRelocateChecker {relocateCheckerErr.ToString()}");
                return (moveRes, ControllerErrors.CantRelocateChecker);
            }

            if (CheckPromotion(move, color, boardSize)) {
                CheckerPromotion(move.to, color);
            }

            if (sentenced.HasValue) {
                board[sentenced.Value.x, sentenced.Value.y] = Option<Checker>.None();
                var (isNeedAttack, needAttackErr) = CheckNeedAttack(board, move.to, color);
                if (needAttackErr != ControllerErrors.None) {
                    Debug.LogError($"CantCheckNeedAttack {needAttackErr.ToString()}");
                    return (moveRes, ControllerErrors.CantCheckNeedAttack);
                }

                if (isNeedAttack) {
                    moveRes.attackAgain = true;
                }
            }

            return (moveRes, ControllerErrors.None);
        }

        private (List<Vector2Int>, ControllerErrors) GetAttackPositions(
            Option<Checker>[,] board,
            Color color
        ) {
            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (null, ControllerErrors.BoardIsNull);
            }

            var checkerList = new List<Vector2Int>();
            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j++) {
                    if (board[i, j].IsSome() && board[i, j].Peel().color == color) {
                        var pos = new Vector2Int(i, j);
                        var (isNeedAttack, err) = CheckNeedAttack(board, pos, color);
                        if (err != ControllerErrors.None) {
                            return (null, ControllerErrors.CantCheckNeedAttack);
                        }

                        if (isNeedAttack) {
                            checkerList.Add(pos);
                        }
                    }
                }
            }

            return (checkerList, ControllerErrors.None);
        }

        private ControllerErrors RelocateChecker(
            Move move,
            GameObject[,] boardObj,
            Vector2Int? sentenced
        ) {
            if (boardObj == null) {
                Debug.LogError("BoardIsNull");
                return ControllerErrors.BoardIsNull;
            }
            if (sentenced.HasValue) {
                Destroy(boardObj[sentenced.Value.x, sentenced.Value.y]);
            }
            var pos = ConvertToPointWorld(move.to);
            boardObj[move.from.x, move.from.y].transform.position = pos;
            boardObj[move.to.x, move.to.y] = boardObj[move.from.x, move.from.y];

            return ControllerErrors.None;
        }

        private (List<Linear>, ControllerErrors) GetCheckerMovements(
            Option<Checker>[,] board,
            Vector2Int pos
        ) {
            if (board == null) {
                Debug.LogError("NoSuchColor");
                return (null, ControllerErrors.NoSuchColor);
            }
            var checkerOpt = board[pos.x, pos.y];

            var movements = new List<Linear>();
            if (checkerOpt.IsNone()) {
                return (movements, ControllerErrors.None);
            }
            var checker = checkerOpt.Peel();
            Func<int, int, bool> comporator = (i,j) => true;
            if (checker.type == Type.King) {
                movements.AddRange(GetMovements(pos, 8, MovemenType.Move, comporator));
                movements.AddRange(GetMovements(pos, 8, MovemenType.Attack, comporator));
            } else if (checker.type == Type.Checker) {
                movements.AddRange(GetMovements(pos, 1, MovemenType.Attack, comporator));
                comporator = (i, j) => i < 0;
                if (checker.color == Color.Black) {
                    comporator = (i, j) => i > 0;
                }
                movements.AddRange(GetMovements(pos, 1, MovemenType.Move, comporator));
            }

            return (movements, ControllerErrors.None);
        }

        private List<Linear> GetMovements(
            Vector2Int start,
            int length,
            MovemenType type,
            Func<int, int, bool> comparator
        ) {
            List<Linear> movements = new List<Linear>();
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (i != 0 && j != 0 && comparator(i, j)) {
                        var dir = new Vector2Int(i, j);
                        movements.Add(Linear.Mk(dir, start, length, type));
                    }
                }
            }

            return movements;
        }

        private (List<Move>, ControllerErrors) GetMoves(Option<Checker>[,] board, Vector2Int pos) {
            var (checkerMovements, movementsErr) = GetCheckerMovements(board, pos);
            if (movementsErr != ControllerErrors.None) {
                return (null, ControllerErrors.CantGetCheckerMovements);
            }

            if (board == null) {
                return(null, ControllerErrors.BoardIsNull);
            }

            var checkerOpt = board[pos.x, pos.y];
            List<Move> moves = new List<Move>();
            if (checkerOpt.IsNone()) {
                return (moves, ControllerErrors.None);
            }
            var checker = checkerOpt.Peel();

            var fixCheckerMovements = new List<Linear>();
            foreach (var movement in checkerMovements) {
                var fixMovement = movement;
                var (length, getLengthErr) = GetLength(board, movement);
                if (getLengthErr != ControllerErrors.None) {
                    return (moves, ControllerErrors.CantGetLength);
                }

                fixMovement.length = length;
                var err = ControllerErrors.None;
                var maxLength = movement.length;
                (fixMovement, err) = GetFixMovement(board, fixMovement, checker.color, maxLength);
                if (err != ControllerErrors.None) {
                    return (moves, ControllerErrors.CantGetFixMovement);
                }
                fixCheckerMovements.Add(fixMovement);
            }

            foreach (var fixMovement in fixCheckerMovements) {
                foreach (var cell in GetCellsFromLinear(fixMovement)) {
                    moves.Add(controller.Move.Mk(pos, cell));
                }
            }

            return (moves, ControllerErrors.None);
        }

        public static List<Vector2Int> GetCellsFromLinear(Linear linear) {
            var moveCells = new List<Vector2Int>();
            for (int i = 1; i <= linear.length; i++) {
                var cell = linear.start + linear.dir * i;
                moveCells.Add(cell);
            }

            return moveCells;
        }

        private (Linear, ControllerErrors) GetFixMovement(
            Option<Checker>[,] board,
            Linear movement,
            Color color,
            int maxLength
        ) {
            var lastCell = movement.start + movement.dir * movement.length;
            var (isNeedAttack, err) = CheckNeedAttack(board, movement.start, color);
            if (err != ControllerErrors.None) {
                return (movement, ControllerErrors.CantCheckNeedAttack);
            }
            if (movement.type == MovemenType.Move) {
                if (isNeedAttack) {
                    movement.length = 0;
                    return (movement, ControllerErrors.None);
                }
                if (board[lastCell.x, lastCell.y].IsSome()) {
                    movement.length--;
                }
            }
            if (movement.type == MovemenType.Attack) {
                if (board[lastCell.x, lastCell.y].IsSome()) {
                    movement.length = 0;
                    if (board[lastCell.x, lastCell.y].Peel().color != color) {
                        movement.start = lastCell;
                        var newLinear = movement;
                        newLinear.length = maxLength;
                        var (length, getLengthErr) = GetLength(board, newLinear);
                        lastCell = lastCell + movement.dir * length;
                        if (board[lastCell.x, lastCell.y].IsSome()) {
                            length--;
                        }
                        movement.length = length;
                    }
                } else {
                    movement.length = 0;
                }
            }

            return (movement, ControllerErrors.None);
        }

        private bool CheckPromotion(Move move, Color color, Vector2Int boardSize) {
            if (move.to.x == 0 && color == Color.White) {
                return true;
            }
            if (move.to.x == boardSize.x - 1 && color == Color.Black) {
                return true;
            }

            return false;
        }

        private ControllerErrors CheckerPromotion(Vector2Int pos, Color color) {
            board[pos.x, pos.y] = Option<Checker>.None();
            var king = new Checker {type = Type.King, color = color };
            board[pos.x, pos.y] = Option<Checker>.Some(king);
            Destroy(boardObj[pos.x, pos.y]);

            GameObject prefab;
            if (color == Color.White) {
                prefab = res.whiteKing;
            } else if (color == Color.Black) {
                prefab = res.blackKing;
            } else {
                Debug.LogError("NoSuchColor");
                return ControllerErrors.NoSuchColor;
            }
            boardObj[pos.x, pos.y] = SpawnObject(prefab, pos, res.boardPos.transform);

            return ControllerErrors.None;
        }

        private (int, ControllerErrors) GetLength(Option<Checker>[,] board, Linear linear) {
            int length = 0;
            var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));

            if (board == null) {
                Debug.LogError("BoardIsNull");
                return (0, ControllerErrors.BoardIsNull);
            }
            for (int i = 1; i <= linear.length; i++) {
                var cell = linear.start + linear.dir * i;

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

        private (bool, ControllerErrors) CheckNeedAttack(
            Option<Checker>[,] board,
            Vector2Int pos,
            Color color
        ) {
            var (checkerMovements, movementsErr) = GetCheckerMovements(board, pos);
            var boardSize = new Vector2Int(board.GetLength(1), board.GetLength(0));
            if (board == null) {
                return(false, ControllerErrors.BoardIsNull);
            }

            foreach (var movement in checkerMovements) {
                var (length, getLengthErr) = GetLength(board, movement);
                if (getLengthErr != ControllerErrors.None) {
                    return (false, ControllerErrors.CantGetLength);
                }

                var cell = movement.start + movement.dir * length;
                if (movement.type == MovemenType.Attack) {
                    if(!board[cell.x, cell.y].IsSome()) {
                        continue;
                    }

                    if (board[cell.x, cell.y].Peel().color != color) {
                        cell = cell + movement.dir;
                        if (IsOnBoard(boardSize, cell) && board[cell.x, cell.y].IsNone()) {
                            return (true, ControllerErrors.None);
                        }
                    }
                }
            }

            return (false, ControllerErrors.None);
        }

        public static bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x >= boardSize.x || pos.y < 0 || pos.y >= boardSize.y) {
                return false;
            }

            return true;
        }

        private void DestroyChildren(Transform parent) {
            foreach (Transform child in parent) {
                Destroy(child.gameObject);
            }
        }

        private ControllerErrors HighlightCells(List<Move> possibleMoves) {
            if (possibleMoves == null) {
                return ControllerErrors.ListIsNull;
            }
            var boardPos = res.boardPos.transform.position;
            foreach (var pos in possibleMoves) {
                SpawnObject(res.highlightCell, pos.to, res.storageHighlightCells.transform);
            }

            return ControllerErrors.None;
        }

        private Move? СompareMoveInfo(List<Move> moveInfos, Vector2Int selectPos) {
            foreach (var move in moves) {
                if (move.to == selectPos) {
                    return move;
                }
            }

            return null;
        }

        private Vector2Int ConvertToPointBoard(Vector3 selectedPoint) {
            var inversePoint = res.boardPos.InverseTransformPoint(selectedPoint);
            var cellLoc = cellPos.localPosition;
            var cellSize = res.cellSize.localScale;
            var floatVec = (inversePoint + new Vector3(-cellLoc.x, 0, cellLoc.z)) / cellSize.x;
            var point = new Vector2Int(Mathf.Abs((int)(floatVec.z)), Mathf.Abs((int)floatVec.x));

            return point;
        }

        private Vector3 ConvertToPointWorld(Vector2Int boardPoint) {
            var offset = res.cellSize.localScale / 2.00f;
            var floatVec = new Vector3(boardPoint.x, 0f, boardPoint.y);
            var cellLoc = this.cellPos.localPosition;
            var cellSize = res.cellSize.localScale;
            var point = cellSize.x * floatVec - new Vector3(cellLoc.x, 0, cellLoc.z) + offset;

            return point;
        }

        private GameObject SpawnObject(
            GameObject prefab,
            Vector2Int spawnPos,
            Transform parent
        ) {
            var spawnWorldPos = ConvertToPointWorld(spawnPos);
            return Instantiate(prefab, spawnWorldPos, Quaternion.identity, parent);
        }

        public void FillBoard(Option<Checker>[,] board) {
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
        }
    }
}