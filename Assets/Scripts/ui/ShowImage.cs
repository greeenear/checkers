using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ui {
    public class ShowImage : MonoBehaviour {
        public GameObject image;
        public DelayTimer timer;

        public async void SuccessfulSaving() {
            image.SetActive(true);
            await timer.Timer(2);
            image.SetActive(false);
        }
    }
}