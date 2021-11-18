using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ui {
    public class ShowImage : MonoBehaviour {
        public GameObject image;
        public DelayTimer timer;

        private async void SuccessfulSaving() {
            image.gameObject.SetActive(true);
            await timer.Timer(2);
            image.gameObject.SetActive(false);
        }
    }
}