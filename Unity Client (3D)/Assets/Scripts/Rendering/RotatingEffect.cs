using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates the attached <see cref="GameObject"/> transform to always face the given camera.
/// </summary>
public sealed class RotatingEffect : MonoBehaviour
{
    [Tooltip("Camera with which this GameObject should be aligned")]
    public GameObject MainCamera;

    public void Update()
    {
        // Update GameObject's facing direction
        gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.eulerAngles.x, MainCamera.transform.rotation.eulerAngles.y, gameObject.transform.rotation.eulerAngles.z);
    }
}
