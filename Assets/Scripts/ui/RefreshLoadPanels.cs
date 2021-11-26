using UnityEngine;
using controller;
using UnityEngine.UI;

namespace ui {
    public class RefreshLoadPanels : MonoBehaviour {
        public Controller gmController;
        public Button openMenu;
        public RectTransform savePanelsStorage;
        public UiResources res;

        private void Awake() {
            if (res == null) {
                Debug.LogError("CantGetResources");
                this.enabled = false;
                return;
            }

            if (res.boardImage10x10 == null) {
                Debug.LogError("NoBoardImage10x10");
                this.enabled = false;
                return;
            }

            if (res.boardImage8x8 == null) {
                Debug.LogError("NoBoardImage8x8");
                this.enabled = false;
                return;
            }

            if (res.blackChecker == null) {
                Debug.LogError("NoBlackCheckerImage");
                this.enabled = false;
                return;
            }

            if (res.whiteChecker == null) {
                Debug.LogError("NoWhiteCheckerImage");
                this.enabled = false;
                return;
            }
        }

        public void Refresh() {
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }

            var saves = gmController.GetSavesInfo();
            if (saves == null) {
                Debug.LogError("SaveListIsNull");
                return;
            }

            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));
            foreach (var save in saves) {
                var curPanel = Instantiate(
                    res.loadPanel,
                    Vector3.zero,
                    Quaternion.identity,
                    savePanelsStorage.transform
                );

                if (curPanel.boardImage == null) {
                    Debug.LogError("NoBoardImageRef");
                    return;
                }

                if (curPanel.whoseMove == null) {
                    Debug.LogError("NoWhoseMoveRef");
                    return;
                }

                if (curPanel.date == null) {
                    Debug.LogError("NoDateRef");
                    return;
                }

                if (curPanel.kind == null) {
                    Debug.LogError("NoKindRef");
                    return;
                }

                if (curPanel.delete == null) {
                    Debug.LogError("NoDeleteRef");
                    return;
                }

                if (curPanel.load == null) {
                    Debug.LogError("NoLoadRef");
                    return;
                }

                curPanel.date.text = save.saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                curPanel.kind.text = "Checker Kind: " + save.checkerKind.ToString();
                curPanel.delete.onClick.AddListener(() => gmController.DeleteFile(save.fileName));
                curPanel.delete.onClick.AddListener(() => Destroy(curPanel.gameObject));
                curPanel.load.onClick.AddListener(() => gmController.Load(save.fileName));
                curPanel.load.onClick.AddListener(() => openMenu.onClick?.Invoke());

                var imageBoardPrefab = res.boardImage10x10;
                if (save.board.GetLength(0) < 10) {
                    imageBoardPrefab = res.boardImage8x8;
                }

                var boardGrid = Instantiate(imageBoardPrefab, curPanel.boardImage.transform);
                for (int i = 0; i < save.board.GetLength(1); i++) {
                    for (int j = 0; j < save.board.GetLength(0); j++) {
                        if (save.board[i, j].IsNone()) {
                            Instantiate(res.emptyCell, boardGrid.transform);
                            continue;
                        }
                        var checker = save.board[i, j].Peel();
                        var checkerImage = res.whiteChecker;

                        if (checker.color == ChColor.White) {
                            if (checker.type == Type.King) {
                                checkerImage = res.whiteKing;
                            }
                        } else if (checker.color == ChColor.Black) {
                            if (checker.type == Type.King) {
                                checkerImage = res.blackKing;
                            } else if (checker.type == Type.Checker) {
                                checkerImage = res.blackChecker;
                            }
                        }

                        Instantiate(checkerImage, boardGrid.transform);
                    }
                }

                var whoseMovePref = res.whiteChecker;
                if (save.whoseMove == controller.ChColor.Black) {
                    whoseMovePref = res.blackChecker;
                }
                Instantiate(whoseMovePref, curPanel.whoseMove.transform);
            }
        }
    }
}
