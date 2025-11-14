using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[System.Serializable]
public struct VideoSpeaker
{
    public RawImage imageScene;                // 영상 재생 UI
    public TextMeshProUGUI textDialog;      // 텍스트 스크립트 출력 UI
    public GameObject objectArrow;          // 스크립트 표시 완료 후 활성화할 커서 오브젝트
    public Image fadePanel;                 // 페이드 인/아웃용 패널
}

[System.Serializable]
public struct DialogVideoData
{
    public VideoClip video;
    [TextArea(3, 5)] public string dialog;  // 텍스트 스크립트
}

public class UI_Prologue : MonoBehaviour
{
    [Header("배경 이미지 및 스크립트 설정")]
    [SerializeField] private VideoSpeaker speaker;
    [SerializeField] private DialogVideoData[] dialogs;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "GameScene";
    [SerializeField] private bool isAutoStart = true;
    [SerializeField] private float typeSpeed = 0.05f;
    [SerializeField] private float fadeDuration = 1.0f;
    private bool _isFirst = true;
    private int _currentDialogIndex = -1;
    private bool _isTypingEffect = false;
    private bool _isFadeOut = false;
    private bool _isFadeIn = false;
    private bool _isEnd = false;
    private Coroutine _nextRoutine = null;
    private Coroutine _fadeRoutine = null;

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        if (_isEnd) return;

        if (UpdateDialog())
        {
            // To-do: The End나 엔딩 스크롤이 필요할 경우 해당 부분 수정
            if (!nextSceneName.Equals("GameScene"))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;    // 에디터에서는 플레이 중단
#else
                Application.Quit();                                 // 빌드에서는 프로그램 종료
#endif
                return;
            }

            StartCoroutine(FadePanelAndGoToNext(speaker.fadePanel, 0f, 1f, fadeDuration));
        }
    }

    public bool UpdateDialog()
    {
        if (_isFirst)
        {
            Init();

            if (isAutoStart)
            {
                SetNextDialog();
            }
            _isFirst = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (_isFadeIn)
            {
                _isFadeIn = false;
                StopCoroutine(_nextRoutine);
                StopCoroutine(_fadeRoutine);

                Color color = speaker.imageScene.color;
                speaker.imageScene.color = new Color(color.r, color.g, color.b, 1.0f);

                StartCoroutine("CoTypeText");

                return false;
            }

            if (_isFadeOut)
            {
                _isFadeOut = false;
                StopCoroutine(_nextRoutine);
                StopCoroutine(_fadeRoutine);

                if (!speaker.imageScene.gameObject.activeSelf)
                {
                    speaker.imageScene.gameObject.SetActive(true);
                }

                Color color = speaker.imageScene.color;
                speaker.imageScene.color = new Color(color.r, color.g, color.b, 1.0f);

                ++_currentDialogIndex;
                videoPlayer.Stop();
                videoPlayer.clip = dialogs[_currentDialogIndex].video;
                videoPlayer.Play();
                StartCoroutine("CoTypeText");

                return false;
            }

            if (_isTypingEffect)
            {
                _isTypingEffect = false;
                StopCoroutine("CoTypeText");
                speaker.textDialog.text = dialogs[_currentDialogIndex].dialog;
                speaker.objectArrow.SetActive(true);

                return false;
            }

            if (dialogs.Length > _currentDialogIndex + 1)
            {
                SetNextDialog();
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private void Init()
    {
        //speaker.imageScene.gameObject.SetActive(false);
        speaker.textDialog.text = "";
        speaker.objectArrow.SetActive(false);
    }

    private void SetNextDialog()
    {
        // 다음 씬 영상이 없을 경우
        if (!_isFirst && (_currentDialogIndex + 1 < dialogs.Length
            && !dialogs[_currentDialogIndex + 1].video))
        {
            // 페이드 아웃 없이 바로 진행
            speaker.textDialog.text = "";
            speaker.objectArrow.SetActive(false);
            ++_currentDialogIndex;
            StartCoroutine("CoTypeText");
        }
        else
        {
            _nextRoutine = StartCoroutine(SetNextDialogWithFade(_isFirst));
        }
    }

    private IEnumerator CoTypeText()
    {
        int index = 0;
        _isTypingEffect = true;

        while (index <= dialogs[_currentDialogIndex].dialog.Length)
        {
            speaker.textDialog.text = dialogs[_currentDialogIndex].dialog.Substring(0, index);
            ++index;
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTypingEffect = false;
        speaker.objectArrow.SetActive(true);
    }

    private IEnumerator FadeImage(RawImage image, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = image.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        image.color = new Color(color.r, color.g, color.b, toAlpha);
    }

    private IEnumerator FadePanelAndGoToNext(Image image, float fromAlpha, float toAlpha, float duration)
    {
        _isEnd = true;
        image.gameObject.SetActive(true);

        float elapsed = 0f;
        Color color = image.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        image.color = new Color(color.r, color.g, color.b, toAlpha);

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator SetNextDialogWithFade(bool isFirst = false)
    {
        speaker.textDialog.text = "";
        speaker.objectArrow.SetActive(false);
        _isFadeOut = true;

        // 페이드 아웃
        _fadeRoutine = StartCoroutine(FadeImage(speaker.imageScene, 1f, 0f, fadeDuration));
        yield return _fadeRoutine;
        _isFadeOut = false;

        if (isFirst)
        {
            speaker.imageScene.gameObject.SetActive(true);
        }

        ++_currentDialogIndex;
        videoPlayer.Stop();
        videoPlayer.clip = dialogs[_currentDialogIndex].video;
        _isFadeIn = true;

        yield return new WaitForSeconds(0.2f);

        // 페이드 인
        _fadeRoutine = StartCoroutine(FadeImage(speaker.imageScene, 0f, 1f, fadeDuration));
        yield return _fadeRoutine;
        _isFadeIn = false;

        // 텍스트 타이핑 시작
        videoPlayer.Play();
        StartCoroutine("CoTypeText");
    }
}
