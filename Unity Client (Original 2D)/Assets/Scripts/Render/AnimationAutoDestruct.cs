using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Destroys the pertinent <see cref="GameObject"/> once an animation completes.
/// </summary>
public sealed class AnimationAutoDestruct : MonoBehaviour
{
    public float delay = 0.0f;

    public void Start()
    {
        Destroy(gameObject, this.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + delay);   
    }
}
