using UnityEngine;
using controller;
using UnityEngine.UI;

namespace ui {
    public class SetSaveAction : MonoBehaviour {
        public Button saveBut;
        public Controller controller;
        public RefreshLoadPanels loadPanels;

        private void Awake() {
            if (saveBut == null) {
                Debug.LogError("NoBut");
                return;
            }

            if (controller == null) {
                Debug.LogError("NoController");
                return;
            }

            if (saveBut == null) {
                Debug.LogError("NoBut");
                return;
            }
            saveBut.onClick.AddListener(() => {
                    var savePath = controller.Save();
                    if (savePath != null) {
                        loadPanels.saves.Add(controller.GetSaveInfo(savePath));
                    } else {
                        Debug.Log("CantSave");
                    }
                }
            );
        }
    }
}

