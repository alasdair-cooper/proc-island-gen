using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPlaneChunk
{
    public GameObject WaterPlane;
    public WaterPlaneChunk(float width, Material waterMaterial)
    {
        WaterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        WaterPlane.name = "Water Plane";
        WaterPlane.transform.localScale = new Vector3(0.1f * width, 1, 0.1f * width);
        WaterPlane.transform.position = new Vector3(WaterPlane.transform.position.x + width / 2, 0, WaterPlane.transform.position.z + width / 2);
        WaterPlane.layer = 4;

        WaterPlane.GetComponent<MeshRenderer>().sharedMaterial = waterMaterial;

        UnityStandardAssets.Water.Water water = WaterPlane.AddComponent<UnityStandardAssets.Water.Water>();
        water.waterMode = UnityStandardAssets.Water.Water.WaterMode.Simple;
    }
}
