using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TooltipDirection
{
    Auto,
    Up,
    Down
}

public class StageTooltipUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private Canvas canvas;

    [Header("UI")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private Image image;

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 24f);
    [SerializeField] private float screenPadding = 16f;

    private RectTransform canvasRect;
    public static StageTooltipUI Instance { get; private set; }
    
    private bool isVisible;
    private TooltipDirection currentDirection;
    
    private void Update()
    {
        if (!isVisible)
            return;

        SetPosition(Input.mousePosition, currentDirection);
    }

    private void Awake()
    {
        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        canvasRect = canvas.transform as RectTransform;

        Hide();
    }

    public void Show(LevelData stageInfo, Vector2 screenPosition, TooltipDirection direction = TooltipDirection.Auto)
    {
        isVisible = true;
        currentDirection = direction;
        
        stageNameText.text = stageInfo.stageName;
        image.sprite = stageInfo.thumbnail;

        tooltipPanel.gameObject.SetActive(true);

        // LayoutGroup / ContentSizeFitter 사용 시 크기 갱신을 위해 필요
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

        SetPosition(screenPosition, direction);
    }

    public void Hide()
    {
        isVisible = false;
        
        tooltipPanel.gameObject.SetActive(false);
    }

    private void SetPosition(Vector2 screenPosition, TooltipDirection direction)
    {
        Vector2 panelSize = tooltipPanel.rect.size;

        TooltipDirection finalDirection = direction;

        if (finalDirection == TooltipDirection.Auto)
        {
            float topY = screenPosition.y + offset.y + panelSize.y;

            finalDirection = topY > Screen.height - screenPadding
                ? TooltipDirection.Down
                : TooltipDirection.Up;
        }

        Vector2 targetScreenPosition = screenPosition;

        if (finalDirection == TooltipDirection.Up)
        {
            targetScreenPosition += new Vector2(offset.x, offset.y);
            tooltipPanel.pivot = new Vector2(0.5f, 0f);
        }
        else
        {
            targetScreenPosition += new Vector2(offset.x, -offset.y);
            tooltipPanel.pivot = new Vector2(0.5f, 1f);
        }

        // 좌우 화면 밖으로 나가는 것까지 보정
        targetScreenPosition.x = Mathf.Clamp(
            targetScreenPosition.x,
            screenPadding + panelSize.x * tooltipPanel.pivot.x,
            Screen.width - screenPadding - panelSize.x * (1f - tooltipPanel.pivot.x)
        );

        // 위아래도 최종 보정
        targetScreenPosition.y = Mathf.Clamp(
            targetScreenPosition.y,
            screenPadding + panelSize.y * tooltipPanel.pivot.y,
            Screen.height - screenPadding - panelSize.y * (1f - tooltipPanel.pivot.y)
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetScreenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );

        tooltipPanel.anchoredPosition = localPoint;
    }
}
