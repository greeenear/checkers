using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace ui {
    public class RefreshLoadPanels : MonoBehaviour {
        public Controller gmController;
        public Button openMenu;
        public List<LoadPanelRes> loadPanels;
        public RectTransform loadPanelsStorage;
        public RectTransform loadPanel;
        public GameObject pageList;
        public UiResources res;

        private void Awake() {
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }

            if (res.boardImages.boardImage10x10 == null) {
                Debug.LogError("NoBoardImage10x10");
                this.enabled = false;
                return;
            }

            if (res.boardImages.boardImage8x8 == null) {
                Debug.LogError("NoBoardImage8x8");
                this.enabled = false;
                return;
            }

            if (res.checkerImages.blackChecker == null) {
                Debug.LogError("NoBlackCheckerImage");
                this.enabled = false;
                return;
            }

            if (res.checkerImages.whiteChecker == null) {
                Debug.LogError("NoWhiteCheckerImage");
                this.enabled = false;
                return;
            }
        }

        public void Refresh() {
            var countSavesInPanel = (int)(loadPanelsStorage.sizeDelta.y / loadPanel.sizeDelta.y);
            var sentencedHighlight = new List<Transform>();
            foreach (Transform child in pageList.transform) {
                sentencedHighlight.Add(child);
            }
            foreach (Transform child in sentencedHighlight) {
                Destroy(child.gameObject);
                child.SetParent(null);
            }

            var saves = gmController.GetSavesInfo();
            if (saves == null) {
                Debug.LogError("SaveListIsNull");
                return;
            }
            var numberOfPage = saves.Count / countSavesInPanel;
            if (saves.Count % countSavesInPanel != 0) numberOfPage++;

            for (int i = 0; i < numberOfPage; i++) {
                var curBut = Instantiate(res.pageBut, pageList.transform);
                int loadNum = i;
                curBut.onClick.AddListener(() => FillPage(loadNum));
            }
        }

        public void FillPage(int pageNumber) {
            var saves = gmController.GetSavesInfo();
            var countSavesInPanel = (int)(loadPanelsStorage.sizeDelta.y / loadPanel.sizeDelta.y);

            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));
            if (res.loadPanel.boardImage == null) {
                Debug.LogError("NoBoardImage");
                return;
            }

            if (res.loadPanel.whoseMove == null) {
                Debug.LogError("NoWhoseMove");
                return;
            }

            if (res.loadPanel.date == null) {
                Debug.LogError("NoDate");
                return;
            }

            if (res.loadPanel.kind == null) {
                Debug.LogError("NoKind");
                return;
            }

            if (res.loadPanel.delete == null) {
                Debug.LogError("NoDelete");
                return;
            }

            if (res.loadPanel.load == null) {
                Debug.LogError("NoLoad");
                return;
            }
            if (saves == null) {
                Debug.LogError("SaveListIsNull");
                return;
            }

            var startSave = pageNumber * countSavesInPanel;
            for (int i = startSave; i < pageNumber * countSavesInPanel + countSavesInPanel; i++) {
                var curPanel = loadPanels[i - pageNumber * countSavesInPanel];

                curPanel.gameObject.SetActive(true);
                curPanel.delete.onClick.RemoveAllListeners();
                curPanel.load.onClick.RemoveAllListeners();
                if (i >= saves.Count) {
                    curPanel.gameObject.SetActive(false);
                    continue;
                }

                int curIndex = i;
                var fileName = saves[curIndex].fileName;

                curPanel.date.text = saves[curIndex].saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                curPanel.kind.text = "Checker Kind: " + saves[curIndex].checkerKind.ToString();
                curPanel.delete.onClick.AddListener(() => gmController.DeleteFile(fileName));
                curPanel.delete.onClick.AddListener(() => Refresh());
                curPanel.delete.onClick.AddListener(() => FillPage(pageNumber));
                curPanel.delete.onClick.AddListener(() => {
                        if (File.Exists(fileName)) {
                            Debug.LogError("FileNotDeleted");
                        }
                    }
                );

                curPanel.load.onClick.AddListener(() => gmController.Load(fileName));
                curPanel.load.onClick.AddListener(() => openMenu.onClick?.Invoke());
                curPanel.whoseMove.texture = res.checkerImages.whiteChecker.texture;
                curPanel.whoseMove.color = res.checkerImages.whiteChecker.color;
                if (saves[curIndex].whoseMove == controller.ChColor.Black) {
                    curPanel.whoseMove.texture = res.checkerImages.blackChecker.texture;
                    curPanel.whoseMove.color = res.checkerImages.blackChecker.color;
                }

                var imageBoard = curPanel.boardImage8x8;
                if (saves[curIndex].board.GetLength(0) == 10) {
                    continue;
                }

                for (int o = 0; o < saves[curIndex].board.GetLength(1); o++) {
                    for (int j = 0; j < saves[curIndex].board.GetLength(0); j++) {
                        var saveNum = o * saves[curIndex].board.GetLength(0) + j;

                        var emptyImage = res.checkerImages.emptyCell.texture;
                        imageBoard.boardCells[saveNum].texture = emptyImage;
                        imageBoard.boardCells[saveNum].color = res.checkerImages.emptyCell.color;
                        if (saves[curIndex].board[o, j].IsNone()) {
                            continue;
                        }
                        var checker = saves[i].board[o, j].Peel();
                        var checkerImage = res.checkerImages.whiteChecker;

                        if (checker.color == ChColor.White) {
                            if (checker.type == Type.King) {
                                checkerImage = res.checkerImages.whiteKing;
                            }
                        } else if (checker.color == ChColor.Black) {
                            if (checker.type == Type.King) {
                                checkerImage = res.checkerImages.blackKing;
                            } else if (checker.type == Type.Checker) {
                                checkerImage = res.checkerImages.blackChecker;
                            }
                        }

                        imageBoard.boardCells[saveNum].texture = checkerImage.texture;
                        imageBoard.boardCells[saveNum].color = checkerImage.color;
                    }
                }

            }
        }
    }
}
