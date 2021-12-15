using System;
using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ui {
    [System.Serializable]
    public struct PageShowInfo {
        public int maxVisiblePage;
        public int leftPageRadius;
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
        public Action<int> onChangeSavesCount;

        public PageShowInfo pageShowInfo;
        public PagePointers pagePointers;
        public Controller gmController;
        public Button openMenu;
        public LoadPanels loadPanelsData;
        public List<SaveInfo> saves;

        public GameObject pageList;
        public UiResources res;

        private Dictionary<int, List<BoardImageRes>> boardsImageRef;
        private int curPage;
        private List<PageButRes> pageButs = new List<PageButRes>();

        private void Awake() {
            boardsImageRef = new Dictionary<int, List<BoardImageRes>>();
        }

        private void OnEnable() {
            saves = gmController.GetSavesInfo();
        }

        public void RefreshPagesBut() {
            if (res == null) {
                Debug.LogError("CantGetResources");
                return;
            }

            if (res.pageBut == null) {
                Debug.LogError("CantGetResources");
                return;
            }

            if (res.pageBut.button == null) {
                Debug.LogError("CantGetResources");
                return;
            }

            var pageButtons = new List<Transform>();
            if (pageButs == null || pageButs.Count == 0) {
                pageButs = new List<PageButRes>();
                for (int i = 0; i < pageShowInfo.maxVisiblePage; i++) {
                    pageButs.Add(Instantiate(res.pageBut, pageList.transform));
                }
            }

            int saveCount = loadPanelsData.storage.transform.childCount;
            if (saves == null) return;

            var countOfPage = saves.Count / saveCount;
            if (saves.Count % saveCount != 0) countOfPage++;

            var leftPageRadius = pageShowInfo.leftPageRadius;
            if (countOfPage - curPage < pageShowInfo.maxVisiblePage - leftPageRadius) {
                leftPageRadius  = pageShowInfo.maxVisiblePage - countOfPage + curPage;
            }

            var start = Mathf.Clamp(curPage - leftPageRadius, 0, int.MaxValue);
            var showCount = start + pageShowInfo.maxVisiblePage;
            var lastPage = Mathf.Min(showCount, countOfPage);
            if (res.spaceBetweenButtons == null) {
                Debug.LogError("CantGetSpaceBetweenButtons");
                return;
            }

            for (int i = start; i < lastPage; i++) {
                var curBut = pageButs[i - start];
                curBut.image.sprite = res.pageBut.image.sprite;
                curBut.gameObject.SetActive(true);

                curBut.button.onClick.RemoveAllListeners();
                curBut.button.interactable = i != curPage;

                int loadNum = i;
                if (i == lastPage - 1) loadNum = countOfPage - 1;
                if (i == start) loadNum = 0;

                if (lastPage != countOfPage && i == lastPage - 2 || start > 0 && i == start + 1) {
                    if (curBut.toggleImageRes != null) {
                        curBut.toggleImageRes.Change();
                        curBut.text.text = "";
                        continue;
                    }
                }

                curBut.button.onClick.AddListener(() => {
                        curPage = loadNum;
                        FillPage();
                        RefreshPagesBut();
                    }
                );
                curBut.text.text = (loadNum + 1).ToString();

                if (i == lastPage - 1) {
                    for (int j = lastPage; j < pageButs.Count; j++) {
                        pageButs[j].gameObject.SetActive(false);
                    }
                }
            }
            pagePointers.left.interactable = curPage != 0;
            pagePointers.right.interactable = curPage + 1 != countOfPage;
        }

        public void TurnPage(int dir) {
            if (curPage + dir < 0 || curPage + dir > saves.Count) {
                Debug.LogError("IndexOutOfRange");
                return;
            }
            curPage = curPage + dir;
            RefreshPagesBut();
            FillPage();
        }

        public void FillPage() {
            if (res == null) {
                Debug.LogError("CantGetResources");
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

            if (saves == null || saves.Count == 0) return;

            var sizePanel = loadPanelsData.panel.sizeDelta.y;
            var countSavesInPanel = (int)(loadPanelsData.storage.sizeDelta.y / sizePanel);

            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));


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
            var noLoadBut = res.loadPanel.load == null;

            for (int i = 0; i < howMatchSavesShow; i++) {
                var curPanel = loadPanelsData.loadPanels[i];
                int curIndex = i + startSave;
                var curSave = saves[curIndex];
                var fileName = curSave.fileName;
                if (noLoadBut) {
                    Debug.LogError("NoLoad");
                } else {
                    curPanel.load.onClick.RemoveAllListeners();
                    curPanel.load.onClick.AddListener(() => {
                            if (!gmController.Load(fileName)) {
                                Debug.LogError("cant load");
                                openMenu.onClick?.Invoke();
                                return;
                            }
                            openMenu.onClick?.Invoke();
                        }
                    );
                }

                if (curPanel.delete == null) {
                    Debug.LogError("NoDelete");
                } else {
                    curPanel.delete.onClick.RemoveAllListeners();
                    curPanel.delete.onClick.AddListener(() => {
                            if (!gmController.DeleteFile(fileName)) {
                                Debug.LogError("cant delete");
                                return;
                            }
                            saves.Remove(curSave);
                            onChangeSavesCount?.Invoke(saves.Count);
                            RefreshPagesBut();
                            FillPage();
                        }
                    );
                }

                if (curPanel.whoseMove == null) {
                    Debug.LogError("NoWhoseMove");
                } else {
                    curPanel.whoseMove.texture = res.checkerImages.checkerImg.texture;
                    curPanel.whoseMove.color = res.checkerImages.checkerImg.color;
                    if (curSave.whoseMove == controller.ChColor.Black) {
                        curPanel.whoseMove.color = Color.grey;
                    }
                }

                if (curPanel.date.text == null) {
                    Debug.LogError("NoLoadPanelText");
                } else {
                    curPanel.date.text = curSave.saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                    curPanel.kind.text = "Checker Kind: " + curSave.checkerKind.ToString();
                }
                curPanel.gameObject.SetActive(true);

                var boardImage = curPanel.boardImage;
                if (boardImage.boardImage8x8 == null || boardImage.boardImage10x10 == null) {
                    Debug.LogError("NoBoardImage");
                    return;
                }
                if (res.checkerImages.emptyCell == null || res.checkerImages.kingImg == null){
                    return;
                }

                var imageBoard = curPanel.boardImage.boardImage8x8;
                curPanel.boardImage.boardImage10x10.gameObject.SetActive(false);
                curPanel.boardImage.boardImage8x8.gameObject.SetActive(true);
                if (curSave.board.GetLength(0) == 10) {
                    curPanel.boardImage.boardImage8x8.gameObject.SetActive(false);
                    curPanel.boardImage.boardImage10x10.gameObject.SetActive(true);
                    imageBoard = curPanel.boardImage.boardImage10x10;
                }

                for (int k = 0; k < curSave.board.GetLength(1); k++) {
                    var saveLength = curSave.board.Length;
                    var emptyCell = res.checkerImages.emptyCell;
                    if (!boardsImageRef.ContainsKey(i) || saveLength != boardsImageRef[i].Count) {
                        var cellsList = new List<BoardImageRes>();
                        foreach (Transform child in imageBoard.transform) {
                            Destroy(child.gameObject);
                        }

                        foreach (var cell in curSave.board) {
                            cellsList.Add(Instantiate(emptyCell, imageBoard.transform));
                        }
                        boardsImageRef[i] = cellsList;
                    }

                    var cellsRef = boardsImageRef[i];

                    for (int j = 0; j < curSave.board.GetLength(0); j++) {
                        var saveNum = k * curSave.board.GetLength(0) + j;

                        cellsRef[saveNum].cell.texture = emptyCell.cell.texture;
                        cellsRef[saveNum].cell.color = emptyCell.cell.color;

                        var checkerOpt = curSave.board[k, j];
                        if (checkerOpt.IsNone()) continue;

                        var checker = checkerOpt.Peel();
                        var checkerImage = res.checkerImages.checkerImg;

                        var color = res.checkerImages.checkerImg.color;
                        if (checker.color == ChColor.White) {
                            if (checker.type == ChType.King) {
                                checkerImage = res.checkerImages.kingImg;
                            }
                        } else if (checker.color == ChColor.Black) {
                            color = Color.gray;
                            if (checker.type == ChType.King) {
                                checkerImage = res.checkerImages.kingImg;
                            } else if (checker.type == ChType.Checker) {
                                checkerImage = res.checkerImages.checkerImg;
                            }
                        }
                        cellsRef[saveNum].cell.texture = checkerImage.texture;
                        cellsRef[saveNum].cell.color = color;
                    }
                }
            }
        }
    }
}