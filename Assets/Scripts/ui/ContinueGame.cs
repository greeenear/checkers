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
            if (saves == null || saves.Count == 0) {
                loadButton.interactable = false;
                return;
            }

            loadButton.interactable = true;
            return;
        }

        public void LoadLastSave() {
            controller.enabled = true;
            var saves = controller.GetSavesInfo();
            if (saves == null) {
                Debug.LogError("SavesIsNull");
                return;
            }

            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));
            controller.Load(saves[0].fileName);
        }
    }
}