using System.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using option;
using movement;
using rules;

namespace checkers {
    public enum CheckersErrors {
        None,
        BoardIsNull,
        ListIsNull,
        CantGetLength
    }
    public static class Checkers {
        public static void FillingBoard(Option<Checker>[,] board) {
            board[0, 1] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[0, 3] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[0, 5] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[0, 7] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 0] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 2] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 4] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[1, 6] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 1] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 3] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 5] = Option<Checker>.Some(new Checker { color = rules.Color.Black });
            board[2, 7] = Option<Checker>.Some(new Checker { color = rules.Color.Black });

            board[5, 6] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[5, 4] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[5, 2] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[5, 0] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 7] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 5] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 3] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[6, 1] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 6] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 4] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 2] = Option<Checker>.Some(new Checker { color = rules.Color.White });
            board[7, 0] = Option<Checker>.Some(new Checker { color = rules.Color.White });
        }
    }
}
