using UnityEngine;
using controller;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ui {
    public class RefreshLoadPanels : MonoBehaviour {
        public Controller gmController;
        public Button openMenu;
        public RectTransform savePanelsStorage;
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
            var sentencedHighlight = new List<Transform>();
            foreach (Transform child in pageList.transform) {
                sentencedHighlight.Add(child);
            }
            foreach (Transform child in sentencedHighlight) {
                Destroy(child.gameObject);
                child.SetParent(null);
            }

            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }

            var saves = gmController.GetSavesInfo();
            if (saves == null) {
                Debug.LogError("SaveListIsNull");
                return;
            }
            var numberOfPage = saves.Count / 4;
            if (saves.Count % 4 != 0) numberOfPage++;

            for (int i = 0; i < numberOfPage; i++) {
                var curBut = Instantiate(res.pageBut, pageList.transform);
                int loadNum = i;
                curBut.onClick.AddListener(() => ShowOnePage(loadNum));
            }
            ShowOnePage(0);
        }

        public void ShowOnePage(int pageNumber) {
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }

            var saves = gmController.GetSavesInfo();

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

            for (int i = pageNumber * 4; i < pageNumber * 4 + 4; i++) {
                if (i >= saves.Count) break;
                var curPanel = Instantiate(
                    res.loadPanel,
                    Vector3.zero,
                    Quaternion.identity,
                    savePanelsStorage.transform
                );

                curPanel.date.text = saves[i].saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                int curIndex = i;
                curPanel.kind.text = "Checker Kind: " + saves[curIndex].checkerKind.ToString();
                curPanel.delete.onClick.AddListener(() => gmController.DeleteFile(saves[curIndex].fileName));
                curPanel.delete.onClick.AddListener(() => ShowOnePage(pageNumber));
                curPanel.delete.onClick.AddListener(() => Destroy(curPanel.gameObject));
                curPanel.load.onClick.AddListener(() => gmController.Load(saves[curIndex].fileName));
                curPanel.load.onClick.AddListener(() => openMenu.onClick?.Invoke());

                var imageBoardPrefab = res.boardImages.boardImage10x10;
                if (saves[curIndex].board.GetLength(0) < 10) {
                    imageBoardPrefab = res.boardImages.boardImage8x8;
                }

                var boardGrid = Instantiate(imageBoardPrefab, curPanel.boardImage.transform);
                for (int o = 0; o < saves[curIndex].board.GetLength(1); o++) {
                    for (int j = 0; j < saves[curIndex].board.GetLength(0); j++) {
                        if (saves[curIndex].board[o, j].IsNone()) {
                            Instantiate(res.checkerImages.emptyCell, boardGrid.transform);
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

                        Instantiate(checkerImage, boardGrid.transform);
                    }
                }

                var whoseMovePref = res.checkerImages.whiteChecker;
                if (saves[curIndex].whoseMove == controller.ChColor.Black) {
                    whoseMovePref = res.checkerImages.blackChecker;
                }
                Instantiate(whoseMovePref, curPanel.whoseMove.transform);
            }
        }
    }
}
