using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class VRPresentationManager : MonoBehaviour
{
    [Header("시스템 컴포넌트")]
    public TransitionManager transitionManager;
    public HandPresentationController handController;
    public VoiceAnalyzer voiceAnalyzer;
    public FeedbackManager feedbackManager;
    public AudienceReactionManager audienceManager;
    public PresentationResultManager resultManager;
    
    [Header("VR 컨트롤러")]
    public XRBaseController leftController;
    public XRBaseController rightController;
    
    [Header("발표 설정")]
    public bool autoStart = false; // 자동 시작 여부
    public float startDelay = 2f; // 시작 지연 시간
    
    [Header("UI 버튼 제어")]
    public Button startButton; // 시작 버튼
    public Button stopButton; // 중지 버튼
    public Button restartButton; // 재시작 버튼
    public GameObject startUI; // 시작 UI 패널
    public GameObject presentationUI; // 발표 중 UI 패널
    
    [Header("발표 상태")]
    public bool isPresentationActive = false;
    public bool isSystemReady = false;
    public bool isWaitingForStart = false; // 시작 대기 상태
    
    [Header("이벤트")]
    public System.Action OnSystemReady; // 시스템 준비 완료 이벤트
    public System.Action OnPresentationStarted; // 발표 시작 이벤트
    public System.Action OnPresentationEnded; // 발표 종료 이벤트
    
    void Start()
    {
        // 시스템 초기화
        StartCoroutine(InitializeSystem());
    }
    
    /// <summary>
    /// 시스템 초기화 코루틴
    /// </summary>
    private IEnumerator InitializeSystem()
    {
        Debug.Log("VR 발표 시뮬레이터 시스템 초기화 시작");
        
        // 컴포넌트 자동 찾기
        FindSystemComponents();
        
        // 컴포넌트 검증
        yield return StartCoroutine(ValidateComponents());
        
        // 시스템 설정
        SetupSystem();
        
        // 이벤트 연결
        ConnectEvents();
        
        // 시스템 준비 완료
        isSystemReady = true;
        OnSystemReady?.Invoke();
        
        Debug.Log("시스템 초기화 완료");
        
        // 자동 시작이 활성화된 경우
        if (autoStart)
        {
            yield return new WaitForSeconds(startDelay);
            StartPresentation();
        }
    }
    
    /// <summary>
    /// 시스템 컴포넌트 자동 찾기
    /// </summary>
    private void FindSystemComponents()
    {
        // 각 컴포넌트가 없으면 자동으로 찾기
        if (transitionManager == null)
        {
            transitionManager = FindObjectOfType<TransitionManager>();
        }
        
        if (handController == null)
        {
            handController = FindObjectOfType<HandPresentationController>();
        }
        
        if (voiceAnalyzer == null)
        {
            voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();
        }
        
        if (feedbackManager == null)
        {
            feedbackManager = FindObjectOfType<FeedbackManager>();
        }
        
        if (audienceManager == null)
        {
            audienceManager = FindObjectOfType<AudienceReactionManager>();
        }
        
        if (resultManager == null)
        {
            resultManager = FindObjectOfType<PresentationResultManager>();
        }
        
        // 컨트롤러 자동 찾기
        if (leftController == null || rightController == null)
        {
            FindControllers();
        }
    }
    
    /// <summary>
    /// VR 컨트롤러 찾기
    /// </summary>
    private void FindControllers()
    {
        // 먼저 XRController 찾기 (XROrigin에서 주로 사용)
        var xrControllers = FindObjectsOfType<XRController>();
        foreach (var controller in xrControllers)
        {
            string controllerName = controller.name.ToLower();
            if (controllerName.Contains("left"))
            {
                leftController = controller;
            }
            else if (controllerName.Contains("right"))
            {
                rightController = controller;
            }
        }
        
        // XRController를 못 찾으면 다른 XRBaseController 찾기
        if (leftController == null || rightController == null)
        {
            var controllers = FindObjectsOfType<XRBaseController>();
            
            foreach (var controller in controllers)
            {
                string controllerName = controller.name.ToLower();
                if (controllerName.Contains("left") && leftController == null)
                {
                    leftController = controller;
                }
                else if (controllerName.Contains("right") && rightController == null)
                {
                    rightController = controller;
                }
            }
        }
        
        // 찾은 컨트롤러 정보 로그
        Debug.Log($"Left Controller: {(leftController != null ? leftController.name : "Not Found")}");
        Debug.Log($"Right Controller: {(rightController != null ? rightController.name : "Not Found")}");
    }
    
    /// <summary>
    /// 컴포넌트 유효성 검증
    /// </summary>
    private IEnumerator ValidateComponents()
    {
        bool allValid = true;
        
        // 필수 컴포넌트 검증
        if (transitionManager == null)
        {
            Debug.LogError("TransitionManager를 찾을 수 없습니다.");
            allValid = false;
        }
        
        if (voiceAnalyzer == null)
        {
            Debug.LogError("VoiceAnalyzer를 찾을 수 없습니다.");
            allValid = false;
        }
        
        if (feedbackManager == null)
        {
            Debug.LogError("FeedbackManager를 찾을 수 없습니다.");
            allValid = false;
        }
        
        if (leftController == null || rightController == null)
        {
            Debug.LogWarning("VR 컨트롤러를 찾을 수 없습니다. 수동으로 할당해주세요.");
        }
        
        if (!allValid)
        {
            Debug.LogError("필수 컴포넌트가 누락되었습니다. 시스템을 초기화할 수 없습니다.");
            yield break;
        }
        
        yield return null;
    }
    
    /// <summary>
    /// 시스템 설정
    /// </summary>
    private void SetupSystem()
    {
        // 컨트롤러 설정
        if (transitionManager != null && rightController != null)
        {
            transitionManager.rightController = rightController;
        }
        
        if (handController != null && leftController != null)
        {
            handController.leftController = leftController;
        }
        
        // UI 버튼 설정
        SetupUIButtons();
        
        // 초기 UI 상태 설정
        SetUIState(UIState.WaitingForStart);
        
        Debug.Log("시스템 설정 완료");
    }
    
    /// <summary>
    /// UI 버튼 이벤트 설정
    /// </summary>
    private void SetupUIButtons()
    {
        // 시작 버튼
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        // 중지 버튼
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }
        
        // 재시작 버튼
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        
        Debug.Log("UI 버튼 이벤트 설정 완료");
    }
    
    /// <summary>
    /// UI 상태 열거형
    /// </summary>
    public enum UIState
    {
        WaitingForStart,    // 시작 대기
        Presenting,         // 발표 중
        Paused,            // 일시 정지
        Finished           // 완료
    }
    
    /// <summary>
    /// UI 상태 설정
    /// </summary>
    /// <param name="state">설정할 UI 상태</param>
    private void SetUIState(UIState state)
    {
        switch (state)
        {
            case UIState.WaitingForStart:
                if (startUI != null) startUI.SetActive(true);
                if (presentationUI != null) presentationUI.SetActive(false);
                if (startButton != null) startButton.gameObject.SetActive(true);
                if (stopButton != null) stopButton.gameObject.SetActive(false);
                if (restartButton != null) restartButton.gameObject.SetActive(false);
                isWaitingForStart = true;
                break;
                
            case UIState.Presenting:
                if (startUI != null) startUI.SetActive(false);
                if (presentationUI != null) presentationUI.SetActive(true);
                if (startButton != null) startButton.gameObject.SetActive(false);
                if (stopButton != null) stopButton.gameObject.SetActive(true);
                if (restartButton != null) restartButton.gameObject.SetActive(false);
                isWaitingForStart = false;
                break;
                
            case UIState.Finished:
                if (startUI != null) startUI.SetActive(false);
                if (presentationUI != null) presentationUI.SetActive(true);
                if (startButton != null) startButton.gameObject.SetActive(false);
                if (stopButton != null) stopButton.gameObject.SetActive(false);
                if (restartButton != null) restartButton.gameObject.SetActive(true);
                isWaitingForStart = false;
                break;
        }
        
        Debug.Log($"UI 상태 변경: {state}");
    }
    
    /// <summary>
    /// 이벤트 연결
    /// </summary>
    private void ConnectEvents()
    {
        // 발표 결과 매니저 이벤트
        if (resultManager != null)
        {
            resultManager.OnRestartRequested += RestartPresentation;
            resultManager.OnExitRequested += ExitPresentation;
        }
        
        // 슬라이드 전환 매니저 이벤트
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart += OnPresentationStart;
            transitionManager.OnPresentationEnd += OnPresentationEnd;
        }
        
        Debug.Log("이벤트 연결 완료");
    }
    
    /// <summary>
    /// 발표 시작
    /// </summary>
    public void StartPresentation()
    {
        if (!isSystemReady)
        {
            Debug.LogWarning("시스템이 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }
        
        if (isPresentationActive)
        {
            Debug.LogWarning("이미 발표가 진행 중입니다.");
            return;
        }
        
        Debug.Log("발표 시작!");
        
        // 발표 활성화
        isPresentationActive = true;
        
        // 시스템 시작
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.StartAnalysis();
        }
        
        if (feedbackManager != null)
        {
            feedbackManager.SetRealTimeFeedbackEnabled(true);
        }
        
        // AudienceReactionManager와 PresentationResultManager는 자동으로 동작
        
        // 발표 시작 이벤트 발생
        OnPresentationStarted?.Invoke();
        
        // UI 상태 변경
        SetUIState(UIState.Presenting);
        
        Debug.Log("발표가 시작되었습니다!");
    }
    
    /// <summary>
    /// 발표 종료
    /// </summary>
    public void StopPresentation()
    {
        if (!isPresentationActive)
        {
            Debug.LogWarning("발표가 진행 중이지 않습니다.");
            return;
        }
        
        Debug.Log("발표 종료");
        
        // 발표 비활성화
        isPresentationActive = false;
        
        // 시스템 종료
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.StopAnalysis();
        }
        
        if (feedbackManager != null)
        {
            feedbackManager.SetRealTimeFeedbackEnabled(false);
        }
        
        // AudienceReactionManager와 PresentationResultManager는 자동으로 종료 처리
        
        // 발표 종료 이벤트 발생
        OnPresentationEnded?.Invoke();
        
        // UI 상태 변경
        SetUIState(UIState.Finished);
        
        Debug.Log("발표가 종료되었습니다!");
    }
    
    /// <summary>
    /// 발표 재시작
    /// </summary>
    public void RestartPresentation()
    {
        StopPresentation();
        
        // 잠시 대기 후 재시작
        StartCoroutine(RestartAfterDelay());
    }
    
    /// <summary>
    /// 지연 후 재시작 코루틴
    /// </summary>
    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        StartPresentation();
    }
    
    /// <summary>
    /// 발표 종료
    /// </summary>
    public void ExitPresentation()
    {
        StopPresentation();
        
        // 애플리케이션 종료 또는 메인 메뉴로 이동
        Debug.Log("발표 프로그램 종료");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 발표 시작 이벤트 처리
    /// </summary>
    private void OnPresentationStart()
    {
        Debug.Log("발표 시작 이벤트 수신");
    }
    
    /// <summary>
    /// 발표 종료 이벤트 처리
    /// </summary>
    private void OnPresentationEnd()
    {
        isPresentationActive = false;
        OnPresentationEnded?.Invoke();
        Debug.Log("발표 종료 이벤트 수신");
    }
    
    /// <summary>
    /// 시스템 상태 정보 반환
    /// </summary>
    /// <returns>시스템 상태 정보</returns>
    public string GetSystemStatus()
    {
        string status = "=== VR 발표 시뮬레이터 상태 ===\n";
        status += $"시스템 준비: {(isSystemReady ? "완료" : "미완료")}\n";
        status += $"발표 진행: {(isPresentationActive ? "진행 중" : "중지")}\n";
        status += $"슬라이드 관리자: {(transitionManager != null ? "연결됨" : "없음")}\n";
        status += $"음성 분석기: {(voiceAnalyzer != null ? "연결됨" : "없음")}\n";
        status += $"피드백 매니저: {(feedbackManager != null ? "연결됨" : "없음")}\n";
        status += $"청중 반응 매니저: {(audienceManager != null ? "연결됨" : "없음")}\n";
        status += $"결과 매니저: {(resultManager != null ? "연결됨" : "없음")}\n";
        status += $"왼쪽 컨트롤러: {(leftController != null ? "연결됨" : "없음")}\n";
        status += $"오른쪽 컨트롤러: {(rightController != null ? "연결됨" : "없음")}\n";
        
        return status;
    }
    
    /// <summary>
    /// 시스템 상태 로그 출력
    /// </summary>
    public void LogSystemStatus()
    {
        Debug.Log(GetSystemStatus());
    }
    
    /// <summary>
    /// 긴급 시스템 재시작
    /// </summary>
    public void EmergencyRestart()
    {
        Debug.Log("긴급 시스템 재시작 실행");
        
        // 모든 시스템 강제 중지
        StopPresentation();
        
        // 시스템 재초기화
        StartCoroutine(InitializeSystem());
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (resultManager != null)
        {
            // 결과 관리 이벤트 해제
            resultManager.OnRestartRequested -= RestartPresentation;
            resultManager.OnExitRequested -= ExitPresentation;
        }
        
        // UI 버튼 이벤트 해제
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(OnStopButtonClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        }
        
        Debug.Log("VRPresentationManager 리소스 해제 완료");
    }
    
    // 디버그용 메서드들
    #if UNITY_EDITOR
    [ContextMenu("시스템 상태 확인")]
    private void DebugSystemStatus()
    {
        LogSystemStatus();
    }
    
    [ContextMenu("발표 시작 (디버그)")]
    private void DebugStartPresentation()
    {
        StartPresentation();
    }
    
    [ContextMenu("발표 중지 (디버그)")]
    private void DebugStopPresentation()
    {
        StopPresentation();
    }
    
    [ContextMenu("시스템 재시작 (디버그)")]
    private void DebugRestartSystem()
    {
        EmergencyRestart();
    }
    #endif
    
    /// <summary>
    /// 시작 버튼 클릭 이벤트
    /// </summary>
    public void OnStartButtonClicked()
    {
        Debug.Log("시작 버튼 클릭됨");
        StartPresentation();
    }
    
    /// <summary>
    /// 중지 버튼 클릭 이벤트
    /// </summary>
    public void OnStopButtonClicked()
    {
        Debug.Log("중지 버튼 클릭됨");
        StopPresentation();
    }
    
    /// <summary>
    /// 재시작 버튼 클릭 이벤트
    /// </summary>
    public void OnRestartButtonClicked()
    {
        Debug.Log("재시작 버튼 클릭됨");
        RestartPresentation();
    }
} 