using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[System.Serializable]
public class PresentationResult
{
    public float overallScore; // 전체 점수
    public float confidenceScore; // 자신감 점수
    public float clarityScore; // 명확도 점수
    public float volumeScore; // 볼륨 점수
    public float paceScore; // 속도 점수
    public TimeSpan totalDuration; // 총 발표 시간
    public int totalSlides; // 총 슬라이드 수
    public int analyzedSegments; // 분석된 구간 수
    public List<string> strengths; // 강점 목록
    public List<string> improvements; // 개선점 목록
    public string grade; // 등급 (A+, A, B+ 등)
    public DateTime presentationDate; // 발표 날짜
}

public class PresentationResultManager : MonoBehaviour
{
    [Header("결과 UI 설정")]
    public GameObject resultPanel; // 결과 패널
    public TextMeshProUGUI overallScoreText; // 전체 점수 텍스트
    public TextMeshProUGUI gradeText; // 등급 텍스트
    public TextMeshProUGUI durationText; // 발표 시간 텍스트
    public Slider overallScoreSlider; // 전체 점수 슬라이더
    
    [Header("세부 점수 UI")]
    public Slider confidenceSlider; // 자신감 슬라이더
    public Slider claritySlider; // 명확도 슬라이더
    public Slider volumeSlider; // 볼륨 슬라이더
    public Slider paceSlider; // 속도 슬라이더
    public TextMeshProUGUI confidenceText; // 자신감 텍스트
    public TextMeshProUGUI clarityText; // 명확도 텍스트
    public TextMeshProUGUI volumeText; // 볼륨 텍스트
    public TextMeshProUGUI paceText; // 속도 텍스트
    
    [Header("피드백 UI")]
    public TextMeshProUGUI strengthsText; // 강점 텍스트
    public TextMeshProUGUI improvementsText; // 개선점 텍스트
    public TextMeshProUGUI detailsText; // 세부 정보 텍스트
    
    [Header("버튼 설정")]
    public Button restartButton; // 다시 시작 버튼
    public Button saveButton; // 저장 버튼
    public Button exitButton; // 종료 버튼
    
    [Header("등급 설정")]
    public Color[] gradeColors = new Color[]
    {
        Color.red,    // F (0-30)
        new Color(1f, 0.5f, 0f), // D (30-50) - Orange
        Color.yellow, // C (50-70)
        Color.green,  // B (70-85)
        Color.blue    // A (85-100)
    };
    
    [Header("이벤트")]
    public System.Action<PresentationResult> OnResultDisplayed; // 결과 표시 이벤트
    public System.Action OnRestartRequested; // 재시작 요청 이벤트
    public System.Action OnExitRequested; // 종료 요청 이벤트
    
    private TransitionManager transitionManager;
    private VoiceAnalyzer voiceAnalyzer;
    private DateTime presentationStartTime;
    private PresentationResult currentResult;
    
    void Start()
    {
        // 컴포넌트 찾기
        transitionManager = FindObjectOfType<TransitionManager>();
        voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();
        
        // 이벤트 연결
        ConnectEvents();
        
        // 버튼 이벤트 설정
        SetupButtons();
        
        // 초기 상태 설정
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 이벤트 연결
    /// </summary>
    private void ConnectEvents()
    {
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart += OnPresentationStart;
            transitionManager.OnPresentationEnd += OnPresentationEnd;
        }
    }
    
    /// <summary>
    /// 버튼 설정
    /// </summary>
    private void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartPresentation);
        }
        
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveResult);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitPresentation);
        }
    }
    
    /// <summary>
    /// 발표 시작 처리
    /// </summary>
    private void OnPresentationStart()
    {
        presentationStartTime = DateTime.Now;
        Debug.Log("발표 시작 시간 기록");
    }
    
    /// <summary>
    /// 발표 종료 처리
    /// </summary>
    private void OnPresentationEnd()
    {
        // 발표 결과 생성
        GeneratePresentationResult();
        
        // 결과 표시
        DisplayResult();
    }
    
    /// <summary>
    /// 발표 결과 생성
    /// </summary>
    private void GeneratePresentationResult()
    {
        currentResult = new PresentationResult
        {
            presentationDate = DateTime.Now,
            totalDuration = DateTime.Now - presentationStartTime
        };
        
        // 음성 분석 결과 통합
        if (voiceAnalyzer != null)
        {
            var finalAnalysis = voiceAnalyzer.GetFinalAnalysisResult();
            if (finalAnalysis != null)
            {
                currentResult.overallScore = finalAnalysis.overallScore;
                currentResult.confidenceScore = finalAnalysis.analysisData.confidence * 100f;
                currentResult.clarityScore = finalAnalysis.analysisData.clarity * 100f;
                currentResult.volumeScore = finalAnalysis.analysisData.volume * 100f;
                currentResult.paceScore = CalculatePaceScore(finalAnalysis.analysisData.speechRate);
                currentResult.analyzedSegments = voiceAnalyzer.analysisHistory.Count;
            }
        }
        
        // 슬라이드 정보
        if (transitionManager != null)
        {
            var slideInfo = transitionManager.GetSlideInfo();
            currentResult.totalSlides = slideInfo.total;
        }
        
        // 등급 계산
        currentResult.grade = CalculateGrade(currentResult.overallScore);
        
        // 피드백 생성
        GenerateFeedback();
        
        Debug.Log($"발표 결과 생성 완료: {currentResult.overallScore:F1}점 ({currentResult.grade})");
    }
    
    /// <summary>
    /// 속도 점수 계산
    /// </summary>
    /// <param name="speechRate">말하기 속도</param>
    /// <returns>속도 점수</returns>
    private float CalculatePaceScore(float speechRate)
    {
        // 적정 속도: 120-160 WPM
        float idealMin = 120f;
        float idealMax = 160f;
        
        if (speechRate >= idealMin && speechRate <= idealMax)
        {
            return 100f;
        }
        else if (speechRate < idealMin)
        {
            return Mathf.Lerp(50f, 100f, speechRate / idealMin);
        }
        else
        {
            return Mathf.Lerp(100f, 50f, (speechRate - idealMax) / (200f - idealMax));
        }
    }
    
    /// <summary>
    /// 등급 계산
    /// </summary>
    /// <param name="score">점수</param>
    /// <returns>등급</returns>
    private string CalculateGrade(float score)
    {
        if (score >= 95f) return "A+";
        else if (score >= 90f) return "A";
        else if (score >= 85f) return "A-";
        else if (score >= 80f) return "B+";
        else if (score >= 75f) return "B";
        else if (score >= 70f) return "B-";
        else if (score >= 65f) return "C+";
        else if (score >= 60f) return "C";
        else if (score >= 55f) return "C-";
        else if (score >= 50f) return "D+";
        else if (score >= 45f) return "D";
        else if (score >= 40f) return "D-";
        else return "F";
    }
    
    /// <summary>
    /// 피드백 생성
    /// </summary>
    private void GenerateFeedback()
    {
        currentResult.strengths = new List<string>();
        currentResult.improvements = new List<string>();
        
        // 강점 분석
        if (currentResult.confidenceScore >= 80f)
        {
            currentResult.strengths.Add("자신감 넘치는 발표");
        }
        
        if (currentResult.clarityScore >= 80f)
        {
            currentResult.strengths.Add("명확한 발음과 전달");
        }
        
        if (currentResult.volumeScore >= 70f)
        {
            currentResult.strengths.Add("적절한 음성 크기");
        }
        
        if (currentResult.paceScore >= 80f)
        {
            currentResult.strengths.Add("적절한 발표 속도");
        }
        
        // 개선점 분석
        if (currentResult.confidenceScore < 60f)
        {
            currentResult.improvements.Add("더 자신감 있는 발표 필요");
        }
        
        if (currentResult.clarityScore < 60f)
        {
            currentResult.improvements.Add("발음과 전달 개선 필요");
        }
        
        if (currentResult.volumeScore < 50f)
        {
            currentResult.improvements.Add("목소리 크기 개선 필요");
        }
        
        if (currentResult.paceScore < 60f)
        {
            currentResult.improvements.Add("발표 속도 조절 필요");
        }
        
        // 기본 메시지
        if (currentResult.strengths.Count == 0)
        {
            currentResult.strengths.Add("발표 완료");
        }
        
        if (currentResult.improvements.Count == 0)
        {
            currentResult.improvements.Add("계속 연습하여 실력 향상");
        }
    }
    
    /// <summary>
    /// 결과 표시
    /// </summary>
    private void DisplayResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
        
        // 전체 점수 표시
        if (overallScoreText != null)
        {
            overallScoreText.text = $"{currentResult.overallScore:F1}점";
        }
        
        if (overallScoreSlider != null)
        {
            overallScoreSlider.value = currentResult.overallScore / 100f;
        }
        
        // 등급 표시
        if (gradeText != null)
        {
            gradeText.text = currentResult.grade;
            gradeText.color = GetGradeColor(currentResult.overallScore);
        }
        
        // 발표 시간 표시
        if (durationText != null)
        {
            durationText.text = $"{currentResult.totalDuration.Minutes:D2}:{currentResult.totalDuration.Seconds:D2}";
        }
        
        // 세부 점수 표시
        UpdateDetailScores();
        
        // 피드백 표시
        UpdateFeedback();
        
        // 세부 정보 표시
        UpdateDetails();
        
        // 이벤트 발생
        OnResultDisplayed?.Invoke(currentResult);
    }
    
    /// <summary>
    /// 세부 점수 업데이트
    /// </summary>
    private void UpdateDetailScores()
    {
        // 자신감
        if (confidenceSlider != null)
        {
            confidenceSlider.value = currentResult.confidenceScore / 100f;
        }
        if (confidenceText != null)
        {
            confidenceText.text = $"{currentResult.confidenceScore:F1}점";
        }
        
        // 명확도
        if (claritySlider != null)
        {
            claritySlider.value = currentResult.clarityScore / 100f;
        }
        if (clarityText != null)
        {
            clarityText.text = $"{currentResult.clarityScore:F1}점";
        }
        
        // 볼륨
        if (volumeSlider != null)
        {
            volumeSlider.value = currentResult.volumeScore / 100f;
        }
        if (volumeText != null)
        {
            volumeText.text = $"{currentResult.volumeScore:F1}점";
        }
        
        // 속도
        if (paceSlider != null)
        {
            paceSlider.value = currentResult.paceScore / 100f;
        }
        if (paceText != null)
        {
            paceText.text = $"{currentResult.paceScore:F1}점";
        }
    }
    
    /// <summary>
    /// 피드백 업데이트
    /// </summary>
    private void UpdateFeedback()
    {
        // 강점 표시
        if (strengthsText != null)
        {
            strengthsText.text = "강점:\n• " + string.Join("\n• ", currentResult.strengths);
        }
        
        // 개선점 표시
        if (improvementsText != null)
        {
            improvementsText.text = "개선점:\n• " + string.Join("\n• ", currentResult.improvements);
        }
    }
    
    /// <summary>
    /// 세부 정보 업데이트
    /// </summary>
    private void UpdateDetails()
    {
        if (detailsText != null)
        {
            string details = $"발표 날짜: {currentResult.presentationDate:yyyy-MM-dd HH:mm}\n";
            details += $"발표 시간: {currentResult.totalDuration.Minutes}분 {currentResult.totalDuration.Seconds}초\n";
            details += $"총 슬라이드: {currentResult.totalSlides}장\n";
            details += $"분석 구간: {currentResult.analyzedSegments}개";
            
            detailsText.text = details;
        }
    }
    
    /// <summary>
    /// 등급 색상 반환
    /// </summary>
    /// <param name="score">점수</param>
    /// <returns>등급 색상</returns>
    private Color GetGradeColor(float score)
    {
        if (score >= 85f) return gradeColors[4]; // A
        else if (score >= 70f) return gradeColors[3]; // B
        else if (score >= 50f) return gradeColors[2]; // C
        else if (score >= 30f) return gradeColors[1]; // D
        else return gradeColors[0]; // F
    }
    
    /// <summary>
    /// 발표 재시작
    /// </summary>
    private void RestartPresentation()
    {
        // 결과 패널 비활성화
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        // 모든 시스템 초기화
        ResetAllSystems();
        
        // 이벤트 발생
        OnRestartRequested?.Invoke();
        
        Debug.Log("발표 재시작");
    }
    
    /// <summary>
    /// 결과 저장
    /// </summary>
    private void SaveResult()
    {
        if (currentResult == null) return;
        
        // JSON으로 변환하여 저장
        string json = JsonUtility.ToJson(currentResult, true);
        string fileName = $"PresentationResult_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Application.persistentDataPath + "/" + fileName;
        
        try
        {
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"결과 저장 완료: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"결과 저장 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// 발표 종료
    /// </summary>
    private void ExitPresentation()
    {
        OnExitRequested?.Invoke();
        Debug.Log("발표 종료");
    }
    
    /// <summary>
    /// 모든 시스템 초기화
    /// </summary>
    private void ResetAllSystems()
    {
        // 음성 분석기 초기화
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.StopAnalysis();
            voiceAnalyzer.ClearAnalysisHistory();
        }
        
        // 슬라이드 관리자 초기화
        if (transitionManager != null)
        {
            transitionManager.currentSlideIndex = 0;
            transitionManager.isPresenting = false;
        }
        
        // 피드백 매니저 초기화
        var feedbackManager = FindObjectOfType<FeedbackManager>();
        if (feedbackManager != null)
        {
            feedbackManager.ClearFeedbackQueue();
        }
        
        // 청중 반응 매니저 초기화
        var audienceManager = FindObjectOfType<AudienceReactionManager>();
        if (audienceManager != null)
        {
            audienceManager.ClearReactionQueue();
            audienceManager.ClearAllVFX();
        }
    }
    
    /// <summary>
    /// 수동 결과 표시
    /// </summary>
    /// <param name="result">표시할 결과</param>
    public void DisplayManualResult(PresentationResult result)
    {
        currentResult = result;
        DisplayResult();
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart -= OnPresentationStart;
            transitionManager.OnPresentationEnd -= OnPresentationEnd;
        }
    }
} 