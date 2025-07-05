using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class VoiceAnalysisData
{
    public float confidence; // 자신감 수준 (0-1)
    public float speechRate; // 말하기 속도 (분당 단어 수)
    public float volume; // 음성 볼륨 (0-1)
    public float clarity; // 발음 명확도 (0-1)
    public List<string> detectedWords = new List<string>(); // 감지된 단어들
    public DateTime timestamp; // 분석 시간
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
    public float analysisInterval = 15f; // 분석 간격 (초)
    public float minVolumeThreshold = 0.01f; // 최소 음성 볼륨 임계값
    public bool isAnalyzing = false;
    
    [Header("마이크 설정")]
    public AudioSource audioSource;
    public string microphoneDevice = null; // null이면 기본 마이크 사용
    public int sampleRate = 44100;
    public int clipLength = 10; // 녹음 클립 길이 (초)
    
    [Header("분석 결과")]
    public List<AnalysisResult> analysisHistory = new List<AnalysisResult>();
    
    [Header("이벤트")]
    public System.Action<AnalysisResult> OnAnalysisCompleted; // 분석 완료 이벤트
    public System.Action<VoiceAnalysisData> OnVoiceDataReceived; // 음성 데이터 수신 이벤트
    
    private AudioClip microphoneClip;
    private float[] audioSamples;
    private bool isMicrophoneActive = false;
    private Coroutine analysisCoroutine;
    
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
        
        Debug.Log("음성 분석 시작");
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
        
        Debug.Log("음성 분석 중지");
    }
    
    /// <summary>
    /// 마이크 녹음 시작
    /// </summary>
    private void StartMicrophoneRecording()
    {
        if (isMicrophoneActive) return;
        
        microphoneClip = Microphone.Start(microphoneDevice, true, clipLength, sampleRate);
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
    /// <returns>전체 분석 결과</returns>
    public AnalysisResult GetFinalAnalysisResult()
    {
        if (analysisHistory.Count == 0) return null;
        
        float avgScore = 0f;
        float avgConfidence = 0f;
        float avgVolume = 0f;
        float avgClarity = 0f;
        
        foreach (var analysis in analysisHistory)
        {
            avgScore += analysis.overallScore;
            avgConfidence += analysis.analysisData.confidence;
            avgVolume += analysis.analysisData.volume;
            avgClarity += analysis.analysisData.clarity;
        }
        
        int count = analysisHistory.Count;
        avgScore /= count;
        avgConfidence /= count;
        avgVolume /= count;
        avgClarity /= count;
        
        return new AnalysisResult
        {
            overallScore = avgScore,
            feedback = $"전체 평균 점수: {avgScore:F1}점",
            feedbackColor = avgScore >= 80f ? Color.green : avgScore >= 60f ? Color.yellow : Color.red,
            analysisData = new VoiceAnalysisData
            {
                confidence = avgConfidence,
                volume = avgVolume,
                clarity = avgClarity,
                timestamp = DateTime.Now
            }
        };
    }
} 