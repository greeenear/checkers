using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {
    private void Start() {
        
    }

    private void Update() {
        if (!Input.GetMouseButtonDown(0)) {
            return;
        }

        Ray ray;
        RaycastHit hit;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit, 100f)) {
            return;
        }

        
    }
}
