using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderReleaseSFX : MonoBehaviour, IPointerUpHandler
{
    public void OnPointerUp(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX(AudioManager.SFXType.Click);
    }
}
