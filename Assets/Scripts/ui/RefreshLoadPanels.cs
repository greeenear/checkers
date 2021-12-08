using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ui {
    [System.Serializable]
    public struct PageShowInfo {
        public int howManyPagesShow;
        public int howManySkipPages;
    }

    [System.Serializable]
    public struct PagePointers {
        public Button left;
        public Button right;
    }

    [System.Serializable]
    public struct LoadPanels {
        public List<LoadPanelRes> loadPanels;
        public RectTransform storage;
        public RectTransform panel;
    }

    public class RefreshLoadPanels : MonoBehaviour {
        public PageShowInfo pageShowInfo;
        public PagePointers pagePointers;
        public Controller gmController;
        public Button openMenu;
        public PageButRes lastPg;
        public LoadPanels loadPanelsData;

        public GameObject pageList;
        public UiResources res;

        private Dictionary<int, List<BoardImageRes>> boardsImageRef;
        private int curPage;

        private void Awake() {
            boardsImageRef = new Dictionary<int, List<BoardImageRes>>();
            if (res == null) {
                Debug.LogError("CantGetResources");
                return;
            }

            if (res.boardImages.boardImage10x10 == null) {
                Debug.LogError("NoBoardImage10x10");
                return;
            }

            if (res.boardImages.boardImage8x8 == null) {
                Debug.LogError("NoBoardImage8x8");
                return;
            }

            if (res.checkerImages.checkerImg == null) {
                Debug.LogError("NoBlackCheckerImage");
                return;
            }

            if (lastPg.text == null || lastPg.button == null) {
                Debug.LogError("LastPageNoRef");
                return;
            };
        }

        public void RefreshPagesBut() {
            var sentencedHighlight = new List<Transform>();
            foreach (Transform child in pageList.transform) {
                sentencedHighlight.Add(child);
            }
            foreach (Transform child in sentencedHighlight) {
                Destroy(child.gameObject);
                child.SetParent(null);
            }

            int pageCount = loadPanelsData.storage.transform.childCount;
            var saves = gmController.GetSavesInfo();
            if (saves == null) return;

            if (saves.Count == 0) {
                openMenu.onClick.Invoke();
                openMenu.onClick.Invoke();
            }

            var countOfPage = saves.Count / pageCount;
            if (saves.Count % pageCount != 0) countOfPage++;

            var skipCount = pageShowInfo.howManySkipPages;
            if (countOfPage - curPage < pageShowInfo.howManyPagesShow - skipCount) {
                skipCount  = pageShowInfo.howManyPagesShow - countOfPage + curPage;
            }

            var start = Mathf.Clamp(curPage - skipCount, 0, int.MaxValue);
            var showCount = start + pageShowInfo.howManyPagesShow;
            var lastPage = Mathf.Min(showCount, countOfPage);
            for (int i = start; i < lastPage; i++) {
                var curBut = Instantiate(res.pageBut, pageList.transform);
                curBut.button.interactable = i != curPage;

                int loadNum = i;
                curBut.button.onClick.AddListener(() => {
                        curPage = loadNum;
                        FillPage();
                        RefreshPagesBut();
                    }
                );
                curBut.text.text = (i + 1).ToString();
            }

            lastPg.gameObject.SetActive(false);
            if (countOfPage > pageShowInfo.howManyPagesShow && lastPage != countOfPage) {
                lastPg.button.onClick.RemoveAllListeners();
                lastPg.text.text = countOfPage.ToString();
                lastPg.button.onClick.AddListener(() => {
                        curPage = countOfPage;
                        FillPage();
                        lastPg.gameObject.SetActive(false);
                    }
                );
                lastPg.gameObject.SetActive(true);
            }

            pagePointers.left.interactable = curPage != 0;
            pagePointers.right.interactable = curPage + 1 != countOfPage;
        }

        public void TurnPage(int dir) {
            curPage = curPage + dir;
            RefreshPagesBut();
            FillPage();
        }

        public void FillPage() {
            var saves = gmController.GetSavesInfo();
            if (saves == null || saves.Count == 0) return;

            var sizePanel = loadPanelsData.panel.sizeDelta.y;
            var countSavesInPanel = (int)(loadPanelsData.storage.sizeDelta.y / sizePanel);

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

            var startSave = curPage * countSavesInPanel;
            var howMatchSavesShow = 0;
            for (int i = 0; i < countSavesInPanel; i++) {
                var curPanel = loadPanelsData.loadPanels[i];
                int curIndex = i + startSave;
                if (curIndex >= saves.Count) {
                    if (i == 0) {
                        curPage = curPage - 1;
                        RefreshPagesBut();
                        FillPage();
                        return;
                    }
                    curPanel.gameObject.SetActive(false);
                    continue;
                }
                howMatchSavesShow++;
            }

            for (int i = 0; i < howMatchSavesShow; i++) {
                var curPanel = loadPanelsData.loadPanels[i];

                curPanel.gameObject.SetActive(true);
                curPanel.delete.onClick.RemoveAllListeners();
                curPanel.load.onClick.RemoveAllListeners();
                int curIndex = i + startSave;

                curPanel.date.text = saves[curIndex].saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                curPanel.kind.text = "Checker Kind: " + saves[curIndex].checkerKind.ToString();
                curPanel.delete.onClick.AddListener(() => {
                        if (gmController.DeleteFile(saves[curIndex].fileName) != Errors.None) {
                            Debug.LogError("cant delete");
                            return;
                        }
                        RefreshPagesBut();
                        FillPage();
                    }
                );

                curPanel.load.onClick.AddListener(() => {
                        if (gmController.Load(saves[curIndex].fileName) != Errors.None) {
                            Debug.LogError("cant load");
                            openMenu.onClick?.Invoke();
                            return;
                        }
                    }
                );

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
                    var emptyCell = res.checkerImages.emptyCell;
                    if (!boardsImageRef.ContainsKey(i) || saveLength != boardsImageRef[i].Count) {
                        var cellsList = new List<BoardImageRes>();
                        foreach (Transform child in imageBoard.transform) {
                            Destroy(child.gameObject);
                        }

                        foreach (var cell in saves[curIndex].board) {
                            cellsList.Add(Instantiate(emptyCell, imageBoard.transform));
                        }
                        boardsImageRef[i] = cellsList;
                    }

                    for (int j = 0; j < saves[curIndex].board.GetLength(0); j++) {
                        var saveNum = k * saves[curIndex].board.GetLength(0) + j;

                        boardsImageRef[i][saveNum].cell.texture = emptyCell.cell.texture;
                        boardsImageRef[i][saveNum].cell.color = emptyCell.cell.color;

                        var checkerOpt = saves[curIndex].board[k, j];
                        if (checkerOpt.IsNone()) continue;

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