using UnityEngine;

public class ButtonSFX : MonoBehaviour
{
    public void OnClick()
    {
        AudioManager.Instance.PlaySFX(AudioManager.SFXType.Click);
    }
}
