using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class MaterialController : MonoBehaviour
{
    public Material TerrainMaterial;

    public readonly int MaxSupportedTextures = 4;

    [Range(1, 32)]
    public float Tesselation = 15;
    public float TesselationDistance = 40;

    [Range(0, 1)]
    public float LowThreshold;
    [Range(0, 1)]
    public float MidThreshold;
    [Range(0, 1)]
    public float HighThreshold;

    [Range(0, 1)]
    public float Specular;
    [Range(0, 1)]
    public float Gloss;

    public PbrTextureSet[] Textures;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateThresholds();
        UpdateMaterial();
    }

    void UpdateThresholds()
    {
        if (MidThreshold > HighThreshold)
        {
            MidThreshold = HighThreshold;
        }
        if (LowThreshold > MidThreshold)
        {
            LowThreshold = MidThreshold;
        }
    }

    void UpdateMaterial()
    {
        PbrTextureSet[] textures = new PbrTextureSet[MaxSupportedTextures];
        for (int i = 0; i < textures.Length; i++)
        {
            if(i >= Textures.Length)
            {
                textures[i] = new PbrTextureSet();
            }
            else
            {
                textures[i] = Textures[i];
            }
        }

        TerrainMaterial.SetFloat("_Tess", Tesselation);
        TerrainMaterial.SetFloat("_TessDst", TesselationDistance);

        for (int i = 1; i <= MaxSupportedTextures; i++)
        {
            if (!textures[i - 1].ValuesNotNull() && i > 1)
            {
                textures[i - 1] = textures[i - 2];
            }

            TerrainMaterial.SetTexture($"_MainTex{i}", textures[i - 1].Albedo);
            TerrainMaterial.SetTexture($"_DispTex{i}", textures[i - 1].Displacement);
            TerrainMaterial.SetTexture($"_NormalMap{i}", textures[i - 1].Normalmap);
            TerrainMaterial.SetFloat($"_Disp{i}", textures[i - 1].DisplacementAmount);
            TerrainMaterial.SetFloat($"_Tiling{i}", textures[i - 1].TilingAmount);
        }

        TerrainMaterial.SetFloat("_LowThreshold", LowThreshold);
        TerrainMaterial.SetFloat("_MidThreshold", MidThreshold);
        TerrainMaterial.SetFloat("_HighThreshold", HighThreshold);

        TerrainMaterial.SetFloat("_Spec", Specular);
        TerrainMaterial.SetFloat("_Gloss", Gloss);

    }

    [System.Serializable]
    public class PbrTextureSet
    {
        [Range(0, 10)]
        public float DisplacementAmount = 0.2f;
        public float TilingAmount = 1f;

        public Texture2D Albedo;
        public Texture2D Displacement;
        public Texture2D Normalmap;

        public bool ValuesNotNull()
        {
            if(Albedo != null && Displacement != null && Normalmap != null)
            {
                return true;
            }
            return false;
        }
    }
}
