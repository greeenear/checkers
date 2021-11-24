using UnityEngine;
using controller;
using UnityEngine.UI;

namespace ui {
    public class RefreshLoadPanels : MonoBehaviour {
        public Controller controller;
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
                //Destroy(child.gameObject);
            }

            var saves = controller.GetSavesInfo();
            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));
            foreach (var save in saves) {
                var curPanel = Instantiate(
                    res.loadPanel,
                    Vector3.zero,
                    Quaternion.identity,
                    savePanelsStorage.transform
                );
                FillBoardImage(curPanel.boardImage, save);
                curPanel.date.text = save.saveDate.ToString("dd.MM.yyyy HH:mm:ss");
                curPanel.kind.text = "Checker Kind: " + save.checkerKind.ToString();
                curPanel.delete.onClick.AddListener(() => controller.DeleteFile(save.fileName));
                curPanel.delete.onClick.AddListener(() => Destroy(curPanel.gameObject));
                curPanel.load.onClick.AddListener(() => controller.Load(save.fileName));
                curPanel.load.onClick.AddListener(() => openMenu.onClick?.Invoke());

                var whoseMovePref = res.whiteChecker;
                if (save.whoseMove == global::controller.ChColor.Black) {
                    whoseMovePref = res.blackChecker;
                }
                Instantiate(whoseMovePref, curPanel.whoseMove.transform);
            }
        }

        public void FillBoardImage(GameObject boardImageParent, SaveInfo save) {
            var imageBoardPrefab = res.boardImage10x10;
            if (save.board.GetLength(0) < 10) {
                imageBoardPrefab = res.boardImage8x8;
            }

            var boardGrid = Instantiate(imageBoardPrefab, boardImageParent.transform);
            for (int i = 0; i < save.board.GetLength(1); i++) {
                for (int j = 0; j < save.board.GetLength(0); j++) {
                    if (save.board[i, j].IsNone()) {
                        Instantiate(res.emptyCell, boardGrid.transform);
                        continue;
                    }
                    var checker = save.board[i, j].Peel();
                    var checkerImage = res.whiteChecker;
                    if (checker.color == ChColor.White && checker.type == Type.King) {
                        checkerImage = res.whiteKing;
                    } else if (checker.color == ChColor.Black && checker.type == Type.King) {
                        checkerImage = res.blackKing;
                    } else if (checker.color == ChColor.Black && checker.type == Type.Checker) {
                        checkerImage = res.blackChecker;
                    }

                    Instantiate(checkerImage, boardGrid.transform);
                }
            }
        }
    }
}
