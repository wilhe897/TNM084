using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteNoise : Noise
{
    private const float C = 1000;

    public override float GetNoiseMap(float x, float y, float scale = 1)
    {
        return Random.Range(0f, 1f);
    }
}

