using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedbackManager : MonoBehaviour
{
    [Header("피드백 UI 설정")]
    public GameObject feedbackPanel; // 피드백 패널
    public TextMeshProUGUI feedbackText; // 피드백 텍스트
    public Image feedbackBackground; // 피드백 배경
    public Slider scoreSlider; // 점수 슬라이더
    public TextMeshProUGUI scoreText; // 점수 텍스트
    
    [Header("발표 제어 설정")]
    public Button endButton; // 종료 버튼
    public TextMeshProUGUI statusText; // 상태 텍스트
    
    [Header("애니메이션 설정")]
    public float fadeInDuration = 0.5f; // 페이드인 시간
    public float displayDuration = 3f; // 표시 지속 시간
    public float fadeOutDuration = 0.5f; // 페이드아웃 시간
    
    [Header("피드백 설정")]
    public bool showRealTimeFeedback = true; // 실시간 피드백 표시 여부
    public Vector3 feedbackPosition = new Vector3(0, 2, 3); // 피드백 표시 위치
    
    [Header("색상 설정")]
    public Color excellentColor = Color.green; // 우수 (80점 이상)
    public Color goodColor = Color.yellow; // 양호 (60-80점)
    public Color poorColor = Color.red; // 부족 (60점 미만)
    
    [Header("이벤트")]
    public System.Action<AnalysisResult> OnFeedbackDisplayed; // 피드백 표시 이벤트
    
    private Coroutine currentFeedbackCoroutine;
    private CanvasGroup canvasGroup;
    private VoiceAnalyzer voiceAnalyzer;
    private TransitionManager transitionManager;
    private Queue<AnalysisResult> feedbackQueue = new Queue<AnalysisResult>();
    
    void Start()
    {
        Debug.Log("🚀 FeedbackManager Start() 호출됨");
        
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 음성 분석기 찾기
        voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted += ShowFeedback;
        }
        
        // 전환 관리자 찾기
        transitionManager = FindObjectOfType<TransitionManager>();
        if (transitionManager != null)
        {
            // 발표 상태 이벤트 연결
            transitionManager.OnPresentationStart += OnPresentationStarted;
            transitionManager.OnPresentationEnd += OnPresentationEnded;
            transitionManager.OnSlideChanged += OnSlideChanged;
        }
        
        // 종료 버튼 이벤트 연결
        if (endButton != null)
        {
            endButton.onClick.AddListener(EndPresentationPublic);
        }
        
        // 초기 상태 설정
        SetFeedbackPanelActive(false);
        
        // UI 시스템 초기화 후 UI 업데이트 (지연 호출)
        StartCoroutine(UpdateUIDelayed());
    }
    
    void Update()
    {
        // 큐에 대기 중인 피드백 처리
        if (feedbackQueue.Count > 0 && currentFeedbackCoroutine == null)
        {
            AnalysisResult nextFeedback = feedbackQueue.Dequeue();
            DisplayFeedback(nextFeedback);
        }
        
        // 슬라이드 정보 실시간 업데이트 (발표 진행 중일 때만)
        if (transitionManager != null && transitionManager.isPresenting && statusText != null)
        {
            var slideInfo = transitionManager.GetSlideInfo();
            statusText.text = $"발표 진행 중 ({slideInfo.current}/{slideInfo.total})";
        }
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // Canvas Group 추가 (없으면)
        if (feedbackPanel != null)
        {
            canvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = feedbackPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // 피드백 패널 위치 설정
        if (feedbackPanel != null)
        {
            feedbackPanel.transform.position = feedbackPosition;
        }
    }
    
    /// <summary>
    /// 피드백 표시 (외부 호출용)
    /// </summary>
    /// <param name="result">분석 결과</param>
    public void ShowFeedback(AnalysisResult result)
    {
        if (!showRealTimeFeedback) return;
        
        // 피드백 큐에 추가
        feedbackQueue.Enqueue(result);
    }
    
    /// <summary>
    /// 피드백 직접 표시
    /// </summary>
    /// <param name="result">분석 결과</param>
    private void DisplayFeedback(AnalysisResult result)
    {
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
        }
        
        currentFeedbackCoroutine = StartCoroutine(DisplayFeedbackCoroutine(result));
    }
    
    /// <summary>
    /// 피드백 표시 코루틴
    /// </summary>
    /// <param name="result">분석 결과</param>
    private IEnumerator DisplayFeedbackCoroutine(AnalysisResult result)
    {
        // 피드백 내용 설정
        UpdateFeedbackContent(result);
        
        // 피드백 패널 활성화
        SetFeedbackPanelActive(true);
        
        // 페이드인 애니메이션
        yield return StartCoroutine(FadeIn());
        
        // 표시 지속 시간
        yield return new WaitForSeconds(displayDuration);
        
        // 페이드아웃 애니메이션
        yield return StartCoroutine(FadeOut());
        
        // 피드백 패널 비활성화
        SetFeedbackPanelActive(false);
        
        // 이벤트 발생
        OnFeedbackDisplayed?.Invoke(result);
        
        currentFeedbackCoroutine = null;
    }
    
    /// <summary>
    /// 피드백 내용 업데이트
    /// </summary>
    /// <param name="result">분석 결과</param>
    private void UpdateFeedbackContent(AnalysisResult result)
    {
        // 피드백 텍스트 설정
        if (feedbackText != null)
        {
            feedbackText.text = result.feedback;
            feedbackText.color = result.feedbackColor;
        }
        
        // 배경 색상 설정
        if (feedbackBackground != null)
        {
            Color backgroundColor = GetBackgroundColor(result.overallScore);
            feedbackBackground.color = backgroundColor;
        }
        
        // 점수 슬라이더 설정
        if (scoreSlider != null)
        {
            scoreSlider.value = result.overallScore / 100f;
        }
        
        // 점수 텍스트 설정
        if (scoreText != null)
        {
            scoreText.text = $"{result.overallScore:F1}점";
        }
    }
    
    /// <summary>
    /// 점수에 따른 배경 색상 반환
    /// </summary>
    /// <param name="score">점수</param>
    /// <returns>배경 색상</returns>
    private Color GetBackgroundColor(float score)
    {
        if (score >= 80f)
        {
            return excellentColor;
        }
        else if (score >= 60f)
        {
            return goodColor;
        }
        else
        {
            return poorColor;
        }
    }
    
    /// <summary>
    /// 페이드인 애니메이션
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeInDuration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// 페이드아웃 애니메이션
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// 피드백 패널 활성화/비활성화
    /// </summary>
    /// <param name="active">활성화 여부</param>
    private void SetFeedbackPanelActive(bool active)
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(active);
        }
    }
    
    /// <summary>
    /// 수동 피드백 표시
    /// </summary>
    /// <param name="message">메시지</param>
    /// <param name="score">점수</param>
    /// <param name="color">색상</param>
    public void ShowManualFeedback(string message, float score, Color color)
    {
        AnalysisResult result = new AnalysisResult
        {
            feedback = message,
            overallScore = score,
            feedbackColor = color
        };
        
        ShowFeedback(result);
    }
    
    /// <summary>
    /// 발표 시작 (공개 메서드 - StartTrigger에서 호출)
    /// </summary>
    public void StartPresentationPublic()
    {
        Debug.Log("🎤 발표 시작 요청됨 (StartTrigger에서 호출)");
        StartPresentation();
    }
    
    /// <summary>
    /// 발표 종료 (공개 메서드)
    /// </summary>
    public void EndPresentationPublic()
    {
        Debug.Log("🛑 발표 종료 요청됨");
        EndPresentation();
    }
    
    /// <summary>
    /// 피드백 큐 정리
    /// </summary>
    public void ClearFeedbackQueue()
    {
        feedbackQueue.Clear();
        
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
            currentFeedbackCoroutine = null;
        }
        
        SetFeedbackPanelActive(false);
    }
    
    /// <summary>
    /// 실시간 피드백 표시 설정
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetRealTimeFeedbackEnabled(bool enabled)
    {
        showRealTimeFeedback = enabled;
        
        if (!enabled)
        {
            ClearFeedbackQueue();
        }
    }
    
    /// <summary>
    /// 발표 시작 (내부 메서드)
    /// </summary>
    private void StartPresentation()
    {
        if (transitionManager == null)
        {
            Debug.LogError("❌ TransitionManager가 없습니다!");
            return;
        }
        
        Debug.Log("✅ 발표 시작!");
        
        // 전환 관리자를 통해 발표 시작
        transitionManager.StartPresentation();
        
        // 음성 분석기 시작
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.StartAnalysis();
        }
        
        // UI 업데이트
        UpdateUI();
    }
    
    /// <summary>
    /// 발표 종료 (내부 메서드)
    /// </summary>
    private void EndPresentation()
    {
        if (transitionManager == null)
        {
            Debug.LogError("❌ TransitionManager가 없습니다!");
            return;
        }
        
        Debug.Log("✅ 발표 종료!");
        
        // 전환 관리자를 통해 발표 종료
        transitionManager.EndPresentation();
        
        // 음성 분석기 정지
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.StopAnalysis();
        }
        
        // 피드백 큐 정리
        ClearFeedbackQueue();
        
        // UI 업데이트
        UpdateUI();
    }
    
    /// <summary>
    /// 발표 시작 이벤트 핸들러
    /// </summary>
    private void OnPresentationStarted()
    {
        Debug.Log("📢 발표 시작 이벤트 수신");
        UpdateUI();
    }
    
    /// <summary>
    /// 발표 종료 이벤트 핸들러
    /// </summary>
    private void OnPresentationEnded()
    {
        Debug.Log("📢 발표 종료 이벤트 수신");
        UpdateUI();
    }
    
    /// <summary>
    /// 슬라이드 변경 이벤트 핸들러
    /// </summary>
    /// <param name="slideIndex">슬라이드 인덱스</param>
    private void OnSlideChanged(int slideIndex)
    {
        Debug.Log($"📢 슬라이드 변경 이벤트 수신: {slideIndex}");
        UpdateUI();
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (statusText != null)
        {
            if (transitionManager != null)
            {
                if (transitionManager.isPresenting)
                {
                    var slideInfo = transitionManager.GetSlideInfo();
                    statusText.text = $"발표 진행 중 ({slideInfo.current}/{slideInfo.total})";
                }
                else
                {
                    statusText.text = "발표 대기 중";
                }
            }
            else
            {
                statusText.text = "시스템 초기화 중...";
            }
        }
        
        // 종료 버튼 활성화 상태 업데이트
        if (endButton != null && transitionManager != null)
        {
            endButton.interactable = transitionManager.isPresenting;
        }
    }
    
    /// <summary>
    /// UI 지연 업데이트 (UI 시스템 초기화 후)
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator UpdateUIDelayed()
    {
        // UI 시스템 초기화 대기
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log("🎨 UI 지연 업데이트 실행");
        UpdateUI();
    }
    
    /// <summary>
    /// 컴포넌트 정리
    /// </summary>
    void OnDestroy()
    {
        // 이벤트 연결 해제
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted -= ShowFeedback;
        }
        
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart -= OnPresentationStarted;
            transitionManager.OnPresentationEnd -= OnPresentationEnded;
            transitionManager.OnSlideChanged -= OnSlideChanged;
        }
        
        // 코루틴 정리
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
        }
    }
} 