using System.Collections.Generic;
using UnityEngine;
using controller;
using System;

namespace ui {
    public class SaveStrorage : MonoBehaviour {
        public Controller gmController;
        public Action<int> onChangeSavesCount;
        public List<SaveInfo> saves;

        private void OnEnable() {
            saves = gmController.GetSavesInfo();
            onChangeSavesCount?.Invoke(saves.Count);
        }
    }
}
