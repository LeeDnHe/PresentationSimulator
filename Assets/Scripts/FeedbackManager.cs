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
    
    [Header("발표 제어 설정")]
    public Button endButton; // 종료 버튼
    public TextMeshProUGUI statusText; // 상태 텍스트
    
    [Header("피드백 설정")]
    public bool showRealTimeFeedback = true; // 실시간 피드백 표시 여부
    public string defaultFeedbackText = "음성 분석 대기 중..."; // 기본 피드백 텍스트
    
    [Header("이벤트")]
    public System.Action<AnalysisResult> OnFeedbackDisplayed; // 피드백 표시 이벤트
    
    private VoiceAnalyzer voiceAnalyzer;
    private TransitionManager transitionManager;
    private PresentationResultVisualizer resultVisualizer;
    
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
        
        // 결과 시각화 시스템 찾기
        resultVisualizer = FindObjectOfType<PresentationResultVisualizer>();
        
        // 종료 버튼 이벤트 연결
        if (endButton != null)
        {
            endButton.onClick.AddListener(EndPresentationPublic);
        }
        
        // 초기 상태 설정 - 패널은 항상 활성화, 기본 텍스트 설정
        SetFeedbackPanelActive(true);
        SetDefaultFeedbackText();
        
        // UI 시스템 초기화 후 UI 업데이트 (지연 호출)
        StartCoroutine(UpdateUIDelayed());
    }
    
    void Update()
    {
        // Update() 메서드에서 statusText 업데이트 제거 (피드백 텍스트 간섭 방지)
        // 상태 업데이트는 이벤트 기반으로만 처리하도록 변경
        
        // 디버그: statusText와 feedbackText 간섭 체크
        if (statusText != null && feedbackText != null && statusText == feedbackText)
        {
            Debug.LogError("🚨 statusText와 feedbackText가 같은 UI 요소를 참조하고 있어서 텍스트가 충돌합니다!");
        }
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 피드백 패널 확인
        if (feedbackPanel == null)
        {
            Debug.LogError("❌ FeedbackPanel이 할당되지 않았습니다!");
        }
        
        // 피드백 텍스트 확인
        if (feedbackText == null)
        {
            Debug.LogError("❌ FeedbackText가 할당되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 피드백 표시 (외부 호출용)
    /// </summary>
    /// <param name="result">분석 결과</param>
    public void ShowFeedback(AnalysisResult result)
    {
        if (!showRealTimeFeedback) 
        {
            Debug.Log("🚫 실시간 피드백이 비활성화되어 피드백 업데이트를 무시합니다.");
            return;
        }
        
        // 직접 피드백 텍스트 업데이트
        UpdateFeedbackText(result.feedback);
        
        // 이벤트 발생
        OnFeedbackDisplayed?.Invoke(result);
        
        Debug.Log($"🎤 피드백 업데이트: {result.feedback}");
    }
    
    /// <summary>
    /// 기본 피드백 텍스트 설정
    /// </summary>
    private void SetDefaultFeedbackText()
    {
        UpdateFeedbackText(defaultFeedbackText);
    }
    
    /// <summary>
    /// 피드백 텍스트 업데이트
    /// </summary>
    /// <param name="message">피드백 메시지</param>
    private void UpdateFeedbackText(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }
    
    /// <summary>
    /// 피드백 패널 활성화/비활성화
    /// </summary>
    /// <param name="active">활성화 여부</param>
    public void SetFeedbackPanelActive(bool active)
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
    public void ShowManualFeedback(string message)
    {
        UpdateFeedbackText(message);
        Debug.Log($"🎤 수동 피드백 설정: {message}");
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
    /// 피드백 초기화
    /// </summary>
    public void ClearFeedback()
    {
        SetDefaultFeedbackText();
        Debug.Log("🧹 피드백 초기화됨");
    }
    
    /// <summary>
    /// 최종 피드백 표시
    /// </summary>
    private void ShowFinalFeedback()
    {
        string finalFeedbackText = GenerateFinalFeedbackText();
        UpdateFeedbackText(finalFeedbackText);
        
        // 3초 후 결과 그래프 시스템 시작
        StartCoroutine(ShowResultVisualizationAfterDelay());
        
        Debug.Log("🎉 최종 피드백 표시됨");
    }
    
    /// <summary>
    /// 최종 피드백 텍스트 생성
    /// </summary>
    /// <returns>최종 피드백 텍스트</returns>
    private string GenerateFinalFeedbackText()
    {
        string finalText = "🎉 발표 완료!\n\n";
        finalText += "수고하셨습니다.\n\n";
        finalText += "잠시 후 상세한 분석 결과가 표시됩니다...";
        
        return finalText;
    }
    
    /// <summary>
    /// 지연 후 결과 시각화 표시
    /// </summary>
    private IEnumerator ShowResultVisualizationAfterDelay()
    {
        // 3초 대기
        yield return new WaitForSeconds(3f);
        
        // 결과 시각화 시스템 호출
        if (resultVisualizer != null)
        {
            resultVisualizer.ShowResults();
            Debug.Log("📊 결과 시각화 시스템 호출됨");
        }
        else
        {
            Debug.LogWarning("⚠️ PresentationResultVisualizer를 찾을 수 없습니다!");
        }
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
            ClearFeedback();
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
        
        // 발표 종료 시 최종 피드백 표시 (이벤트 핸들러에서 처리됨)
        // ClearFeedback()는 호출하지 않음 - 최종 피드백 유지
        
        // UI 업데이트
        UpdateUI();
    }
    
    /// <summary>
    /// 발표 시작 이벤트 핸들러
    /// </summary>
    private void OnPresentationStarted()
    {
        Debug.Log("📢 발표 시작 이벤트 수신");
        
        // 실시간 피드백 활성화
        showRealTimeFeedback = true;
        
        // VoiceAnalyzer 이벤트 재연결 (발표 종료 시 해제되었을 수 있음)
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted -= ShowFeedback; // 중복 방지
            voiceAnalyzer.OnAnalysisCompleted += ShowFeedback;
        }
        
        // 기본 피드백 텍스트로 초기화
        SetDefaultFeedbackText();
        
        UpdateUI();
    }
    
    /// <summary>
    /// 발표 종료 이벤트 핸들러
    /// </summary>
    private void OnPresentationEnded()
    {
        Debug.Log("📢 발표 종료 이벤트 수신");
        
        // 실시간 피드백 완전 중단
        showRealTimeFeedback = false;
        
        // VoiceAnalyzer 통신 중단
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.StopAnalysis();
            // 이벤트 연결 해제하여 추가 피드백 방지
            voiceAnalyzer.OnAnalysisCompleted -= ShowFeedback;
        }
        
        // 최종 피드백 텍스트 표시
        ShowFinalFeedback();
        
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
        // 실시간 피드백 중단
        showRealTimeFeedback = false;
        
        // 이벤트 연결 해제
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted -= ShowFeedback;
            voiceAnalyzer.StopAnalysis(); // 분석 완전 중단
        }
        
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart -= OnPresentationStarted;
            transitionManager.OnPresentationEnd -= OnPresentationEnded;
            transitionManager.OnSlideChanged -= OnSlideChanged;
        }
        
        // 실행 중인 코루틴 중지
        StopAllCoroutines();
        
        // 피드백 정리
        ClearFeedback();
    }
} 