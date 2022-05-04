using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ArController : MonoBehaviour
{
    public bool isChange = true;
    public ARRaycastManager raycastManager;
    public Camera mainCamera;
    public GameObject prefab;
    public Transform board;

    private GameObject curObject;
    private void Start() {
        curObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
    }
    void Update() {
        var hits = new List<ARRaycastHit>();
        var raycast = raycastManager.Raycast(
            new Vector2(Screen.width / 2, Screen.height / 2),
            hits,
            TrackableType.PlaneWithinPolygon
        );

        if (hits.Count > 0 && isChange) {
            curObject.transform.position = hits[0].pose.position;
            board.position = curObject.transform.position;
            board.up = Vector3.up;
        }

        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began) {
            return;
        }
    }

    public void ToggleChangePos() {
        isChange = !isChange;
    }

    private void OnDisable() {
        board.position = curObject.transform.position;
        board.up = Vector3.up;
        Destroy(curObject);
    }
}
