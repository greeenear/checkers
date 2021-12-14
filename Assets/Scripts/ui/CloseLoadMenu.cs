using UnityEngine;

namespace ui {
    public class CloseLoadMenu : MonoBehaviour {
        public RefreshLoadPanels loadMenu;
        public Canvas loadMenuCanvas;

        private void Awake() {
            loadMenu.onChangeSavesCount += CheckNeedMenuCLose;
        }

        public void CheckNeedMenuCLose(int saveCount) {
            if (saveCount == 0) loadMenuCanvas.enabled = false;
        }
    }
}
