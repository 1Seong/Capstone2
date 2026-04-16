using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonGroup : MonoBehaviour
{
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private int defaultIndex = 0;

    public event Action<int> OnSelectionChanged;

    private readonly List<Button> _buttons = new();
    private int _selectedIndex = -1;

    private void Awake()
    {
        // 자식 오브젝트의 Button 컴포넌트를 자동 등록
        GetComponentsInChildren(true, _buttons);

        for (int i = 0; i < _buttons.Count; i++)
        {
            int captured = i;
            _buttons[i].onClick.AddListener(() => Select(captured));
        }

        if (_buttons.Count > 0)
            Select(defaultIndex);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= _buttons.Count) return;
        if (_selectedIndex == index) return;

        if (_selectedIndex >= 0) // 기존 버튼 해제
        {
            SetButtonColor(_buttons[_selectedIndex], normalColor);
            _buttons[_selectedIndex].interactable = true;
        }

        _selectedIndex = index;
        SetButtonColor(_buttons[_selectedIndex], selectedColor);
        _buttons[_selectedIndex].interactable = false;

        OnSelectionChanged?.Invoke(_selectedIndex);
    }

    public int SelectedIndex => _selectedIndex;
    
    public Button GetSelectedButton() =>  _buttons[_selectedIndex];

    public void DeleteSelectedButton()
    {
        _buttons[_selectedIndex].gameObject.SetActive(false);
    }

    private void SetButtonColor(Button button, Color color)
    {
        var colors = button.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        button.colors = colors;
    }
}
