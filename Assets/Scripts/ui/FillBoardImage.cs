using UnityEngine;
using controller;

namespace ui {
    public class FillBoardImage : MonoBehaviour {
        public void FillBoardPreview(SaveInfo save, UiResources res) {
            var imageBoardPrefab = res.boardImage10x10;
            if (save.boadSize == BoadSize.SmallBoard) {
                imageBoardPrefab = res.boardImage8x8;
            }

            var boardGrid = Instantiate(imageBoardPrefab, gameObject.transform);
            for (int i = 0; i < save.board.GetLength(1); i++) {
                for (int j = 0; j < save.board.GetLength(0); j++) {
                    if (save.board[i, j].IsNone()) {
                        Instantiate(res.emptyCell, boardGrid.transform);
                        continue;
                    }
                    var checker = save.board[i, j].Peel();
                    if (checker.color == ChColor.White) {
                        Instantiate(res.whiteCheckerImage, boardGrid.transform);
                    } else if (checker.color == ChColor.Black) {
                        Instantiate(res.blackCheckerImage, boardGrid.transform);
                    }
                }
            }
        }
    }
}
