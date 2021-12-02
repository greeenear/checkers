using UnityEngine;
using controller;
using UnityEngine.UI;

namespace ui {
    public class ContinueGame : MonoBehaviour {
        public Controller controller;
        public Button loadButton;

        private void Awake() {
            if (controller == null) {
                Debug.LogError("NoController");
                return;
            }

            if (loadButton == null) {
                Debug.LogError("NoLoadButton");
                return;
            }

            ChangeButtonInteractable();
        }

        public void ChangeButtonInteractable() {
            var saves = controller.GetSavesInfo();
            if (saves == null) {
                Debug.LogError("SavesIsNull");
                return;
            }
            loadButton.interactable = saves.Count != 0;
        }

        public void LoadLastSave() {
            var saves = controller.GetSavesInfo();
            if (saves == null) {
                Debug.LogError("SavesIsNull");
                return;
            }

            if (saves.Count == 0) {
                return;
            }

            var lastSave = saves[0];
            foreach (var save in saves) {
                if (lastSave.saveDate.CompareTo(save.saveDate) < 0) {
                    lastSave = save;
                }
            }

            var res = controller.Load(lastSave.fileName);
            if (res != Errors.None) {
                return;
            }
            controller.enabled = true;
        }
    }
}