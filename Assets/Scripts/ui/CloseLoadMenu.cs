using UnityEngine;

namespace ui {
    public class CloseLoadMenu : MonoBehaviour {
        public RefreshLoadPanels loadMenu;
        public Canvas loadMenuCanvas;

        private void Awake() {
            loadMenu.onCloseMenu += CloseMenu;
        }

        public void CloseMenu() {
            loadMenuCanvas.enabled = false;
        }
    }
}
