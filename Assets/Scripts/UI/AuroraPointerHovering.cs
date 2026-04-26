
using UnityEngine;
using UnityEngine.EventSystems;

public class AuroraPointerHovering : MonoBehaviour
{
    public Material mat;
    public float originalOpacity;
    public float targetOpacity;

    public void OnMouseEnter()
    {
        mat.SetFloat("_Opacity", targetOpacity);
    }

    public void OnMouseExit()
    {
        mat.SetFloat("_Opacity", originalOpacity);
    }
}
