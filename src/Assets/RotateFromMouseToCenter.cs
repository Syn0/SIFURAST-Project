using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateFromMouseToCenter : MonoBehaviour {
    public float multiplier = 1f;

	void Update () {
        transform.rotation = Quaternion.Euler(
            (Screen.height / 2f - Input.mousePosition.y) * multiplier,
            (Screen.width / 2f - Input.mousePosition.x) * multiplier,
            0f
            );
	}
}
