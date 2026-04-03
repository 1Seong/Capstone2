using UnityEngine;

public class ReturnToEditor : MonoBehaviour
{
    public void OnClick()
    {
        GameManager.Instance.ReturnToEditor();
    }
}
