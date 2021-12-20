using System.Collections.Generic;
using UnityEngine;
using option;
using System.Reflection;

namespace checkers {
    public struct Selected {
        public Vector2Int? selectedChecker;
        public Vector2Int? selectedPos;
    }


    public class TestApi : MonoBehaviour {
        private Selected sel;
        //private Dictionary<Vector2Int, List<MoveInfo>> moves;
        private Vector2Int? curPos;
        private List<Vector2Int> sentenced = new List<Vector2Int>();
        private Option<Checker>[,] board = new Option<Checker>[8,8];
        private ChColor whoseMove = ChColor.White;
        private ChKind kind = ChKind.Russian;

        private void Awake() {
            FillBoard(board);
            ShowBoard(board);
            Movement.GetMovesTree(board, new Vector2Int(5,3), kind);
        }

        // public void CheckInputPoint() {
        //     if (board == null) {
        //         Debug.LogError("BoardIsNull");
        //         return;
        //     }

        //     if (!curPos.HasValue) return;

        //     var chOpt = board[curPos.Value.x, curPos.Value.y];
        //     if (chOpt.IsNone()) {
        //         if (!sel.selectedChecker.HasValue) return;
        //         sel.selectedPos = curPos;
        //         if (moves.ContainsKey(sel.selectedChecker.Value)) {
        //             foreach(var move in moves[sel.selectedChecker.Value]) {
        //                 if (move.move.to == sel.selectedPos) {
        //                     var newMove = Move.Mk(sel.selectedChecker.Value, sel.selectedPos.Value);
        //                     Movement.Move(
        //                         board,
        //                         newMove,
        //                         sentenced
        //                     );

        //                     foreach (var sentencedPos in sentenced) {
        //                         board[sentencedPos.x, sentencedPos.y] = Option<Checker>.None();
        //                     }

        //                     ClearConsole();
        //                     ShowBoard(board);
        //                     sel.selectedChecker = null;
        //                     whoseMove = Movement.ChangeMove(whoseMove);
        //                     break;
        //                 }
        //             }
        //         }

        //         return;
        //     }

        //     var ch = chOpt.Peel();
        //     if (ch.color == whoseMove) {
        //         sel.selectedChecker = curPos;
        //         moves = Movement.GetCheckersMoves(board, whoseMove, kind);
        //         moves = Movement.GetAnalysedCheckerMoves(moves);
        //         ClearConsole();
        //         ShowBoard(board);
        //         Debug.Log("Selected " + curPos);
        //         return;
        //     } else {
        //         Debug.LogError("BadPos");
        //     }
        // }

        // public bool SetPos(string targetPos) {
        //     string firstHalfPos = "";
        //     string secondHalfPos = "";
        //     if (targetPos == null) {
        //         Debug.LogError("StringIsNull");
        //         return false;
        //     }

        //     bool fillSecondHalf = false;
        //     foreach (var pos in targetPos) {
        //         if (pos == ' ' || pos == ',' || pos == '/') {
        //             fillSecondHalf = true;
        //             continue;
        //         }

        //         if (fillSecondHalf) {
        //             secondHalfPos += pos;
        //         } else {
        //             firstHalfPos += pos;
        //         }
        //     }

        //     if (firstHalfPos == "" || secondHalfPos == "") {
        //         Debug.LogError("BadInput");
        //         return false;
        //     }

        //     if (!int.TryParse(firstHalfPos, out int resI)) {
        //         Debug.LogError("BadInput");
        //         return false;
        //     }

        //     if (!int.TryParse(secondHalfPos, out int resJ)) {
        //         Debug.LogError("BadInput");
        //         return false;
        //     }

        //     curPos = new Vector2Int(resI, resJ);
        //     return true;
        // }

        private void ClearConsole() {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }

        private void FillBoard(Option<Checker>[,] board) {
            if (board == null) {
                Debug.LogError($"BoardIsNull");
                return;
            }
            // var color = ChColor.Black;
            // for (int i = 0; i < board.GetLength(1); i++) {
            //     for (int j = 0; j < board.GetLength(0); j = j + 2) {
            //         if (i == 3 || i == 4) {
            //             color = ChColor.White;
            //             break;
            //         }

            //         if (i % 2 == 0) {
            //             board[i, j + 1] = Option<Checker>.Some(new Checker { color = color});
            //         } else {
            //             board[i, j] = Option<Checker>.Some(new Checker { color = color });
            //         }
            //     }
            // }
            board[5, 3] = Option<Checker>.Some(new Checker { color = ChColor.White});
            board[4, 4] = Option<Checker>.Some(new Checker { color = ChColor.Black});
            board[4, 2] = Option<Checker>.Some(new Checker { color = ChColor.Black});
            //board[2, 2] = Option<Checker>.Some(new Checker { color = ChColor.Black});
            board[2, 4] = Option<Checker>.Some(new Checker { color = ChColor.Black});
            board[2, 6] = Option<Checker>.Some(new Checker { color = ChColor.Black});
        }

        public void ShowBoard(Option<Checker>[,] board) {
            var output = "                                  0";
            for (int i = 0; i < board.GetLength(0); i++) {
                output += $"         {i}";
            }
            Debug.Log(output);
            output = "                                  0|";

            for (int i = 0; i < board.GetLength(1); i++) {
                for (int j = 0; j < board.GetLength(0); j++) {
                    char cellInf = '*';
                    if (board[i, j].IsSome()) {
                        var checker = board[i, j].Peel();
                        int typeNum = (int)checker.color + (int)checker.type * 2;
                        cellInf = (char)(48 + typeNum);
                    }
                    output +=  $"         { cellInf }";
                }
                Debug.Log(output);
                output = "                                  " + (i + 1).ToString() + "|";
            }
        }
    }
}
