using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoiceAnalyzerTestManager : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Button startButton;
    public Button stopButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI logText;
    public TextMeshProUGUI resultText;
    public Slider progressSlider;
    
    [Header("VoiceAnalyzer")]
    public VoiceAnalyzer voiceAnalyzer;
    
    private bool isRecording = false;
    private float recordingTimer = 0f;
    private int recordingCount = 0;
    
    void Start()
    {
        // UI 초기화
        InitializeUI();
        
        // VoiceAnalyzer 설정
        SetupVoiceAnalyzer();
        
        // 초기 상태 표시
        UpdateStatus("준비 완료");
        LogMessage("VoiceAnalyzer 테스트 준비 완료");
    }
    
    void Update()
    {
        // 녹음 중일 때 타이머 업데이트
        if (isRecording)
        {
            recordingTimer += Time.deltaTime;
            
            // 진행률 표시 (10초 기준)
            float progress = (recordingTimer % 10f) / 10f;
            progressSlider.value = progress;
            
            // 카운트다운 표시
            float countdown = 10f - (recordingTimer % 10f);
            UpdateStatus($"녹음 중... 다음 전송까지 {countdown:F1}초");
        }
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 버튼 이벤트 연결
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartRecording);
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(StopRecording);
            stopButton.interactable = false;
        }
        
        // 진행률 슬라이더 초기화
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }
    }
    
    /// <summary>
    /// VoiceAnalyzer 설정
    /// </summary>
    private void SetupVoiceAnalyzer()
    {
        if (voiceAnalyzer == null)
        {
            voiceAnalyzer = GetComponent<VoiceAnalyzer>();
        }
        
        if (voiceAnalyzer == null)
        {
            LogMessage("❌ VoiceAnalyzer 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        // 이벤트 구독
        voiceAnalyzer.OnAnalysisCompleted += OnAnalysisCompleted;
        voiceAnalyzer.OnVoiceDataReceived += OnVoiceDataReceived;
        
        // 설정 확인
        LogMessage($"✅ VoiceAnalyzer 설정 완료");
        LogMessage($"📡 WebSocket URL: {voiceAnalyzer.websocketUrl}");
        LogMessage($"🔄 자동 재연결: {voiceAnalyzer.autoReconnect}");
        LogMessage($"⏰ 녹음 간격: {voiceAnalyzer.analysisInterval}초");
    }
    
    /// <summary>
    /// 녹음 시작
    /// </summary>
    public void StartRecording()
    {
        if (voiceAnalyzer == null)
        {
            LogMessage("❌ VoiceAnalyzer가 없습니다!");
            return;
        }
        
        LogMessage("🎤 녹음 시작");
        UpdateStatus("녹음 시작 중...");
        
        voiceAnalyzer.StartAnalysis();
        
        isRecording = true;
        recordingTimer = 0f;
        recordingCount = 0;
        
        // 버튼 상태 변경
        startButton.interactable = false;
        stopButton.interactable = true;
        
        UpdateStatus("녹음 중... WebSocket 연결 대기");
    }
    
    /// <summary>
    /// 녹음 중지
    /// </summary>
    public void StopRecording()
    {
        if (voiceAnalyzer == null)
        {
            LogMessage("❌ VoiceAnalyzer가 없습니다!");
            return;
        }
        
        LogMessage("🛑 녹음 중지");
        UpdateStatus("녹음 중지 중...");
        
        voiceAnalyzer.StopAnalysis();
        
        isRecording = false;
        recordingTimer = 0f;
        progressSlider.value = 0f;
        
        // 버튼 상태 변경
        startButton.interactable = true;
        stopButton.interactable = false;
        
        UpdateStatus("녹음 중지됨");
    }
    
    /// <summary>
    /// 음성 분석 완료 이벤트
    /// </summary>
    private void OnAnalysisCompleted(AnalysisResult result)
    {
        recordingCount++;
        
        LogMessage($"📊 분석 완료 #{recordingCount}");
        LogMessage($"   점수: {result.overallScore:F1}점");
        LogMessage($"   피드백: {result.feedback}");
        
        // 결과 표시
        if (resultText != null)
        {
            resultText.text = $"분석 #{recordingCount}\n점수: {result.overallScore:F1}점\n{result.feedback}";
        }
        
        UpdateStatus($"분석 완료 #{recordingCount} - 점수: {result.overallScore:F1}점");
    }
    
    /// <summary>
    /// 음성 데이터 수신 이벤트
    /// </summary>
    private void OnVoiceDataReceived(VoiceAnalysisData data)
    {
        LogMessage($"📡 서버 응답 수신:");
        LogMessage($"   WPM: {data.wpm:F1}");
        LogMessage($"   볼륨: {data.volume:F3}");
        LogMessage($"   명확도: {data.clarityScore:F1}");
        LogMessage($"   STT시간: {data.sttTime:F1}초");
        LogMessage($"   지속시간: {data.duration:F1}초");
        
        if (!string.IsNullOrEmpty(data.recognizedText))
        {
            LogMessage($"   인식된 텍스트: {data.recognizedText.Substring(0, Mathf.Min(50, data.recognizedText.Length))}...");
        }
    }
    
    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = $"상태: {status}";
        }
        
        Debug.Log($"[VoiceAnalyzerTest] {status}");
    }
    
    /// <summary>
    /// 로그 메시지 추가
    /// </summary>
    private void LogMessage(string message)
    {
        if (logText != null)
        {
            logText.text += $"\n{System.DateTime.Now:HH:mm:ss} {message}";
            
            // 로그가 너무 길어지지 않도록 제한
            string[] lines = logText.text.Split('\n');
            if (lines.Length > 20)
            {
                string[] recentLines = new string[20];
                System.Array.Copy(lines, lines.Length - 20, recentLines, 0, 20);
                logText.text = string.Join("\n", recentLines);
            }
        }
        
        Debug.Log($"[VoiceAnalyzerTest] {message}");
    }
    
    /// <summary>
    /// 연결 상태 확인
    /// </summary>
    public void CheckConnection()
    {
        if (voiceAnalyzer != null)
        {
            bool isConnected = voiceAnalyzer.IsWebSocketConnected();
            LogMessage($"🔗 WebSocket 연결 상태: {(isConnected ? "연결됨" : "연결 안됨")}");
            
            if (!isConnected && voiceAnalyzer.isAnalyzing)
            {
                LogMessage("⚠️ 분석 중이지만 WebSocket 연결이 끊어졌습니다.");
            }
        }
    }
    
    /// <summary>
    /// 로그 지우기
    /// </summary>
    public void ClearLog()
    {
        if (logText != null)
        {
            logText.text = "";
        }
        
        LogMessage("로그 지워짐");
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted -= OnAnalysisCompleted;
            voiceAnalyzer.OnVoiceDataReceived -= OnVoiceDataReceived;
        }
    }
} 