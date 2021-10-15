using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;
namespace controller {
    public struct Checker {
        public Color color;
    }

    public enum Color {
        White,
        Black
    }

    public class Controller : MonoBehaviour {
        public Transform cellSize;
        public GameObject whiteChecker;
        public GameObject blackChecker;
        Option<Checker>[,] board = new Option<Checker>[8, 8];
        GameObject[,] boardObj = new GameObject[8, 8];
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
