using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates pseudo-random noise used to animate water.
/// </summary>
public sealed class WaterNoise : MonoBehaviour
{
    public float power = 3;
    public float scale = 1;
    public float timescale = 1;

    // Keeps track of how much time passed for Perlin noise function
    private float _xOffset;
    private float _yOffset;

    private MeshFilter _meshFilter;

    public void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
    }

    public void Update()
    {
        MakeNoise();

        _xOffset += Time.deltaTime * timescale;
        _yOffset += Time.deltaTime * timescale;
    }

    public void MakeNoise()
    {
        var vertices = _meshFilter.mesh.vertices;

        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i].z = CalculateHeight(vertices[i].x, vertices[i].y) * power;
        }

        _meshFilter.mesh.vertices = vertices;
        _meshFilter.mesh.RecalculateBounds();
        _meshFilter.mesh.RecalculateNormals();
    }

    /// <summary>
    /// Calculates the height of the water at the given coordinates, using the Perlin Noise function.
    /// </summary>
    float CalculateHeight(float x, float y)
    {
        var xCoord = x * scale + _xOffset;
        var yCoord = y * scale + _yOffset;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
