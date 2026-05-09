
using UnityEngine;

public class GuideUI : MonoBehaviour
{
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private bool isEditor = true;
    
    private void Awake()
    {
        if (isEditor && PlayerPrefs.GetInt("EditorGuide", 0) == 0)
        {
            guidePanel.SetActive(true);
            PlayerPrefs.SetInt("EditorGuide", 1);
            PlayerPrefs.Save();
        }
        else if (PlayerPrefs.GetInt("GameGuide", 0) == 0)
        {
            guidePanel.SetActive(true);
            PlayerPrefs.SetInt("GameGuide", 1);
            PlayerPrefs.Save();
        }
    }
}
