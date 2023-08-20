using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a mesh collider for large terrain objects.
/// Use the <see cref="MeshColliderGenerator"/> on parent objects of terrain meshes!
/// </summary>
public sealed class MeshColliderGenerator : MonoBehaviour
{
    public void Start()
    {
        var counter = 0;
        foreach (var child in transform)
        {
            if (child.gameObject.GetComponent<MeshCollider>() == null)
                child.gameObject.AddComponent(typeof(MeshCollider));
            counter++;
        }
        
        UnityEngine.Debug.Log("Generated " + counter + " terrain mesh colliders");
    }
}
