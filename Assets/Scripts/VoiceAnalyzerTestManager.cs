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
    
    // 음성 데이터 저장용
    private VoiceAnalysisData lastReceivedData;
    private VoiceAnalysisData currentDisplayData;
    private float lastDataReceivedTime = 0f;
    private bool isNewDataReceived = false;
    
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
            
            // 새로운 데이터 표시 관리
            ManageDataDisplay();
        }
    }
    
    /// <summary>
    /// 데이터 표시 관리 (새 데이터 표시 후 3초 뒤 숨김)
    /// </summary>
    private void ManageDataDisplay()
    {
        if (isNewDataReceived)
        {
            // 새 데이터 받은 후 3초가 지나면 표시 지우기
            if (Time.time - lastDataReceivedTime > 3f)
            {
                ClearCurrentDataDisplay();
                isNewDataReceived = false;
                // 🔧 중요: currentDisplayData도 초기화해서 다음 비교 준비
                currentDisplayData = null;
            }
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
        
        // 데이터 초기화
        lastReceivedData = null;
        currentDisplayData = null;
        lastDataReceivedTime = 0f;
        isNewDataReceived = false;
        
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
    /// 음성 데이터 수신 이벤트 (새로운 데이터만 표시)
    /// </summary>
    private void OnVoiceDataReceived(VoiceAnalysisData data)
    {
        // 항상 최신 데이터 저장
        lastReceivedData = data;
        
        // 새로운 데이터인지 확인 (타임스탬프 또는 내용 비교)
        bool isNewData = IsNewData(data);
        
        if (isNewData)
        {
            // 새로운 데이터 표시
            DisplayNewData(data);
            currentDisplayData = data;
            lastDataReceivedTime = Time.time;
            isNewDataReceived = true;
        }
        
        // 디버그용 (내부 저장은 계속 유지, 1초마다 한번만)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VoiceData] WPM: {data.wpm:F1}, 볼륨: {data.volume:F3}, 명확도: {data.clarityScore:F1}");
        }
    }
    
    /// <summary>
    /// 새로운 데이터인지 확인 (시간 기반 필터링 + 의미있는 변화)
    /// </summary>
    private bool IsNewData(VoiceAnalysisData newData)
    {
        // 첫 번째 데이터는 표시
        if (currentDisplayData == null)
        {
            Debug.Log("🔍 [IsNewData] 첫 번째 데이터 → 표시");
            return true;
        }
        
        // 🔧 시간 기반 필터링: 마지막 표시 후 최소 8초는 지나야 함
        float timeSinceLastDisplay = Time.time - lastDataReceivedTime;
        if (timeSinceLastDisplay < 8f)
        {
            Debug.Log($"🔍 [IsNewData] 시간 부족 ({timeSinceLastDisplay:F1}초 < 8초) → 무시");
            return false; // 시간이 충분히 지나지 않으면 무조건 false
        }
        
        // 🔧 의미있는 변화가 있는지 확인 (더 엄격한 조건)
        bool significantWpmChange = Mathf.Abs(newData.wpm - currentDisplayData.wpm) >= 5f;
        bool textChanged = newData.recognizedText != currentDisplayData.recognizedText;
        bool clarityChanged = Mathf.Abs(newData.clarityScore - currentDisplayData.clarityScore) >= 10f;
        
        bool result = significantWpmChange || textChanged || clarityChanged;
        Debug.Log($"🔍 [IsNewData] 시간 OK ({timeSinceLastDisplay:F1}초), 변화 감지: WPM={significantWpmChange}, 텍스트={textChanged}, 명확도={clarityChanged} → {(result ? "표시" : "무시")}");
        
        return result;
    }
    
    /// <summary>
    /// 새로운 데이터 표시
    /// </summary>
    private void DisplayNewData(VoiceAnalysisData data)
    {
        LogMessage($"📡 새로운 서버 응답 수신:");
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
    /// 현재 데이터 표시 지우기
    /// </summary>
    private void ClearCurrentDataDisplay()
    {
        LogMessage("📡 데이터 표시 정리됨 (내부 저장은 유지, 다음 비교 준비)");
        Debug.Log("🧹 [ClearCurrentDataDisplay] currentDisplayData 초기화됨");
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
    
    /// <summary>
    /// 저장된 데이터 조회 (디버깅용)
    /// </summary>
    [ContextMenu("저장된 데이터 확인")]
    public void ShowStoredData()
    {
        if (lastReceivedData != null)
        {
            LogMessage("💾 저장된 최신 데이터:");
            LogMessage($"   WPM: {lastReceivedData.wpm:F1}");
            LogMessage($"   볼륨: {lastReceivedData.volume:F3}");
            LogMessage($"   명확도: {lastReceivedData.clarityScore:F1}");
            LogMessage($"   타임스탬프: {lastReceivedData.timestamp}");
        }
        else
        {
            LogMessage("💾 저장된 데이터가 없습니다.");
        }
    }
    
    /// <summary>
    /// 플래그 상태 확인 (디버깅용)
    /// </summary>
    [ContextMenu("플래그 상태 확인")]
    public void CheckFlags()
    {
        LogMessage("🏴 현재 플래그 상태:");
        LogMessage($"   isNewDataReceived: {isNewDataReceived}");
        LogMessage($"   currentDisplayData: {(currentDisplayData != null ? "있음" : "없음")}");
        LogMessage($"   lastDataReceivedTime: {lastDataReceivedTime:F1}초");
        LogMessage($"   현재 시간: {Time.time:F1}초");
        LogMessage($"   마지막 표시 후 경과 시간: {(Time.time - lastDataReceivedTime):F1}초");
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