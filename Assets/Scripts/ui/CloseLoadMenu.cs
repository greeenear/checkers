using UnityEngine;

namespace ui {
    public class CloseLoadMenu : MonoBehaviour {
        public SaveStrorage saves;
        public Canvas loadMenuCanvas;

        private void Awake() {
            saves.onChangeSavesCount += CheckNeedMenuCLose;
        }

        public void CheckNeedMenuCLose(int saveCount) {
            if (saveCount == 0) loadMenuCanvas.enabled = false;
        }
    }
}
