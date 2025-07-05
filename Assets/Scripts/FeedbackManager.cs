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
    public BoxCollider startCollider; // 시작 감지 콜라이더
    public Button endButton; // 종료 버튼
    public TextMeshProUGUI statusText; // 상태 텍스트
    
    [Header("시작 감지 설정")]
    public string playerTag = "Player"; // 플레이어 태그
    public bool usePlayerTag = true; // 플레이어 태그 사용 여부
    
    [Header("중요: 이 스크립트가 부착된 GameObject에 BoxCollider가 있어야 합니다!")]
    [Tooltip("이 FeedbackManager 스크립트가 부착된 GameObject에 BoxCollider(isTrigger=true)를 추가해주세요.")]
    public bool setupInfo = true;
    
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
        
        // 시작 콜라이더 및 버튼 이벤트 연결
        SetupColliderAndButtons();
        
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
    /// 외부에서 발표 시작 (Public 메서드)
    /// </summary>
    public void StartPresentationPublic()
    {
        Debug.Log("🎯 외부에서 발표 시작 요청됨!");
        StartPresentation();
    }
    
    /// <summary>
    /// 콜라이더 테스트용 메서드 (Inspector에서 직접 호출 가능)
    /// </summary>
    [ContextMenu("콜라이더 테스트")]
    public void TestCollider()
    {
        Debug.Log("🧪 콜라이더 테스트 메서드 호출됨!");
        if (startCollider != null)
        {
            Debug.Log($"시작 콜라이더 상태: isTrigger={startCollider.isTrigger}, enabled={startCollider.enabled}");
            Debug.Log($"시작 콜라이더 GameObject: {startCollider.gameObject.name}, active={startCollider.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("시작 콜라이더가 null입니다!");
        }
    }
    
    /// <summary>
    /// 강제로 발표 시작 시뮬레이션
    /// </summary>
    [ContextMenu("강제 발표 시작")]
    public void ForceStartPresentation()
    {
        Debug.Log("🔥 강제 발표 시작 시뮬레이션!");
        StartPresentation();
    }
    
    /// <summary>
    /// 외부에서 발표 종료 (Public 메서드)
    /// </summary>
    public void EndPresentationPublic()
    {
        EndPresentation();
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
    
    /// <summary>
    /// 시작 콜라이더 및 버튼 이벤트 설정
    /// </summary>
    private void SetupColliderAndButtons()
    {
        // 시작 콜라이더 설정
        if (startCollider != null)
        {
            // 트리거로 설정되어 있는지 확인
            if (!startCollider.isTrigger)
            {
                Debug.LogWarning("시작 콜라이더가 Trigger로 설정되어 있지 않습니다. Trigger로 설정합니다.");
                startCollider.isTrigger = true;
            }
            
            Debug.Log($"시작 콜라이더 설정 완료 - 콜라이더명: {startCollider.name}");
        }
        else
        {
            Debug.LogError("시작 콜라이더가 할당되지 않았습니다! Inspector에서 Start Collider를 할당해주세요.");
        }
        
        // 종료 버튼 이벤트
        if (endButton != null)
        {
            // 기존 이벤트 제거 후 새로 추가
            endButton.onClick.RemoveAllListeners();
            endButton.onClick.AddListener(EndPresentation);
            
            // 버튼 상태 확인 및 설정
            endButton.interactable = false;
            
            Debug.Log("종료 버튼 이벤트 연결 완료");
        }
        else
        {
            Debug.LogWarning("종료 버튼이 할당되지 않았습니다! Inspector에서 End Button을 할당해주세요.");
        }
    }
    
    /// <summary>
    /// 트리거 진입 시 발표 시작
    /// </summary>
    /// <param name="other">진입한 콜라이더</param>
    void OnTriggerEnter(Collider other)
    {
        // 발표가 이미 시작된 경우 무시
        if (transitionManager != null && transitionManager.isPresenting)
        {
            return;
        }
        
        // 플레이어 태그 확인 (옵션)
        if (usePlayerTag && !other.CompareTag(playerTag))
        {
            return;
        }
        
        Debug.Log($"🎯 시작 트리거 감지! 진입 객체: {other.name}");
        StartPresentation();
    }
    
    /// <summary>
    /// 트리거 탈출 시 (선택적으로 사용 가능)
    /// </summary>
    /// <param name="other">탈출한 콜라이더</param>
    void OnTriggerExit(Collider other)
    {
        // 필요한 경우 여기에 로직 추가
        // 현재는 사용하지 않음
    }
    
    /// <summary>
    /// 발표 시작
    /// </summary>
    private void StartPresentation()
    {
        Debug.Log("===== 발표 시작 트리거 감지됨! =====");
        
        // 이미 발표가 진행 중인 경우 무시
        if (transitionManager != null && transitionManager.isPresenting)
        {
            Debug.Log("이미 발표가 진행 중입니다.");
            return;
        }
        
        if (transitionManager != null)
        {
            Debug.Log("TransitionManager 발견, StartPresentation 호출");
            transitionManager.StartPresentation();
            Debug.Log("TransitionManager.StartPresentation 호출 완료");
        }
        else
        {
            Debug.LogError("TransitionManager를 찾을 수 없습니다!");
        }
        
        Debug.Log("===== 발표 시작 메서드 완료 =====");
    }
    
    /// <summary>
    /// 발표 종료
    /// </summary>
    private void EndPresentation()
    {
        if (transitionManager != null)
        {
            transitionManager.EndPresentation();
        }
        else
        {
            Debug.LogError("TransitionManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 발표 시작 시 호출
    /// </summary>
    private void OnPresentationStarted()
    {
        Debug.Log("발표가 시작되었습니다!");
        UpdateUI();
    }
    
    /// <summary>
    /// 발표 종료 시 호출
    /// </summary>
    private void OnPresentationEnded()
    {
        Debug.Log("발표가 종료되었습니다!");
        UpdateUI();
        ClearFeedbackQueue();
    }
    
    /// <summary>
    /// 슬라이드 변경 시 호출
    /// </summary>
    /// <param name="slideIndex">변경된 슬라이드 인덱스</param>
    private void OnSlideChanged(int slideIndex)
    {
        Debug.Log($"슬라이드 변경: {slideIndex + 1}");
        // UI 업데이트는 Update에서 실시간으로 처리됨
    }
    
    /// <summary>
    /// UI 상태 업데이트
    /// </summary>
    private void UpdateUI()
    {
        bool isPresenting = transitionManager != null && transitionManager.isPresenting;
        
        // 종료 버튼 활성화/비활성화
        if (endButton != null)
        {
            endButton.interactable = isPresenting;
        }
        
        // 상태 텍스트 업데이트
        if (statusText != null)
        {
            if (isPresenting)
            {
                var slideInfo = transitionManager.GetSlideInfo();
                if (transitionManager.isOnLastSlide)
                {
                    statusText.text = $"발표 진행 중 ({slideInfo.current}/{slideInfo.total}) - 한 번 더 클릭하면 종료";
                }
                else
                {
                    statusText.text = $"발표 진행 중 ({slideInfo.current}/{slideInfo.total})";
                }
            }
            else
            {
                statusText.text = "발표 대기 중 - 시작 영역에 진입하세요";
            }
        }
    }
    
    /// <summary>
    /// 지연된 UI 업데이트
    /// </summary>
    private IEnumerator UpdateUIDelayed()
    {
        // UI 시스템 초기화 완료 대기
        yield return null;
        
        // UI 업데이트
        UpdateUI();
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
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
        
        // 버튼 이벤트 해제
        if (endButton != null)
        {
            endButton.onClick.RemoveListener(EndPresentation);
        }
    }
} 