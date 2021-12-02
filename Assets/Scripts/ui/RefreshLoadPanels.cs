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
        private int curPage;
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

            if (res.checkerImages.checkerImg == null) {
                Debug.LogError("NoBlackCheckerImage");
                this.enabled = false;
                return;
            }
        }

        public void Refresh(int numberOfPage) {
            curPage = numberOfPage;
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

            var countOfPage = saves.Count / countSavesInPanel;
            if (saves.Count % countSavesInPanel != 0) countOfPage++;

            int showed = 0;
            int skipPages = 4;
            if (countOfPage - numberOfPage < 12 - skipPages) {
                skipPages  = skipPages + (12 - skipPages - countOfPage + numberOfPage);//shit
            }

            for (int i = numberOfPage - skipPages; i < countOfPage; i++) {
                if (showed == 12) {
                    break;
                }
                if (i < 0) {
                    continue;
                }

                var curBut = Instantiate(res.pageBut, pageList.transform);
                var text = curBut.GetComponentInChildren<Text>();
                int loadNum = i;
                if (loadNum == curPage) {
                    curBut.interactable = false;
                }
                text.text = loadNum.ToString();
                curBut.onClick.AddListener(() => FillPage(loadNum));
                curBut.onClick.AddListener(() => Refresh(loadNum));
                showed++;
            }
        }

        public void SkipPages(int dir) {
            Refresh(curPage + 12 * dir);
        }

        public void FillPage(int pageNumber) {
            var saves = gmController.GetSavesInfo();
            if (saves == null) {
                return;
            }

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
            for (int i = 0; i < countSavesInPanel; i++) {
                var curPanel = loadPanels[i];

                curPanel.gameObject.SetActive(true);
                curPanel.delete.onClick.RemoveAllListeners();
                curPanel.load.onClick.RemoveAllListeners();

                int curIndex = i + startSave;
                if (curIndex >= saves.Count) {
                    curPanel.gameObject.SetActive(false);
                    continue;
                }

                var fileName = saves[curIndex].fileName;

                curPanel.date.text = saves[curIndex].saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                curPanel.kind.text = "Checker Kind: " + saves[curIndex].checkerKind.ToString();
                curPanel.delete.onClick.AddListener(() => {
                        if (gmController.DeleteFile(fileName) != Errors.None) {
                            Debug.LogError("cant delete");
                            return;
                        }
                    }
                );
                curPanel.delete.onClick.AddListener(() => Refresh(pageNumber));
                curPanel.delete.onClick.AddListener(() => FillPage(pageNumber));

                curPanel.load.onClick.AddListener(() => { 
                        if (gmController.Load(fileName) != Errors.None) {
                            Debug.LogError("cant load");
                            return;
                        }
                    }
                );

                curPanel.load.onClick.AddListener(() => openMenu.onClick?.Invoke());
                curPanel.whoseMove.texture = res.checkerImages.checkerImg.texture;
                curPanel.whoseMove.color = res.checkerImages.checkerImg.color;
                if (saves[curIndex].whoseMove == controller.ChColor.Black) {
                    curPanel.whoseMove.texture = res.checkerImages.checkerImg.texture;
                    curPanel.whoseMove.color = res.checkerImages.checkerImg.color;
                }

                var imageBoard = curPanel.boardImage8x8;
                if (saves[curIndex].board.GetLength(0) == 10) {
                    continue;
                }

                for (int k = 0; k < saves[curIndex].board.GetLength(1); k++) {
                    for (int j = 0; j < saves[curIndex].board.GetLength(0); j++) {
                        var saveNum = k * saves[curIndex].board.GetLength(0) + j;

                        var emptyImage = res.checkerImages.emptyCell.texture;
                        imageBoard.boardCells[saveNum].texture = emptyImage;
                        imageBoard.boardCells[saveNum].color = res.checkerImages.emptyCell.color;

                        var checkerOpt = saves[curIndex].board[k, j];
                        if (checkerOpt.IsNone()) {
                            continue;
                        }
                        var checker = checkerOpt.Peel();
                        var checkerImage = res.checkerImages.checkerImg;

                        var color = res.checkerImages.checkerImg.color;
                        if (checker.color == ChColor.White) {
                            if (checker.type == Type.King) {
                                checkerImage = res.checkerImages.kingImg;
                            }
                        } else if (checker.color == ChColor.Black) {
                            color = Color.gray;
                            if (checker.type == Type.King) {
                                checkerImage = res.checkerImages.kingImg;
                            } else if (checker.type == Type.Checker) {
                                checkerImage = res.checkerImages.checkerImg;
                            }
                        }

                        imageBoard.boardCells[saveNum].texture = checkerImage.texture;
                        imageBoard.boardCells[saveNum].color = color;
                    }
                }
            }
        }
    }
}
