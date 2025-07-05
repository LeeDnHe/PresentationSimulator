using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

[System.Serializable]
public class VoiceAnalysisData
{
    public float confidence; // 자신감 수준 (0-1)
    public float speechRate; // 말하기 속도 (분당 단어 수)
    public float volume; // 음성 볼륨 (0-1)
    public float clarity; // 발음 명확도 (0-1)
    public List<string> detectedWords = new List<string>(); // 감지된 단어들
    public DateTime timestamp; // 분석 시간
    
    // 서버에서 받은 원시 데이터
    public float wpm; // Words Per Minute
    public float pitchVariation; // 음성 변화
    public float clarityScore; // 명확도 점수
    public string recognizedText; // 인식된 텍스트
    public float sttTime; // 음성 인식 처리 시간
    public float duration; // 음성 지속 시간
}

[System.Serializable]
public class ServerResponse
{
    public float wpm;
    public float volume;
    public float pitch_variation;
    public float clarity_score;
    public string recognized_text;
    public float stt_time; // 음성 인식 처리 시간
    public float duration; // 음성 지속 시간
}

[System.Serializable]
public class AudioMessage
{
    public string type = "audio";
    public string format = "wav";
    public int sample_rate;
    public int duration;
    public string data; // Base64 인코딩된 오디오 데이터
    public string timestamp;
}

[System.Serializable]
public class AnalysisResult
{
    public string feedback; // 피드백 메시지
    public float overallScore; // 전체 점수 (0-100)
    public Color feedbackColor; // 피드백 색상
    public VoiceAnalysisData analysisData; // 분석 데이터
}

public class VoiceAnalyzer : MonoBehaviour
{
    [Header("음성 분석 설정")]
    public float analysisInterval = 10f; // 분석 간격 (초) - 10초마다 전송
    public float minVolumeThreshold = 0.01f; // 최소 음성 볼륨 임계값
    public bool isAnalyzing = false;
    
    [Header("웹소켓 설정")]
    public string websocketUrl = "ws://192.168.12.79:8080"; // 웹소켓 서버 주소
    public bool enableWebSocketSending = true; // 웹소켓 전송 활성화
    public bool maintainConnection = true; // 웹소켓 연결 유지
    public bool autoReconnect = true; // 자동 재연결
    public float reconnectDelay = 5f; // 재연결 대기 시간
    
    [Header("마이크 설정")]
    public AudioSource audioSource;
    public string microphoneDevice = null; // null이면 기본 마이크 사용
    public int sampleRate = 44100;
    public int recordingLength = 10; // 녹음 길이 (초) - 10초마다 전송
    
    [Header("분석 결과")]
    public List<AnalysisResult> analysisHistory = new List<AnalysisResult>();
    
    [Header("이벤트")]
    public System.Action<AnalysisResult> OnAnalysisCompleted; // 분석 완료 이벤트
    public System.Action<VoiceAnalysisData> OnVoiceDataReceived; // 음성 데이터 수신 이벤트
    
    private AudioClip microphoneClip;
    private AudioClip recordingClip; // 15초 녹음용 클립
    private float[] audioSamples;
    private List<float> recordedSamples = new List<float>(); // 녹음된 샘플 저장
    private bool isMicrophoneActive = false;
    private Coroutine analysisCoroutine;
    private Coroutine recordingCoroutine;
    
    // 웹소켓 관련
    private bool isWebSocketConnected = false;
    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationTokenSource;
    private Task webSocketTask;
    
    void Start()
    {
        // 오디오 소스 설정
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 마이크 권한 확인
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            InitializeMicrophone();
        }
        else
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
    }
    
    void Update()
    {
        if (isMicrophoneActive && isAnalyzing)
        {
            ProcessAudioInput();
        }
        
        // 웹소켓 연결 상태 모니터링
        if (enableWebSocketSending && isAnalyzing)
        {
            MonitorWebSocketConnection();
        }
    }
    
    /// <summary>
    /// 웹소켓 연결 상태 모니터링
    /// </summary>
    private void MonitorWebSocketConnection()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Closed && isWebSocketConnected)
        {
            Debug.LogWarning("웹소켓 연결이 예기치 않게 종료되었습니다.");
            isWebSocketConnected = false;
            
            // 자동 재연결 시도
            if (autoReconnect)
            {
                StartCoroutine(ReconnectWebSocket());
            }
        }
    }
    
    /// <summary>
    /// 마이크 초기화
    /// </summary>
    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"마이크 초기화: {microphoneDevice}");
        }
        else
        {
            Debug.LogWarning("사용 가능한 마이크가 없습니다.");
        }
    }
    
    /// <summary>
    /// 음성 분석 시작
    /// </summary>
    public void StartAnalysis()
    {
        if (isAnalyzing) return;
        
        isAnalyzing = true;
        StartMicrophoneRecording();
        
        // 분석 코루틴 시작
        if (analysisCoroutine != null)
        {
            StopCoroutine(analysisCoroutine);
        }
        analysisCoroutine = StartCoroutine(AnalysisLoop());
        
        // 웹소켓 녹음 코루틴 시작
        if (enableWebSocketSending)
        {
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
            }
            recordingCoroutine = StartCoroutine(WebSocketRecordingLoop());
            ConnectWebSocket();
        }
        
        Debug.Log("음성 분석 및 웹소켓 녹음 시작");
    }
    
    /// <summary>
    /// 음성 분석 중지
    /// </summary>
    public void StopAnalysis()
    {
        if (!isAnalyzing) return;
        
        isAnalyzing = false;
        StopMicrophoneRecording();
        
        if (analysisCoroutine != null)
        {
            StopCoroutine(analysisCoroutine);
            analysisCoroutine = null;
        }
        
        // 웹소켓 녹음 중지
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
            recordingCoroutine = null;
        }
        
        DisconnectWebSocket();
        
        Debug.Log("음성 분석 및 웹소켓 녹음 중지");
    }
    
    /// <summary>
    /// 마이크 녹음 시작
    /// </summary>
    private void StartMicrophoneRecording()
    {
        if (isMicrophoneActive) return;
        
        microphoneClip = Microphone.Start(microphoneDevice, true, recordingLength * 2, sampleRate);
        isMicrophoneActive = true;
        
        Debug.Log("마이크 녹음 시작");
    }
    
    /// <summary>
    /// 마이크 녹음 중지
    /// </summary>
    private void StopMicrophoneRecording()
    {
        if (!isMicrophoneActive) return;
        
        Microphone.End(microphoneDevice);
        isMicrophoneActive = false;
        
        Debug.Log("마이크 녹음 중지");
    }
    
    /// <summary>
    /// 오디오 입력 처리
    /// </summary>
    private void ProcessAudioInput()
    {
        if (microphoneClip == null) return;
        
        // 오디오 샘플 가져오기
        audioSamples = new float[microphoneClip.samples];
        microphoneClip.GetData(audioSamples, 0);
        
        // 볼륨 계산
        float volume = CalculateVolume(audioSamples);
        
        // 음성 활동 감지
        if (volume > minVolumeThreshold)
        {
            // 음성 데이터 생성
            VoiceAnalysisData voiceData = new VoiceAnalysisData
            {
                volume = volume,
                timestamp = DateTime.Now
            };
            
            OnVoiceDataReceived?.Invoke(voiceData);
        }
    }
    
    /// <summary>
    /// 볼륨 계산
    /// </summary>
    /// <param name="samples">오디오 샘플</param>
    /// <returns>볼륨 값 (0-1)</returns>
    private float CalculateVolume(float[] samples)
    {
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        return sum / samples.Length;
    }
    
    /// <summary>
    /// 분석 루프 코루틴
    /// </summary>
    private IEnumerator AnalysisLoop()
    {
        while (isAnalyzing)
        {
            yield return new WaitForSeconds(analysisInterval);
            
            if (isAnalyzing)
            {
                PerformAnalysis();
            }
        }
    }
    
    /// <summary>
    /// 음성 분석 수행
    /// </summary>
    private void PerformAnalysis()
    {
        // 실제 AI 분석 모델 호출 대신 임시 분석 결과 생성
        VoiceAnalysisData analysisData = GenerateAnalysisData();
        AnalysisResult result = GenerateFeedback(analysisData);
        
        // 분석 결과 저장
        analysisHistory.Add(result);
        
        // 이벤트 발생
        OnAnalysisCompleted?.Invoke(result);
        
        Debug.Log($"분석 완료: {result.feedback} (점수: {result.overallScore})");
    }
    
    /// <summary>
    /// 분석 데이터 생성 (임시)
    /// </summary>
    /// <returns>분석 데이터</returns>
    private VoiceAnalysisData GenerateAnalysisData()
    {
        // 실제 구현에서는 AI 모델을 통해 분석
        return new VoiceAnalysisData
        {
            confidence = UnityEngine.Random.Range(0.3f, 0.9f),
            speechRate = UnityEngine.Random.Range(120f, 180f),
            volume = UnityEngine.Random.Range(0.4f, 0.8f),
            clarity = UnityEngine.Random.Range(0.6f, 0.95f),
            timestamp = DateTime.Now
        };
    }
    
    /// <summary>
    /// 피드백 생성
    /// </summary>
    /// <param name="data">분석 데이터</param>
    /// <returns>분석 결과</returns>
    private AnalysisResult GenerateFeedback(VoiceAnalysisData data)
    {
        AnalysisResult result = new AnalysisResult
        {
            analysisData = data
        };
        
        // 전체 점수 계산
        result.overallScore = (data.confidence + data.volume + data.clarity) * 100f / 3f;
        
        // 피드백 메시지 생성
        if (result.overallScore >= 80f)
        {
            result.feedback = "훌륭합니다! 자신감 있게 발표하고 있습니다.";
            result.feedbackColor = Color.green;
        }
        else if (result.overallScore >= 60f)
        {
            result.feedback = "좋습니다! 조금 더 크고 명확하게 말해보세요.";
            result.feedbackColor = Color.yellow;
        }
        else
        {
            result.feedback = "더 자신감 있게 발표해보세요. 목소리를 크게 내어보세요.";
            result.feedbackColor = Color.red;
        }
        
        return result;
    }
    
    /// <summary>
    /// 분석 기록 초기화
    /// </summary>
    public void ClearAnalysisHistory()
    {
        analysisHistory.Clear();
    }
    
    /// <summary>
    /// 최종 분석 결과 반환
    /// </summary>
    /// <returns>최종 분석 결과</returns>
    public AnalysisResult GetFinalAnalysisResult()
    {
        if (analysisHistory.Count == 0) return null;
        
        // 모든 분석 결과의 평균 계산
        float totalScore = 0f;
        foreach (var result in analysisHistory)
        {
            totalScore += result.overallScore;
        }
        
        float averageScore = totalScore / analysisHistory.Count;
        
        // 최종 분석 결과 생성
        AnalysisResult finalResult = new AnalysisResult
        {
            overallScore = averageScore,
            feedback = $"발표 완료! 평균 점수: {averageScore:F1}점",
            feedbackColor = averageScore >= 80f ? Color.green : 
                           averageScore >= 60f ? Color.yellow : Color.red,
            analysisData = new VoiceAnalysisData
            {
                timestamp = DateTime.Now
            }
        };
        
        return finalResult;
    }
    
    #region 웹소켓 및 음성 전송 기능
    
    /// <summary>
    /// 웹소켓 연결
    /// </summary>
    private void ConnectWebSocket()
    {
        if (!enableWebSocketSending) return;
        
        Debug.Log($"웹소켓 서버 연결 시도: {websocketUrl}");
        StartCoroutine(ConnectWebSocketCoroutine());
    }
    
    /// <summary>
    /// 웹소켓 연결 코루틴
    /// </summary>
    private IEnumerator ConnectWebSocketCoroutine()
    {
        webSocket = new ClientWebSocket();
        cancellationTokenSource = new CancellationTokenSource();
        
        Uri serverUri = null;
        bool hasError = false;
        
        // URI 파싱 시도
        if (!Uri.TryCreate(websocketUrl, UriKind.Absolute, out serverUri))
        {
            Debug.LogError($"잘못된 웹소켓 URL: {websocketUrl}");
            isWebSocketConnected = false;
            yield break;
        }
        
        // 웹소켓 연결 시도
        webSocketTask = webSocket.ConnectAsync(serverUri, cancellationTokenSource.Token);
        
        // 연결 완료까지 대기
        while (!webSocketTask.IsCompleted)
        {
            yield return null;
        }
        
        // 연결 결과 확인
        if (webSocketTask.Exception != null)
        {
            Debug.LogError($"웹소켓 연결 오류: {webSocketTask.Exception.Message}");
            hasError = true;
        }
        
        if (!hasError && webSocket.State == WebSocketState.Open)
        {
            isWebSocketConnected = true;
            Debug.Log("웹소켓 연결 성공!");
            
            // 메시지 수신 시작
            StartCoroutine(ReceiveMessages());
        }
        else
        {
            Debug.LogError($"웹소켓 연결 실패: {webSocket.State}");
            isWebSocketConnected = false;
        }
    }
    
    /// <summary>
    /// 웹소켓 메시지 수신 코루틴
    /// </summary>
    private IEnumerator ReceiveMessages()
    {
        byte[] buffer = new byte[1024 * 4];
        
        while (isWebSocketConnected && webSocket.State == WebSocketState.Open)
        {
            var receiveTask = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
            
            // 메시지 수신 완료까지 대기
            while (!receiveTask.IsCompleted)
            {
                yield return null;
            }
            
            // 예외 확인
            if (receiveTask.Exception != null)
            {
                Debug.LogError($"웹소켓 메시지 수신 오류: {receiveTask.Exception.Message}");
                isWebSocketConnected = false;
                
                // 자동 재연결 시도
                if (autoReconnect && enableWebSocketSending)
                {
                    StartCoroutine(ReconnectWebSocket());
                }
                break;
            }
            
            WebSocketReceiveResult result = receiveTask.Result;
            
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"웹소켓 응답 수신: {message}");
                
                // 메인 스레드에서 응답 처리
                ProcessServerResponse(message);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Debug.Log("웹소켓 연결 종료");
                isWebSocketConnected = false;
                
                // 자동 재연결 시도
                if (autoReconnect && enableWebSocketSending)
                {
                    StartCoroutine(ReconnectWebSocket());
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 웹소켓 연결 해제
    /// </summary>
    private void DisconnectWebSocket()
    {
        isWebSocketConnected = false;
        
        try
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
            
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "연결 종료", CancellationToken.None);
            }
            
            webSocket?.Dispose();
            cancellationTokenSource?.Dispose();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"웹소켓 연결 해제 오류: {e.Message}");
        }
        
        Debug.Log("웹소켓 연결 해제");
    }
    
    /// <summary>
    /// 웹소켓 자동 재연결
    /// </summary>
    private IEnumerator ReconnectWebSocket()
    {
        Debug.Log($"웹소켓 재연결 시도... {reconnectDelay}초 후");
        
        // 재연결 대기
        yield return new WaitForSeconds(reconnectDelay);
        
        // 분석 중일 때만 재연결
        if (isAnalyzing && enableWebSocketSending)
        {
            Debug.Log("웹소켓 재연결 시도");
            ConnectWebSocket();
        }
    }
    
    /// <summary>
    /// 웹소켓 연결 상태 확인
    /// </summary>
    public bool IsWebSocketConnected()
    {
        return isWebSocketConnected && webSocket != null && webSocket.State == WebSocketState.Open;
    }
    
    /// <summary>
    /// 10초마다 음성 녹음 및 전송 코루틴
    /// </summary>
    private IEnumerator WebSocketRecordingLoop()
    {
        while (isAnalyzing && enableWebSocketSending)
        {
            yield return StartCoroutine(RecordAndSendAudio());
            yield return new WaitForSeconds(analysisInterval);
        }
    }
    
    /// <summary>
    /// 음성 녹음 및 전송
    /// </summary>
    private IEnumerator RecordAndSendAudio()
    {
        if (!isMicrophoneActive || microphoneClip == null) yield break;
        
        Debug.Log("10초 음성 녹음 시작...");
        
        // 현재 마이크 위치 저장
        int startPosition = Microphone.GetPosition(microphoneDevice);
        
        // 10초 대기
        yield return new WaitForSeconds(recordingLength);
        
        // 녹음 완료 후 현재 위치
        int endPosition = Microphone.GetPosition(microphoneDevice);
        
        // 샘플 데이터 추출
        float[] recordedData = ExtractAudioData(startPosition, endPosition);
        
        if (recordedData != null && recordedData.Length > 0)
        {
            // WAV 형식으로 변환
            byte[] wavData = ConvertToWav(recordedData, sampleRate);
            
            // 웹소켓으로 전송
            yield return StartCoroutine(SendAudioToWebSocket(wavData));
        }
    }
    
    /// <summary>
    /// 마이크에서 오디오 데이터 추출
    /// </summary>
    private float[] ExtractAudioData(int startPos, int endPos)
    {
        if (microphoneClip == null) return null;
        
        int sampleCount = recordingLength * sampleRate;
        float[] audioData = new float[sampleCount];
        
        // 마이크 클립에서 데이터 가져오기
        if (endPos > startPos)
        {
            // 일반적인 경우
            microphoneClip.GetData(audioData, startPos);
        }
        else
        {
            // 버퍼가 순환한 경우
            int firstPart = microphoneClip.samples - startPos;
            int secondPart = endPos;
            
            float[] firstData = new float[firstPart];
            float[] secondData = new float[secondPart];
            
            microphoneClip.GetData(firstData, startPos);
            microphoneClip.GetData(secondData, 0);
            
            // 두 부분 합치기
            Array.Copy(firstData, 0, audioData, 0, firstPart);
            Array.Copy(secondData, 0, audioData, firstPart, secondPart);
        }
        
        return audioData;
    }
    
    /// <summary>
    /// 오디오 데이터를 WAV 형식으로 변환
    /// </summary>
    private byte[] ConvertToWav(float[] audioData, int sampleRate)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            // WAV 헤더 작성
            WriteWavHeader(stream, audioData.Length, sampleRate);
            
            // 오디오 데이터를 16비트 PCM으로 변환
            foreach (float sample in audioData)
            {
                short pcmValue = (short)(sample * short.MaxValue);
                byte[] bytes = BitConverter.GetBytes(pcmValue);
                stream.Write(bytes, 0, 2);
            }
            
            return stream.ToArray();
        }
    }
    
    /// <summary>
    /// WAV 파일 헤더 작성
    /// </summary>
    private void WriteWavHeader(MemoryStream stream, int audioDataLength, int sampleRate)
    {
        int fileSize = 36 + audioDataLength * 2; // 16비트 스테레오
        
        // RIFF 헤더
        stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(fileSize), 0, 4);
        stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        
        // fmt 청크
        stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4); // 청크 크기
        stream.Write(BitConverter.GetBytes((short)1), 0, 2); // PCM 포맷
        stream.Write(BitConverter.GetBytes((short)1), 0, 2); // 모노 채널
        stream.Write(BitConverter.GetBytes(sampleRate), 0, 4); // 샘플 레이트
        stream.Write(BitConverter.GetBytes(sampleRate * 2), 0, 4); // 바이트 레이트
        stream.Write(BitConverter.GetBytes((short)2), 0, 2); // 블록 정렬
        stream.Write(BitConverter.GetBytes((short)16), 0, 2); // 비트 깊이
        
        // data 청크
        stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(audioDataLength * 2), 0, 4);
    }
    
    /// <summary>
    /// 웹소켓으로 오디오 데이터 전송
    /// </summary>
    private IEnumerator SendAudioToWebSocket(byte[] wavData)
    {
        if (!IsWebSocketConnected() || wavData == null) 
        {
            Debug.LogWarning("웹소켓이 연결되지 않았거나 데이터가 없습니다.");
            
            // 자동 재연결 시도
            if (autoReconnect && enableWebSocketSending && !isWebSocketConnected)
            {
                StartCoroutine(ReconnectWebSocket());
            }
            yield break;
        }
        
        Debug.Log($"음성 데이터 전송 중... 크기: {wavData.Length} bytes");
        
        // WAV 데이터를 Base64로 인코딩해서 JSON으로 전송
        string base64Audio = System.Convert.ToBase64String(wavData);
        
        // JSON 형태로 패키징
        AudioMessage audioMessage = new AudioMessage
        {
            type = "audio",
            format = "wav",
            sample_rate = sampleRate,
            duration = recordingLength,
            data = base64Audio,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        string jsonMessage = JsonUtility.ToJson(audioMessage);
        byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
        
        Debug.Log($"JSON 메시지 전송 중... 크기: {messageBytes.Length} bytes");
        
        // 텍스트 메시지로 웹소켓 전송
        ArraySegment<byte> buffer = new ArraySegment<byte>(messageBytes);
        var sendTask = webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
        
        // 전송 완료까지 대기
        while (!sendTask.IsCompleted)
        {
            yield return null;
        }
        
        // 전송 결과 확인
        if (sendTask.Exception != null)
        {
            Debug.LogError($"음성 데이터 전송 실패: {sendTask.Exception.Message}");
            isWebSocketConnected = false;
            
            // 자동 재연결 시도
            if (autoReconnect && enableWebSocketSending)
            {
                StartCoroutine(ReconnectWebSocket());
            }
        }
        else
        {
            Debug.Log("JSON 음성 데이터 전송 성공!");
            // 응답은 ReceiveMessages에서 자동으로 처리됨
        }
    }
    
    /// <summary>
    /// 서버 응답 처리
    /// </summary>
    private void ProcessServerResponse(string jsonResponse)
    {
        try
        {
            // JSON 파싱
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(jsonResponse);
            
            if (response != null)
            {
                Debug.Log($"서버 분석 결과 - WPM: {response.wpm}, 볼륨: {response.volume}, 명확도: {response.clarity_score}, STT시간: {response.stt_time}초, 지속시간: {response.duration}초");
                
                // VoiceAnalysisData 생성
                VoiceAnalysisData analysisData = new VoiceAnalysisData
                {
                    wpm = response.wpm,
                    volume = response.volume,
                    pitchVariation = response.pitch_variation,
                    clarityScore = response.clarity_score,
                    recognizedText = response.recognized_text,
                    sttTime = response.stt_time,
                    duration = response.duration,
                    timestamp = DateTime.Now,
                    
                    // 기존 데이터 변환
                    speechRate = response.wpm,
                    clarity = NormalizeClarity(response.clarity_score),
                    confidence = CalculateConfidence(response.wpm, response.volume, response.clarity_score)
                };
                
                // 인식된 텍스트를 단어로 분리
                if (!string.IsNullOrEmpty(response.recognized_text))
                {
                    analysisData.detectedWords = new List<string>(response.recognized_text.Split(' '));
                }
                
                // 분석 결과 생성
                AnalysisResult result = GenerateFeedbackFromServer(analysisData);
                analysisHistory.Add(result);
                
                // 이벤트 발생
                OnAnalysisCompleted?.Invoke(result);
                OnVoiceDataReceived?.Invoke(analysisData);
                
                Debug.Log($"실시간 분석 완료 - 점수: {result.overallScore:F1}점");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"서버 응답 파싱 실패: {e.Message}");
            Debug.LogError($"원본 응답: {jsonResponse}");
        }
    }
    
    /// <summary>
    /// 명확도 점수 정규화 (서버 값을 0-1 범위로 변환)
    /// </summary>
    private float NormalizeClarity(float clarityScore)
    {
        // 서버에서 오는 clarity_score는 음수일 수 있으므로 정규화
        // 일반적으로 -500 ~ 0 범위라고 가정
        float normalized = Mathf.Clamp01((clarityScore + 500f) / 500f);
        return normalized;
    }
    
    /// <summary>
    /// 자신감 수준 계산
    /// </summary>
    private float CalculateConfidence(float wpm, float volume, float clarityScore)
    {
        // WPM, 볼륨, 명확도를 종합해서 자신감 수준 계산
        float wpmScore = Mathf.Clamp01(wpm / 150f); // 150 WPM을 기준으로 정규화
        float volumeScore = Mathf.Clamp01(volume * 10f); // 볼륨을 증폭
        float clarityNorm = NormalizeClarity(clarityScore);
        
        return (wpmScore + volumeScore + clarityNorm) / 3f;
    }
    
    /// <summary>
    /// 서버 데이터 기반 피드백 생성
    /// </summary>
    private AnalysisResult GenerateFeedbackFromServer(VoiceAnalysisData data)
    {
        float score = 0f;
        string feedback = "";
        Color color = Color.white;
        
        // WPM 평가 (90-150이 적정)
        float wpmScore = 0f;
        if (data.wpm >= 90f && data.wpm <= 150f)
        {
            wpmScore = 100f;
        }
        else if (data.wpm < 90f)
        {
            wpmScore = Mathf.Lerp(50f, 100f, data.wpm / 90f);
        }
        else
        {
            wpmScore = Mathf.Lerp(100f, 60f, (data.wpm - 150f) / 50f);
        }
        
        // 볼륨 평가
        float volumeScore = Mathf.Clamp01(data.volume * 10f) * 100f;
        
        // 명확도 평가
        float clarityScore = data.clarity * 100f;
        
        // 전체 점수 계산
        score = (wpmScore + volumeScore + clarityScore) / 3f;
        
        // 피드백 메시지 생성
        if (score >= 80f)
        {
            feedback = "훌륭한 발표입니다! 👏";
            color = Color.green;
        }
        else if (score >= 60f)
        {
            feedback = "좋은 발표입니다! 💪";
            color = Color.yellow;
        }
        else
        {
            feedback = "조금 더 자신감을 갖고 말해보세요! 💡";
            color = Color.red;
        }
        
        return new AnalysisResult
        {
            overallScore = score,
            feedback = feedback,
            feedbackColor = color,
            analysisData = data
        };
    }
    
    /// <summary>
    /// 오브젝트 파괴 시 리소스 정리
    /// </summary>
    void OnDestroy()
    {
        // 분석 중지
        if (isAnalyzing)
        {
            StopAnalysis();
        }
        
        // 웹소켓 연결 해제
        DisconnectWebSocket();
        
        // 코루틴 정리
        if (analysisCoroutine != null)
        {
            StopCoroutine(analysisCoroutine);
        }
        
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        
        Debug.Log("VoiceAnalyzer 리소스 정리 완료");
    }
    
    #endregion
} 