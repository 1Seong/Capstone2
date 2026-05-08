
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChange : MonoBehaviour
{
    public static SceneChange Instance;
    [SerializeField] private CanvasGroup background;
    [SerializeField] private Image initialBackground;
    [SerializeField] private GameObject lightLoadingBackground;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            Instance = this;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(1.5));
        await initialBackground.DOFade(0f, 0.5f).AsyncWaitForCompletion().AsUniTask();
    }

    public async UniTask LoadScene(string sceneName, bool autoEndFade = true)
    {
        // 암전
        background.gameObject.SetActive(true);
        await background.DOFade(1f, 0.5f).AsyncWaitForCompletion().AsUniTask();

        // 씬 비동기 로드
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            await UniTask.Yield();

        op.allowSceneActivation = true;
        await UniTask.WaitUntil(() => op.isDone);

        for (int i = 0; i != 5; ++i)
            await UniTask.WaitForEndOfFrame(this);

        // 복귀
        if (autoEndFade)
        {
            await background.DOFade(0f, 0.5f).AsyncWaitForCompletion().AsUniTask();
            background.gameObject.SetActive(false);
        }
    }

    public async UniTask ManualEndFade()
    {
        background.gameObject.SetActive(true);
        await background.DOFade(0f, 0.5f).AsyncWaitForCompletion().AsUniTask();
        background.gameObject.SetActive(false);
    }

    public async UniTask LoadSceneAddition(string sceneName, bool autoEndFade = true)
    {
        // 암전
        background.gameObject.SetActive(true);
        await background.DOFade(1f, 0.5f).AsyncWaitForCompletion().AsUniTask();

        // 씬 비동기 로드
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            await UniTask.Yield();

        op.allowSceneActivation = true;
        await UniTask.WaitUntil(() => op.isDone);

        for (int i = 0; i != 5; ++i)
            await UniTask.WaitForEndOfFrame(this);

        // 복귀
        if (autoEndFade)
        {
            await background.DOFade(0f, 0.5f).AsyncWaitForCompletion().AsUniTask();
            background.gameObject.SetActive(false);
        }
    }

    public async UniTask UnloadScene(string sceneName, bool autoEndFade = true)
    {
        // 암전
        background.gameObject.SetActive(true);
        await background.DOFade(1f, 0.5f).AsyncWaitForCompletion().AsUniTask();
        
        GameManager.Instance.GameClearedEventInvoke();
        
        // 씬 비동기 로드
        var op = SceneManager.UnloadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            await UniTask.Yield();

        op.allowSceneActivation = true;
        await UniTask.WaitUntil(() => op.isDone);

        for (int i = 0; i != 5; ++i)
            await UniTask.WaitForEndOfFrame(this);

        // 복귀
        if (autoEndFade)
        {
            await background.DOFade(0f, 0.5f).AsyncWaitForCompletion().AsUniTask();
            background.gameObject.SetActive(false);
        }
    }

    public void LightLoading(bool b)
    {
        lightLoadingBackground.SetActive(b);
    }
}
