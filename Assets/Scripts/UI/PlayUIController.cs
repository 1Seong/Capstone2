using UnityEngine;

public class PlayUIController : MonoBehaviour
{
    [SerializeField] private GameObject escPanel;

    [SerializeField] private GameObject resetPanel;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPanel.SetActive(!escPanel.activeSelf);
        }
        
        if(Input.GetKeyDown(KeyCode.R))
        {
            resetPanel.SetActive(true);
        }
    }
}
