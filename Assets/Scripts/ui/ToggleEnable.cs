using UnityEngine;

namespace ui {
    public class ToggleEnable : MonoBehaviour {
        public MonoBehaviour component;

        public void ChangeEnable() {
            if (component != null) component.enabled = !component.enabled;
        }
    }
}
