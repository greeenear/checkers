using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ui {
    public class RefreshLoadPanels : MonoBehaviour {
        public int howManyPagesShow;
        public Controller gmController;
        public Button openMenu;
        public Button leftPointer;
        public Button rightPointer;
        public PageButRes lastPage;

        public List<LoadPanelRes> loadPanels;
        public RectTransform loadPanelsStorage;
        public RectTransform loadPanel;

        public GameObject pageList;
        public UiResources res;

        private Dictionary<int, List<BoardImageRes>> boardsImageRef;
        private int curPage;

        private void Awake() {
            boardsImageRef = new Dictionary<int, List<BoardImageRes>>();
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
            var countSavesInPanel = (int)(loadPanelsStorage.sizeDelta.y / loadPanel.sizeDelta.y);
            var sentencedHighlight = new List<Transform>();
            lastPage.gameObject.SetActive(false);

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

            if (saves.Count == 0) {
                openMenu.onClick.Invoke();
                return;
            }

            var countOfPage = saves.Count / countSavesInPanel;
            if (saves.Count % countSavesInPanel != 0) countOfPage++;

            leftPointer.gameObject.SetActive(true);
            if (curPage == 0) {
                leftPointer.gameObject.SetActive(false);
            }

            rightPointer.gameObject.SetActive(true);
            if (curPage + 1 == countOfPage) {
                rightPointer.gameObject.SetActive(false);
            }

            int showedPages = 0;
            int skipPages = 4;
            if (countOfPage - numberOfPage < howManyPagesShow - skipPages) {
                skipPages  = howManyPagesShow - countOfPage + numberOfPage;
            }

            var isLastPageShown = curPage + howManyPagesShow - skipPages != countOfPage;
            if (countOfPage > howManyPagesShow && isLastPageShown) {
                lastPage.gameObject.SetActive(true);
                var text = lastPage.GetComponentInChildren<Text>();
                if (text != null) {
                    text.text = (countOfPage).ToString();
                    lastPage.button.onClick.RemoveAllListeners();
                    lastPage.button.onClick.AddListener(() => FillPage(countOfPage));
                    lastPage.button.onClick.AddListener(() => Refresh(countOfPage));
                }
            }

            for (int i = numberOfPage - skipPages; i < countOfPage; i++) {
                if (showedPages == howManyPagesShow) {
                    break;
                }
                if (i < 0) {
                    continue;
                }

                var curBut = Instantiate(res.pageBut, pageList.transform);
                int loadNum = i;
                if (loadNum == curPage) {
                    curBut.button.interactable = false;
                }


                curBut.text.text = (loadNum + 1).ToString();
                curBut.button.onClick.AddListener(() => FillPage(loadNum));
                curBut.button.onClick.AddListener(() => Refresh(loadNum));
                if (curBut.text == null) {
                    continue;
                }
                showedPages++;
            }
        }

        public void SkipPages(int dir) {
            curPage = curPage + dir;
            if (curPage + dir < 0) {
                curPage = 0;
            }

            Refresh(curPage);
            FillPage(curPage);
        }

        public void FillPage(int pageNumber) {
            curPage = pageNumber;
            var saves = gmController.GetSavesInfo();
            if (saves == null || saves.Count == 0) {
                return;
            }

            var countSavesInPanel = (int)(loadPanelsStorage.sizeDelta.y / loadPanel.sizeDelta.y);

            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));

            if (res.loadPanel.whoseMove == null) {
                Debug.LogError("NoWhoseMove");
                return;
            }

            if (res.loadPanel.date == null) {
                Debug.LogError("NoDate");
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
                    if (i == 0) {
                        curPage = curPage - 1;
                        Refresh(curPage);
                        FillPage(curPage);
                        return;
                    }
                    curPanel.gameObject.SetActive(false);
                    continue;
                }

                curPanel.date.text = saves[curIndex].saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                curPanel.kind.text = "Checker Kind: " + saves[curIndex].checkerKind.ToString();
                curPanel.delete.onClick.AddListener(() => {
                        if (gmController.DeleteFile(saves[curIndex].fileName) != Errors.None) {
                            Debug.LogError("cant delete");
                            return;
                        }
                    }
                );

                curPanel.delete.onClick.AddListener(() => Refresh(pageNumber));
                curPanel.delete.onClick.AddListener(() => FillPage(pageNumber));
                curPanel.load.onClick.AddListener(() => { 
                        if (gmController.Load(saves[curIndex].fileName) != Errors.None) {
                            Debug.LogError("cant load");
                            return;
                        }
                    }
                );
                curPanel.load.onClick.AddListener(() => openMenu.onClick?.Invoke());

                curPanel.whoseMove.texture = res.checkerImages.checkerImg.texture;
                curPanel.whoseMove.color = res.checkerImages.checkerImg.color;
                if (saves[curIndex].whoseMove == controller.ChColor.Black) {
                    curPanel.whoseMove.color = Color.grey;
                }

                var imageBoard = curPanel.boardImage.boardImage8x8;
                curPanel.boardImage.boardImage10x10.gameObject.SetActive(false);
                curPanel.boardImage.boardImage8x8.gameObject.SetActive(true);
                if (saves[curIndex].board.GetLength(0) == 10) {
                    curPanel.boardImage.boardImage8x8.gameObject.SetActive(false);
                    curPanel.boardImage.boardImage10x10.gameObject.SetActive(true);
                    imageBoard = curPanel.boardImage.boardImage10x10;
                }

                for (int k = 0; k < saves[curIndex].board.GetLength(1); k++) {
                    var saveLength = saves[curIndex].board.Length;
                    if (!boardsImageRef.ContainsKey(i) || saveLength != boardsImageRef[i].Count) {
                        var cellsList = new List<BoardImageRes>();
                        foreach (Transform child in imageBoard.transform) {
                            Destroy(child.gameObject);
                        }

                        foreach (var cell in saves[curIndex].board) {
                            cellsList.Add(
                                Instantiate(
                                    res.checkerImages.emptyCell,
                                    imageBoard.transform
                                )
                            );
                        }
                        boardsImageRef[i] = cellsList;
                    }

                    for (int j = 0; j < saves[curIndex].board.GetLength(0); j++) {
                        var saveNum = k * saves[curIndex].board.GetLength(0) + j;

                        var emptyTexture = res.checkerImages.emptyCell.cell.texture;
                        var emptyColor = res.checkerImages.emptyCell.cell.color;
                        boardsImageRef[i][saveNum].cell.texture = emptyTexture;
                        boardsImageRef[i][saveNum].cell.color = emptyColor;

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
                        boardsImageRef[i][saveNum].cell.texture = checkerImage.texture;
                        boardsImageRef[i][saveNum].cell.color = color;
                    }
                }
            }
        }
    }
}