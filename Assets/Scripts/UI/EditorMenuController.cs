
using UnityEngine;

public class EditorMenuController : MonoBehaviour
{
    [SerializeField] private GameObject editorReturnButton;
    [SerializeField] private GameObject testReturnButton;
    
    private void OnEnable()
    {
        if (MapEditor.Instance.IsTesting)
        {
            editorReturnButton.SetActive(false);
            testReturnButton.SetActive(true);
        }
        else
        {
            editorReturnButton.SetActive(true);
            testReturnButton.SetActive(false);
        }
    }
}
