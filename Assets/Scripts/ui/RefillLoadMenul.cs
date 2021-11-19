using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using controller;

namespace ui {
    public class RefillLoadMenul : MonoBehaviour {
        public void FillLoadMenu() {
            // foreach (Transform child in saveTemplatesStorage.transform) {
            //     Destroy(child.gameObject);
            // }
            // var saves = gameController.GetSavesInfo();
            // saveTemplatesStorage.sizeDelta = new Vector2(0, 130 * saves.Count);

            // foreach (var save in saves) {
            //     var curObj = Instantiate(
            //         loadTemplate,
            //         Vector3.zero,
            //         Quaternion.identity,
            //         saveTemplatesStorage.transform
            //     );
            //     var imageBoardPrefab = boardImage10x10;
            //     if (save.boadSize == BoadSize.SmallBoard) {
            //         imageBoardPrefab = boardImage8x8;
            //     }

            //     var parent = curObj.transform.GetChild(5).transform;
            //     var boardGrid = Instantiate(imageBoardPrefab, parent).transform.GetChild(0);
            //     for (int i = 0; i < save.board.GetLength(1); i++) {
            //         for (int j = 0; j < save.board.GetLength(0); j++) {
            //             if (save.board[i, j].IsNone()) {
            //                 Instantiate(emptyCell, boardGrid);
            //                 continue;
            //             }
            //             var checker = save.board[i, j].Peel();
            //             if (checker.color == ChColor.White) {
            //                 Instantiate(whiteCheckerImage, boardGrid);
            //             } else if (checker.color == ChColor.Black) {
            //                 Instantiate(blackCheckerImage, boardGrid);
            //             }
            //         }
            //     }

            //     foreach (Transform child in curObj.transform) {
            //         if (child.gameObject.TryGetComponent(out Button but)) {
            //             if (but.name == "Load") {
            //                 but.onClick.AddListener(() => gameController.Load(save.fileName));
            //             } else {
            //                 but.onClick.AddListener(() => {
            //                         File.Delete(save.fileName);
            //                         Destroy(curObj);
            //                     }
            //                 );
            //             }
            //         } else if (child.gameObject.TryGetComponent(out Text text)) {
            //             if (text.name == "Date") {
            //                 var parseDate = save.saveDate.ToString("dd.MM.yyyy HH:mm:ss");
            //                 text.text =  "Date: " + parseDate;
            //             } else if (text.name == "Kind") {
            //                 text.text = "Checker Kind: " + save.checkerKind.ToString();
            //             }
            //         } else if (child.gameObject.TryGetComponent(out RawImage image)) {
            //             if (save.whoseMove == controller.ChColor.White) {
            //                 image.texture = whiteCheckerImage.texture;
            //                 image.color = whiteCheckerImage.color;
            //             } else if (save.whoseMove == controller.ChColor.Black) {
            //                 image.texture = blackCheckerImage.texture;
            //                 image.color = blackCheckerImage.color;
            //             }
            //         }
            //     }
            // }
        }
    }
}
