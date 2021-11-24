using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class ToggleEnable : MonoBehaviour {
        public MonoBehaviour component;

        public void ChangeEnable() {
            component.enabled = !component.enabled;
        }
    }
}
