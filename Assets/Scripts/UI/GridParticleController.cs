using UnityEngine;
using UnityEngine.UI;

public class GridParticleController : MonoBehaviour
{
    public bool useToggle;
    [SerializeField] private Toggle gridToggle;
    [SerializeField] private Toggle particleToggle;
    [SerializeField] private GameObject gridObject;
    [SerializeField] private GameObject particleObject;
    
    private void Start()
    {
        if (useToggle)
        {
            gridToggle.isOn = GameManager.Instance.ShowGrid;
            particleToggle.isOn = GameManager.Instance.ShowParticle;
        }
        else
        {
            gridObject.SetActive(GameManager.Instance.ShowGrid);
            particleObject.SetActive(GameManager.Instance.ShowParticle);
        }

        GameManager.Instance.GridOptionEvent += SetGrid;
        GameManager.Instance.ParticleOptionEvent += SetParticle;
    }

    private void OnDestroy()
    {
        GameManager.Instance.GridOptionEvent -= SetGrid;
        GameManager.Instance.ParticleOptionEvent -= SetParticle;
    }

    private void SetGrid()
    {
        if (useToggle)
        {
            gridToggle.isOn = GameManager.Instance.ShowGrid;
        }
        else
        {
            gridObject.SetActive(GameManager.Instance.ShowGrid);
        }
    }

    private void SetParticle()
    {
        if (useToggle)
        {
            particleToggle.isOn = GameManager.Instance.ShowParticle;
        }
        else
        {
            particleObject.SetActive(GameManager.Instance.ShowParticle);
        }
    }
}
