using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;
namespace controller {
    public struct Checker {
        public Color color;
        public Type type;
    }

    public enum MovementType {
        Move,
        Attack
    }

    public struct Linear {
        public Vector2Int dir;
        public int length;

        public static Linear Mk(Vector2Int dir, int length) {
            return new Linear { dir = dir, length = length };
        }
    }

    public struct FixedMovement {
        public Linear linear;
        public Vector2Int pos;

        public static FixedMovement Mk(Linear linear, Vector2Int pos) {
            return new FixedMovement { linear = linear, pos = pos };
        }
    }

    public struct CheckerMovement {
        public FixedMovement movement;
        public MovementType type;

        public static CheckerMovement Mk(FixedMovement movement, MovementType type) {
            return new CheckerMovement { movement = movement, type = type };
        }
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

    public class Controller : MonoBehaviour {
        public Transform cellSize;
        public GameObject whiteChecker;
        public GameObject blackChecker;
        private Option<Checker>[,] board = new Option<Checker>[8, 8];
        private GameObject[,] boardObj = new GameObject[8, 8];
        private List<Vector2Int> possibleMoves = new List<Vector2Int>();
        private Vector2Int lastSelectedPos;
        private Color whoseMove;
        private Action action;
        public GameObject storageHighlightCells;
        public GameObject highlightCell;
        private void Start() {
            FillingBoard();
            CheckerSpawner(board);
        }

        private void Update() {
            if (!Input.GetMouseButtonDown(0)) {
                return;
            }
            Ray ray;
            RaycastHit hit;
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out hit, 100f)) {
                return;
            }
            var selectedPosFloat = hit.point - (transform.position - cellSize.lossyScale * 4);
            var selectedPos = new Vector2Int((int)selectedPosFloat.x, (int)selectedPosFloat.z);
            var pieceOpt = board[selectedPos.x, selectedPos.y];
            if (action == Action.None && pieceOpt.IsSome() && pieceOpt.Peel().color == whoseMove) {
                action = Action.Select;
            }

            switch (action) {
                case Action.Select:
                    DestroyChildrens(storageHighlightCells.transform);
                    possibleMoves.Clear();
                    var checkerMovement = GetCheckersMovement(board, selectedPos);
                    possibleMoves = GetMoves(board, checkerMovement);
                    lastSelectedPos = selectedPos;
                    HighlightCells(possibleMoves);
                    action = Action.Move;
                    break;
                case Action.Move:
                    Move(possibleMoves, selectedPos, lastSelectedPos);
                    DestroyChildrens(storageHighlightCells.transform);
                    possibleMoves.Clear();
                    action = Action.None;
                    break;
            }
        }

        private void Move(List<Vector2Int> possibleMoves, Vector2Int to, Vector2Int from) {
            var boardPos = gameObject.transform.position;
            foreach (var move in possibleMoves) {
                if (move == to) {
                    board[to.x, to.y] = Option<Checker>.Some(board[from.x, from.y].Peel());
                    board[from.x, from.y] = Option<Checker>.None();
                    boardObj[from.x, from.y].transform.position = new Vector3(
                    to.x + boardPos.x - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2,
                    boardPos.y + cellSize.lossyScale.x / 2,
                    to.y + boardPos.z - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2
                    );
                    whoseMove = (Color)((int)(whoseMove + 1) % (int)Color.Count);
                    break;
                }
            }
        }

        private void CheckerSpawner(Option<Checker>[,] board) {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    if (board[i, j].IsSome()) {
                        var checker = board[i, j].Peel();
                        GameObject obj;
                        if (checker.color == Color.White) {
                            obj = whiteChecker;
                        } else {
                            obj = blackChecker;
                        }
                        var pos = new Vector2Int(i, j);
                        boardObj[i, j] = ObjectSpawner(obj, pos, gameObject.transform);
                    }
                }
            }
        }

        public List<Vector2Int> GetMoves(
            Option<Checker>[,] board,
            List<CheckerMovement> checkerMovements
        ) {
            var moves = new List<Vector2Int>();
            foreach (var checkerMovement in checkerMovements) {
                var movement = checkerMovement.movement;
                var length = GetLength(board, movement.linear, movement.pos);
                moves.AddRange(GetMoveCells(movement.linear.dir, movement.pos, length));
            }
            return moves;
        }

        public int GetLength(Option<Checker>[,] board, Linear linear, Vector2Int pos) {
            int length = 0;
            var boardSize = new Vector2Int (board.GetLength(1), board.GetLength(0));
            for (int i = 1; i <= linear.length; i++) {
                var cell = pos + linear.dir * i;
                if (!IsOnBoard(boardSize, pos)) {
                    break;
                }
                length ++;
                if (board[cell.x, cell.y].IsSome()) {
                    break;
                }
            }
            return length;
        }

        public List<Vector2Int> GetMoveCells(Vector2Int dir, Vector2Int pos, int length) {
            var moveCells = new List<Vector2Int>();
            for (int i = 1; i <= length; i++) {
                var cell = pos + dir * i;
                moveCells.Add(cell);
            }
            return moveCells;
        }

        public bool IsOnBoard(Vector2Int boardSize, Vector2Int pos) {
            if (pos.x < 0 || pos.x > boardSize.x || pos.y < 0 || pos.y > boardSize.y) {
                return false;
            }
            return true;
        }

        public List<CheckerMovement> GetCheckersMovement(Option<Checker>[,] board, Vector2Int pos) {
            if (board[pos.x, pos.y].IsNone()) {
                return null;
            }
            var checker = board[pos.x, pos.y].Peel();
            List<CheckerMovement> checkerMovements = new List<CheckerMovement>();
            switch (checker.type) {
                case Type.Checker:
                    int dir = 1;
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(1, 1), 1), pos), MovementType.Attack));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(1, -1), 1), pos), MovementType.Attack));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, 1), 1), pos), MovementType.Attack));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, -1), 1), pos), MovementType.Attack));
                    if (checker.color == Color.Black) {
                        dir = -1;
                    }
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, 1) * dir, 1), pos), MovementType.Move));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, -1) * dir, 1), pos), MovementType.Move));
                    break;
                case Type.King:
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(1, 1), 8), pos), MovementType.Attack));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(1, -1), 8), pos), MovementType.Attack));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, 1), 8), pos), MovementType.Attack));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, -1), 8), pos), MovementType.Attack));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(1, 1), 8), pos), MovementType.Move));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(1, -1), 8), pos), MovementType.Move));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, 1), 8), pos), MovementType.Move));
                    checkerMovements.Add(CheckerMovement.Mk(FixedMovement.Mk(Linear.Mk(new Vector2Int(-1, -1), 8), pos), MovementType.Move));
                    break;
            }
            return checkerMovements;
        }

        private void DestroyChildrens(Transform parent) {
            foreach (Transform child in parent) {
                Destroy(child.gameObject);
            }
        }

        private GameObject ObjectSpawner(
            GameObject gameObject,
            Vector2Int spawnPos,
            Transform parentTransform
        ) {
            var boardPos = transform.position;

            var spawnWorldPos = new Vector3(
                spawnPos.x + boardPos.x - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2,
                boardPos.y + cellSize.lossyScale.x / 2,
                spawnPos.y + boardPos.z - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2
            );
            return Instantiate(gameObject, spawnWorldPos, Quaternion.identity, parentTransform);
        }

        private void HighlightCells(List<Vector2Int> possibleMoves) {
            var parentTransform = storageHighlightCells.transform;
            var boardPos = gameObject.transform.position;
            foreach (var pos in possibleMoves) {
                var spawnWorldPos = new Vector3(
                    pos.x + boardPos.x - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2,
                    boardPos.y + cellSize.lossyScale.x / 2,
                    pos.y + boardPos.z - cellSize.lossyScale.x * 4 + cellSize.lossyScale.x / 2
                );

                Instantiate(highlightCell, spawnWorldPos, Quaternion.identity, parentTransform);
            }
        }

        private void FillingBoard() {
            board[0, 0] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[0, 2] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[0, 4] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[0, 6] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[1, 1] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[1, 3] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[1, 5] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[1, 7] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[2, 0] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[2, 2] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[2, 4] = Option<Checker>.Some(new Checker { color = Color.Black });
            board[2, 6] = Option<Checker>.Some(new Checker { color = Color.Black });

            board[5, 7] = Option<Checker>.Some(new Checker { color = Color.White });
            board[5, 5] = Option<Checker>.Some(new Checker { color = Color.White });
            board[5, 3] = Option<Checker>.Some(new Checker { color = Color.White });
            board[5, 1] = Option<Checker>.Some(new Checker { color = Color.White });
            board[6, 6] = Option<Checker>.Some(new Checker { color = Color.White });
            board[6, 4] = Option<Checker>.Some(new Checker { color = Color.White });
            board[6, 2] = Option<Checker>.Some(new Checker { color = Color.White });
            board[6, 0] = Option<Checker>.Some(new Checker { color = Color.White });
            board[7, 7] = Option<Checker>.Some(new Checker { color = Color.White });
            board[7, 5] = Option<Checker>.Some(new Checker { color = Color.White });
            board[7, 3] = Option<Checker>.Some(new Checker { color = Color.White });
            board[7, 1] = Option<Checker>.Some(new Checker { color = Color.White });
        }
    }
}
