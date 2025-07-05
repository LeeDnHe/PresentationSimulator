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
    private Queue<AnalysisResult> feedbackQueue = new Queue<AnalysisResult>();
    
    void Start()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 음성 분석기 찾기
        voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted += ShowFeedback;
        }
        
        // 초기 상태 설정
        SetFeedbackPanelActive(false);
    }
    
    void Update()
    {
        // 큐에 대기 중인 피드백 처리
        if (feedbackQueue.Count > 0 && currentFeedbackCoroutine == null)
        {
            AnalysisResult nextFeedback = feedbackQueue.Dequeue();
            DisplayFeedback(nextFeedback);
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
            backgroundColor.a = 0.7f; // 투명도 설정
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
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            
            yield return null;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// 페이드아웃 애니메이션
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            
            yield return null;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
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
    /// <param name="message">피드백 메시지</param>
    /// <param name="score">점수</param>
    /// <param name="color">색상</param>
    public void ShowManualFeedback(string message, float score, Color color)
    {
        AnalysisResult result = new AnalysisResult
        {
            feedback = message,
            overallScore = score,
            feedbackColor = color,
            analysisData = new VoiceAnalysisData
            {
                timestamp = System.DateTime.Now
            }
        };
        
        ShowFeedback(result);
    }
    
    /// <summary>
    /// 피드백 큐 초기화
    /// </summary>
    public void ClearFeedbackQueue()
    {
        feedbackQueue.Clear();
        
        // 현재 표시 중인 피드백 중지
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
            currentFeedbackCoroutine = null;
            SetFeedbackPanelActive(false);
        }
    }
    
    /// <summary>
    /// 실시간 피드백 활성화/비활성화
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
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted -= ShowFeedback;
        }
    }
} 