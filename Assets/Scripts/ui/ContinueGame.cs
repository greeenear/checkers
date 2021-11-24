using UnityEngine;
using controller;

namespace ui {
    public class ContinueGame : MonoBehaviour {
        public Controller controller;

        public void LoadLastSave() {
            var saves = controller.GetSavesInfo();
            if (saves.Count == 0) {
                // => doing something
                return;
            }

            saves.Sort((f1, f2) => f2.saveDate.CompareTo(f1.saveDate));
            controller.Load(saves[0].fileName);
        }
    }
}
