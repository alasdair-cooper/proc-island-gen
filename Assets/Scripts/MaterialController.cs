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

    public Utils.DebugType DebugMode = Utils.DebugType.none;

    [Range(1, 32)]
    public float Tesselation = 15;
    public float TesselationDistance = 40;

    [Range(0, 1)]
    public float LowThresholdBorder;
    [Range(0, 1)]
    public float MidThresholdBorder;
    [Range(0, 1)]
    public float HighThresholdBorder;


    [Range(0, 1)]
    public float LowThresholdBorderWidth;
    [Range(0, 1)]
    public float MidThresholdBorderWidth;
    [Range(0, 1)]
    public float HighThresholdBorderWidth;

    public float BottomCapThresholdBorder;
    public float BottomCapThresholdBorderWidth;

    [Range(0, 1)]
    public float Specular;
    [Range(0, 1)]
    public float Gloss;

    public PbrTextureSet[] Textures;

    public PbrTextureSet BottomCapTexture;

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
        if (MidThresholdBorder > HighThresholdBorder)
        {
            MidThresholdBorder = HighThresholdBorder;
        }
        if (LowThresholdBorder > MidThresholdBorder)
        {
            LowThresholdBorder = MidThresholdBorder;
        }
        if(LowThresholdBorderWidth + MidThresholdBorderWidth + HighThresholdBorderWidth > 1)
        {
            LowThresholdBorderWidth = Mathf.Min(1, LowThresholdBorderWidth);
            MidThresholdBorderWidth = Mathf.Min(1 - LowThresholdBorderWidth, MidThresholdBorderWidth);
            HighThresholdBorderWidth = Mathf.Min(1 - MidThresholdBorderWidth, HighThresholdBorderWidth);
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

        TerrainMaterial.SetInteger("_DebugMode", ((int)DebugMode));

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

        TerrainMaterial.SetTexture($"_MainTexBottom", BottomCapTexture.Albedo);
        TerrainMaterial.SetTexture($"_DispTexBottom", BottomCapTexture.Displacement);
        TerrainMaterial.SetTexture($"_NormalMapBottom", BottomCapTexture.Normalmap);
        TerrainMaterial.SetFloat($"_DispBottom", BottomCapTexture.DisplacementAmount);
        TerrainMaterial.SetFloat($"_TilingBottom", BottomCapTexture.TilingAmount);

        TerrainMaterial.SetFloat("_LowThreshold", LowThresholdBorder);
        TerrainMaterial.SetFloat("_MidThreshold", MidThresholdBorder);
        TerrainMaterial.SetFloat("_HighThreshold", HighThresholdBorder);

        TerrainMaterial.SetFloat("_BottomCapThreshold", BottomCapThresholdBorder);

        TerrainMaterial.SetFloat("_LowThresholdWidth", LowThresholdBorderWidth);
        TerrainMaterial.SetFloat("_MidThresholdWidth", MidThresholdBorderWidth);
        TerrainMaterial.SetFloat("_HighThresholdWidth", HighThresholdBorderWidth);

        TerrainMaterial.SetFloat("_BottomCapThresholdWidth", BottomCapThresholdBorderWidth);

        TerrainMaterial.SetFloat("_Spec", Specular);
        TerrainMaterial.SetFloat("_Gloss", Gloss);

    }

    [System.Serializable]
    public class PbrTextureSet
    {
        [Range(0, 100)]
        public float DisplacementAmount = 0.2f;
        [Range(0.01f, 1000)]
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
