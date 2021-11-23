using UnityEngine;
using controller;
using UnityEngine.UI;

namespace ui {
    public class RefreshLoadPanels : MonoBehaviour {
        public Controller gameController;
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

            if (res.blackCheckerImage == null) {
                Debug.LogError("NoBlackCheckerImage");
                this.enabled = false;
                return;
            }

            if (res.whiteCheckerImage == null) {
                Debug.LogError("NoWhiteCheckerImage");
                this.enabled = false;
                return;
            }
        }

        public void InstantiateLoadPanels() {
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }

            var saves = gameController.GetSavesInfo();
            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));
            foreach (var save in saves) {
                var curPanel = Instantiate(
                    res.loadPanel,
                    Vector3.zero,
                    Quaternion.identity,
                    savePanelsStorage.transform
                );
                curPanel.FillPanel(curPanel.gameObject, save, res, gameController, openMenu);
            }
        }
    }
}
