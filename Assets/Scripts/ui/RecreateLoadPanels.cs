using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using controller;
using System;
using UnityEngine.UI;
using System.IO;

namespace ui {
    public class RecreateLoadPanels : MonoBehaviour {
        public Controller gameController;
        public ChangeActive changeActiveLoad;
        public RectTransform savePanelsStorage;
        public controller.Resources res;

        private void Awake() {
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }
            if (res.boardImage10x10 == null) {
                Debug.LogError("NoCheckers");
                this.enabled = false;
                return;
            }
            if (res.boardImage8x8 == null) {
                Debug.LogError("NoHighlightCells");
                this.enabled = false;
                return;
            }
            if (res.blackCheckerImage == null) {
                Debug.LogError("NoHighlightCells");
                this.enabled = false;
                return;
            }
            if (res.whiteCheckerImage == null) {
                Debug.LogError("NoHighlightCells");
                this.enabled = false;
                return;
            }
        }
        public void DestroyOldPanels() {
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }
        }

        public void InstantiateLoadPanels() {
            var saves = gameController.GetSavesInfo();
            savePanelsStorage.sizeDelta = new Vector2(0, res.loadPanel.sizeDelta.y * saves.Count);
            saves.Sort(new Comparison<SaveInfo>((f1, f2) => f2.saveDate.CompareTo(f1.saveDate)));
            foreach (var save in saves) {
                var curPanel = Instantiate(
                    res.loadPanel.transform,
                    Vector3.zero,
                    Quaternion.identity,
                    savePanelsStorage.transform
                );

                FillLoadPanel(curPanel.gameObject, save);
            }
        }

        public void FillLoadPanel(GameObject panel, SaveInfo save) {
            var imageBoardPrefab = res.boardImage10x10;
            if (save.boadSize == BoadSize.SmallBoard) {
                imageBoardPrefab = res.boardImage8x8;
            }

            var parent = panel.transform.GetChild(5).transform;
            var boardGrid = Instantiate(imageBoardPrefab, parent).transform.GetChild(0);
            for (int i = 0; i < save.board.GetLength(1); i++) {
                for (int j = 0; j < save.board.GetLength(0); j++) {
                    if (save.board[i, j].IsNone()) {
                        Instantiate(res.emptyCell, boardGrid);
                        continue;
                    }
                    var checker = save.board[i, j].Peel();
                    if (checker.color == ChColor.White) {
                        Instantiate(res.whiteCheckerImage, boardGrid);
                    } else if (checker.color == ChColor.Black) {
                        Instantiate(res.blackCheckerImage, boardGrid);
                    }
                }
            }

            foreach (Transform child in panel.transform) {
                if (child.gameObject.TryGetComponent(out Button but)) {
                    if (but.name == "Load") {
                        but.onClick.AddListener(() => gameController.Load(save.fileName));
                        but.onClick.AddListener(() => changeActiveLoad.ChangeActiveObject());
                    } else {
                        but.onClick.AddListener(() => {
                                File.Delete(save.fileName);
                                Destroy(panel.gameObject);
                            }
                        );
                    }
                } else if (child.gameObject.TryGetComponent(out Text text)) {
                    if (text.name == "Date") {
                        var parseDate = save.saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                        text.text =  "Date: " + parseDate;
                    } else if (text.name == "Kind") {
                        text.text = "Checker Kind: " + save.checkerKind.ToString();
                    }
                } else if (child.gameObject.TryGetComponent(out RawImage image)) {
                    if (save.whoseMove == controller.ChColor.White) {
                        image.texture = res.whiteCheckerImage.texture;
                        image.color = res.whiteCheckerImage.color;
                    } else if (save.whoseMove == controller.ChColor.Black) {
                        image.texture = res.blackCheckerImage.texture;
                        image.color = res.blackCheckerImage.color;
                    }
                }
            }
        }
    }
}
