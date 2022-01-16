using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Noisemanager : MonoBehaviour
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;


    public RawImage noiseTextureImage;
    public Terrain noiseTerrain;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public AnimationCurve meshHeightCurve;
    public Material terrainMaterial;
    public int width = 256;
    public int height = 256;
    public TerrainType[] regions;
    public Layer[] layers;

    private MeshData meshData;
    private string[] _noiseTypes;
    private int _currentNoiseIndex, _lastNoiseIndex;
    private int _currentTextureIndex, _lastTextureIndex;
    private string[] _textureTypes;
    private Noise _noise;
    private float _scale,_lastScale;
    private float _gain, _lastgain;
    private float _startHeight, _lastStartHeight;
    private float _blendStrength, _lastBlendStrength;
    private float _textureScale, _lastTextureScale;
    private float _tintStrength, _lastTintStrength;
    private float _height, _lastheight;
    private float _lacunarity, _lastlacunarity; 
    private int _seed, _lastSeed;
    private int _octaves, _lastoctaves;
    private float maxNoiseHeight = float.MinValue;
    private float minNoiseHeight = float.MaxValue;
    private float topLeftx;
    private float topLeftz;
    private float minHeight;
     private float maxHeight;
    private void Awake()
    {
        _noiseTypes = Noise.NOISE_TYPES.Keys.ToArray();
        _textureTypes = layers.Select(x => x.texture.name).ToArray();
        for (int i = 0; i < regions.Length; i++)
        {
            regions[i].colour = Color.white;                        
        }
        _currentNoiseIndex = 0;
        _currentTextureIndex = 0;
        _scale = 0.1f;
        _seed = 0;
        _height = 1;
        _octaves = 1;
        _lacunarity = 0.0f;
        minHeight = 0.0f;
        maxHeight = 1.0f;
        _gain = 0.0f;
        topLeftz = (height - 1) / 2;
        topLeftx = (width - 1) / 2;
        _startHeight = layers[_currentTextureIndex].startHeight;
        _blendStrength = layers[_currentTextureIndex].blendStrength;
        _tintStrength = layers[_currentTextureIndex].tintStrength;
        _textureScale = layers[_currentTextureIndex].textureScale;
        meshData = new MeshData(width, height);
        _RecomputeNoise();
    }

    private void _RecomputeNoise()
    {
        System.Type NoiseClass = Noise.NOISE_TYPES[_noiseTypes[_currentNoiseIndex]];
        _noise = (Noise)System.Activator.CreateInstance(NoiseClass);
        _noise.Seed = _seed;
        layers[_currentTextureIndex].startHeight = _startHeight;
        layers[_currentTextureIndex].blendStrength = _blendStrength;
        layers[_currentTextureIndex].tintStrength = _tintStrength;
        layers[_currentTextureIndex].textureScale = _textureScale;
        float[,] noise = new float[width, height];
         maxNoiseHeight = float.MinValue;
         minNoiseHeight = float.MaxValue;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = 0.0f;

                //
                // Initial values
                float amplitude = 1f;
                float frequency = 1f;

                for (int i = 0; i < _octaves; i++)
                {
                    float sampley = y * frequency;
                    float samplex = x * frequency;
                    float perlinvalue = _noise.GetNoiseMap(samplex, sampley, _scale) * 2 - 1;
                    value += amplitude * perlinvalue;
                    frequency *= _lacunarity;
                    amplitude *= _gain;
                }
                if (value > maxNoiseHeight)
                {
                    maxNoiseHeight = value;
                }
                else if (value < minNoiseHeight)
                {
                    minNoiseHeight = value;
                }
                noise[y, x] = value;
            }
        }
        _SetNioseTexture(noise);
    }

    private void _SetNioseTexture(float[,] noise)
    {
        Color[] pixels = new Color[width * height];
        int vertextIndex = 0;
        meshData.triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                
                noise[y, x] = Mathf.InverseLerp(minNoiseHeight,maxNoiseHeight, noise[y,x]);
                meshData.vertices[vertextIndex] = new Vector3(topLeftx + x, meshHeightCurve.Evaluate(noise[y, x]) * _height, topLeftz - y);
                meshData.uvs[vertextIndex] = new Vector2(x / (float)width, y / (float)height);
                float currentHeight = noise[y, x];
                pixels[x + width * y] = Color.Lerp(Color.black, Color.white, noise[y, x]);
               
                if (x < width - 1 && y < height - 1) {
                    meshData.AddTriangle(vertextIndex, vertextIndex + width + 1, vertextIndex + width);
                    meshData.AddTriangle(vertextIndex + width + 1, vertextIndex, vertextIndex + 1);
                }
                vertextIndex++;
            }
        }
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        meshFilter.sharedMesh = meshData.CreateMesh ();
        meshRenderer.sharedMaterial.mainTexture = texture;
        noiseTextureImage.texture = texture;
        minHeight = meshHeightCurve.Evaluate(0) * _height;
        maxHeight = meshHeightCurve.Evaluate(1) * _height;
        terrainMaterial.SetInt("layerCount", layers.Length);
        terrainMaterial.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        terrainMaterial.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        terrainMaterial.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        terrainMaterial.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
        terrainMaterial.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        terrainMaterial.SetTexture("baseTextures", texturesArray);

        Debug.Log("Heights updated");
        terrainMaterial.SetFloat("minHeight", minHeight);
        terrainMaterial.SetFloat("minHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures) {
        Texture2DArray textureArray = new Texture2DArray(textureSize,textureSize,textures.Length,textureFormat,true);

        for (int i = 0; i < textures.Length; i++) {

            textureArray.SetPixels(textures[i].GetPixels(), i);
        
        }
        textureArray.Apply();
        return textureArray;
    
    }

    private void _UpdateUI()
    {
        if (_scale == _lastScale && 
            _seed == _lastSeed && 
            _currentNoiseIndex == _lastNoiseIndex &&
            _height == _lastheight &&
            _gain == _lastgain &&
            _startHeight == _lastStartHeight &&
            _blendStrength == _lastBlendStrength &&
            _tintStrength == _lastTintStrength &&
            _textureScale == _lastTextureScale &&
            _lacunarity == _lastlacunarity &&
            _octaves == _lastoctaves &&
            _currentTextureIndex == _lastTextureIndex)
            return;

        if (_currentTextureIndex != _lastTextureIndex)
        {
            _startHeight = layers[_currentTextureIndex].startHeight;
            _blendStrength = layers[_currentTextureIndex].blendStrength;
            _tintStrength = layers[_currentTextureIndex].tintStrength;
            _textureScale = layers[_currentTextureIndex].textureScale;
            _lastTextureIndex = _currentTextureIndex;
        }
        else
        {
            _RecomputeNoise();

            _lastScale = _scale;
            _lastSeed = _seed;
            _lastNoiseIndex = _currentNoiseIndex;
            _lastTextureIndex = _currentTextureIndex;
            _lastlacunarity = _lacunarity;
            _lastgain = _gain;
            _lastBlendStrength = _blendStrength;
            _lastStartHeight = _startHeight;
            _lastTintStrength = _tintStrength;
            _lastTextureScale = _textureScale;
            _lastheight = _height;
            _lastoctaves = _octaves;
        }
    }


    private void OnGUI()
    {
        _currentNoiseIndex = GUI.SelectionGrid(
            new Rect(0f,0f,100f,_noiseTypes.Length * 25f),
            _currentNoiseIndex,
            _noiseTypes,
            1
        );

        _currentTextureIndex = GUI.SelectionGrid(
    new Rect(950f, 0f, 100f, _textureTypes.Length * 25f),
    _currentTextureIndex,
    _textureTypes,
    1
);
        string _scaleStr = _scale.ToString("0.###");
        GUI.Label(new Rect(110f, 0f, 110f, 20f), $"Scale = {_scaleStr}");
        _scale = GUI.HorizontalSlider(new Rect(110f, 20f, 110f, 20f), _scale, 0.001f, 0.3f);

        GUI.Label(new Rect(110f, 40f, 110f, 20f), $"Seed = {_seed}");
        _seed = (int)GUI.HorizontalSlider(new Rect(110f, 60f, 110f, 20f), _seed, 0, 50);

        GUI.Label(new Rect(110f, 80f, 110f, 20f), $"Lacunarity = {_lacunarity}");
        _lacunarity = GUI.HorizontalSlider(new Rect(110f, 100f, 110f, 20f), _lacunarity, 0.0f, 2.0f);

        GUI.Label(new Rect(110f, 120f, 110f, 20f), $"Gain = {_gain}");
        _gain = GUI.HorizontalSlider(new Rect(110f, 140f, 110f, 20f), _gain, 0.0f, 1.0f);

        GUI.Label(new Rect(110f, 160f, 110f, 20f), $"Octaves = {_octaves}");
        _octaves = (int)GUI.HorizontalSlider(new Rect(110f, 180f, 110f, 20f), _octaves, 0, 16);

        GUI.Label(new Rect(110f, 200f, 110f, 20f), $"Height = {_height}");
        _height = GUI.HorizontalSlider(new Rect(110f, 220f, 110f, 20f), _height, 1.0f, 20.0f);






        GUI.Label(new Rect(1060f, 0f, 110f, 20f), $"Start Height = {_startHeight}");
        _startHeight = GUI.HorizontalSlider(new Rect(1060f, 20f, 110f, 20f), _startHeight, 0.0f, 1.0f);

        GUI.Label(new Rect(1060f, 40f, 110f, 20f), $"Blend Strength = {_blendStrength}");
        _blendStrength = GUI.HorizontalSlider(new Rect(1060f, 60f, 110f, 20f), _blendStrength, 0.0f, 1.0f);

        GUI.Label(new Rect(1060f, 80f, 110f, 20f), $"Tint Strength = {_tintStrength}");
        _tintStrength = GUI.HorizontalSlider(new Rect(1060f, 100f, 110f, 20f), _tintStrength, 0.0f, 1.0f);

        GUI.Label(new Rect(1060f, 120f, 110f, 20f), $"Texture Scale = {_textureScale}");
        _textureScale = GUI.HorizontalSlider(new Rect(1060f, 140f, 110f, 20f), _textureScale, 0.0f, 25.0f);

        if (GUI.changed)
            _UpdateUI();
        UnityEditor.EditorApplication.update += _UpdateUI;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;

    }

}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color colour;
}

public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    public int triangleIndex;

    public MeshData(int meshWidth, int meshHeight) {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals ();
        return mesh;
    }
}
