using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserLevelCell : MonoBehaviour
{
    [SerializeField] private GameObject clearedObject;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI creatorTMP;
    [SerializeField] private TextMeshProUGUI likesTMP;
    [SerializeField] private TextMeshProUGUI bestTMP;

    public void UpdateInfo(bool isCleared, string title, string creator, long likes, short? best)
    {
        titleTMP.text = title;
        creatorTMP.text = creator;
        likesTMP.text = likes.ToString();
        bestTMP.text = best == null ? "없음" : best.Value.ToString() + " 회";
        clearedObject.SetActive(isCleared);
    }
    
    public void UpdateLikes(string likes)
    {
        likesTMP.text = likes;
    }

    public void UpdateBest(short? best)
    {
        bestTMP.text = best == null ? "없음" : best.Value.ToString() + " 회";
    }

    public void UpdateCleared(bool isCleared)
    {
        clearedObject.SetActive(isCleared);
    }
}
