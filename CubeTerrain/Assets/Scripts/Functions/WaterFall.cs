using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterFall : MonoBehaviour
{
    public Renderer[] renderers;
    public Transform bottomFog;
    public float floorStartY = -4.4f;
    public float floorHeightY = -4.5f;
    public void SetFogPosition(int diff)
    {
        if (bottomFog != null)
        {
            Vector3 pos = bottomFog.localPosition;
            pos.y = floorStartY + floorHeightY * Mathf.Max(0,diff-1);//floorY[Mathf.Max(0, Mathf.Min(floorY.Length-1, diff-1))];
            bottomFog.localPosition = pos;
        }   
    }
}
