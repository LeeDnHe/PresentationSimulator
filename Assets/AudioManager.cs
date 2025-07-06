using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("에코 효과 설정")]
    [Range(0f, 1f)]
    public float echoVolume = 0.3f; // 에코 볼륨 (0~1)
    [Range(0f, 1f)]
    public float delay = 0.1f; // 에코 지연 시간 (초)
    [Range(0f, 0.9f)]
    public float decay = 0.5f; // 에코 감쇠율
    [Range(0f, 1f)]
    public float dryMix = 0.8f; // 원본 소리 비율
    [Range(0f, 1f)]
    public float wetMix = 0.4f; // 에코 소리 비율
    
    [Header("볼륨 설정")]
    [Range(0f, 1f)]
    public float masterVolume = 0.7f; // 마스터 볼륨
    [Range(0f, 1f)]
    public float microphoneVolume = 0.6f; // 마이크 볼륨
    
    [Header("마이크 에코 활성화")]
    public bool enableMicrophoneEcho = true; // 마이크 에코 활성화
    
    [Header("오디오 컴포넌트")]
    public AudioSource audioSource; // 오디오 출력용
    public AudioEchoFilter echoFilter; // 에코 필터
    
    private VoiceAnalyzer voiceAnalyzer; // VoiceAnalyzer 참조
    private bool isMicrophoneEchoActive = false;
    private AudioClip echoClip; // 에코용 오디오 클립
    private float[] microphoneBuffer; // 마이크 데이터 버퍼
    private int bufferSize = 1024; // 버퍼 크기
    private int sampleRate = 44100; // 샘플 레이트
    private int echoSamples; // 에코 지연 샘플 수
    private float[] echoBuffer; // 에코 버퍼
    private int echoBufferIndex = 0;
    
    void Start()
    {
        Debug.Log("🎤 AudioManager 초기화 시작");
        
        // VoiceAnalyzer 찾기
        voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();
        if (voiceAnalyzer == null)
        {
            Debug.LogWarning("⚠️ VoiceAnalyzer를 찾을 수 없습니다. 마이크 에코가 작동하지 않을 수 있습니다.");
        }
        
        // 오디오 컴포넌트 초기화
        InitializeAudioComponents();
        
        // 마이크 에코 시작
        if (enableMicrophoneEcho)
        {
            StartMicrophoneEcho();
        }
    }
    
    void Update()
    {
        // 마이크 에코 활성화 상태에서 VoiceAnalyzer 마이크 데이터 처리
        if (isMicrophoneEchoActive && voiceAnalyzer != null && voiceAnalyzer.isAnalyzing)
        {
            ProcessMicrophoneEcho();
        }
    }
    
    /// <summary>
    /// 오디오 컴포넌트 초기화
    /// </summary>
    private void InitializeAudioComponents()
    {
        // AudioSource 컴포넌트 확인/생성
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // AudioEchoFilter 컴포넌트 확인/생성
        if (echoFilter == null)
        {
            echoFilter = GetComponent<AudioEchoFilter>();
            if (echoFilter == null)
            {
                echoFilter = gameObject.AddComponent<AudioEchoFilter>();
            }
        }
        
        // AudioSource 기본 설정
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = masterVolume * microphoneVolume;
        
        // 에코 필터 설정
        ApplyEchoSettings();
        
        // 에코 버퍼 초기화
        InitializeEchoBuffer();
        
        Debug.Log("✅ 오디오 컴포넌트 초기화 완료");
    }
    
    /// <summary>
    /// 에코 버퍼 초기화
    /// </summary>
    private void InitializeEchoBuffer()
    {
        echoSamples = Mathf.RoundToInt(delay * sampleRate);
        echoBuffer = new float[echoSamples];
        microphoneBuffer = new float[bufferSize];
        
        // 에코용 오디오 클립 생성
        echoClip = AudioClip.Create("EchoClip", bufferSize, 1, sampleRate, false);
        
        Debug.Log($"🔄 에코 버퍼 초기화: {echoSamples} 샘플, {delay:F2}초 지연");
    }
    
    /// <summary>
    /// 마이크 에코 시작
    /// </summary>
    public void StartMicrophoneEcho()
    {
        if (isMicrophoneEchoActive)
        {
            Debug.LogWarning("⚠️ 마이크 에코가 이미 활성화되어 있습니다.");
            return;
        }
        
        if (voiceAnalyzer == null)
        {
            Debug.LogError("❌ VoiceAnalyzer가 없어서 에코를 시작할 수 없습니다!");
            return;
        }
        
        isMicrophoneEchoActive = true;
        
        // 오디오 소스 설정
        audioSource.clip = echoClip;
        audioSource.Play();
        
        Debug.Log("🎤 마이크 에코 시작됨 (VoiceAnalyzer 연동)");
    }
    
    /// <summary>
    /// 마이크 에코 중지
    /// </summary>
    public void StopMicrophoneEcho()
    {
        if (!isMicrophoneEchoActive)
        {
            Debug.LogWarning("⚠️ 마이크 에코가 이미 비활성화되어 있습니다.");
            return;
        }
        
        isMicrophoneEchoActive = false;
        
        // 오디오 소스 중지
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        // 에코 버퍼 초기화
        if (echoBuffer != null)
        {
            for (int i = 0; i < echoBuffer.Length; i++)
            {
                echoBuffer[i] = 0f;
            }
            echoBufferIndex = 0;
        }
        
        Debug.Log("🎤 마이크 에코 중지됨");
    }
    
    /// <summary>
    /// 마이크 에코 처리 (VoiceAnalyzer 마이크 데이터 사용)
    /// </summary>
    private void ProcessMicrophoneEcho()
    {
        if (voiceAnalyzer.audioSource == null || voiceAnalyzer.audioSource.clip == null)
        {
            return;
        }
        
        // VoiceAnalyzer의 마이크 데이터 가져오기
        float[] micData = GetMicrophoneDataFromVoiceAnalyzer();
        
        if (micData != null && micData.Length > 0)
        {
            // 에코 처리
            float[] processedData = ProcessEchoEffect(micData);
            
            // 오디오 클립에 데이터 설정
            if (echoClip != null)
            {
                echoClip.SetData(processedData, 0);
            }
        }
    }
    
    /// <summary>
    /// VoiceAnalyzer에서 마이크 데이터 가져오기
    /// </summary>
    /// <returns>마이크 오디오 데이터</returns>
    private float[] GetMicrophoneDataFromVoiceAnalyzer()
    {
        if (voiceAnalyzer.audioSource.clip == null) return null;
        
        AudioClip micClip = voiceAnalyzer.audioSource.clip;
        float[] data = new float[bufferSize];
        
        // 현재 마이크 포지션 가져오기
        int micPosition = Microphone.GetPosition(voiceAnalyzer.microphoneDevice);
        
        if (micPosition > 0)
        {
            // 최근 데이터 가져오기
            int startPos = Mathf.Max(0, micPosition - bufferSize);
            micClip.GetData(data, startPos);
        }
        
        return data;
    }
    
    /// <summary>
    /// 에코 효과 처리
    /// </summary>
    /// <param name="inputData">입력 오디오 데이터</param>
    /// <returns>에코 처리된 오디오 데이터</returns>
    private float[] ProcessEchoEffect(float[] inputData)
    {
        float[] outputData = new float[inputData.Length];
        
        for (int i = 0; i < inputData.Length; i++)
        {
            // 현재 입력 샘플
            float inputSample = inputData[i];
            
            // 에코 버퍼에서 지연된 샘플 가져오기
            float echoSample = echoBuffer[echoBufferIndex];
            
            // 출력 = 드라이 믹스 + 웨트 믹스
            outputData[i] = (inputSample * dryMix) + (echoSample * wetMix);
            
            // 에코 버퍼 업데이트 (입력 + 피드백)
            echoBuffer[echoBufferIndex] = inputSample + (echoSample * decay);
            
            // 에코 버퍼 인덱스 업데이트
            echoBufferIndex = (echoBufferIndex + 1) % echoSamples;
        }
        
        return outputData;
    }
    
    /// <summary>
    /// 에코 설정 적용
    /// </summary>
    private void ApplyEchoSettings()
    {
        if (echoFilter != null)
        {
            echoFilter.delay = delay * 1000f; // 밀리초 단위로 변환
            echoFilter.decayRatio = decay;
            echoFilter.dryMix = dryMix;
            echoFilter.wetMix = wetMix;
        }
        
        // 에코 버퍼 재초기화 (지연 시간 변경 시)
        if (echoBuffer != null)
        {
            InitializeEchoBuffer();
        }
    }
    
    /// <summary>
    /// 에코 효과 토글
    /// </summary>
    public void ToggleMicrophoneEcho()
    {
        if (isMicrophoneEchoActive)
        {
            StopMicrophoneEcho();
        }
        else
        {
            StartMicrophoneEcho();
        }
    }
    
    /// <summary>
    /// 마이크 볼륨 설정
    /// </summary>
    /// <param name="volume">볼륨 (0~1)</param>
    public void SetMicrophoneVolume(float volume)
    {
        microphoneVolume = Mathf.Clamp01(volume);
        
        if (audioSource != null)
        {
            audioSource.volume = masterVolume * microphoneVolume;
        }
        
        Debug.Log($"🎤 마이크 볼륨 설정: {microphoneVolume:F2}");
    }
    
    /// <summary>
    /// 마스터 볼륨 설정
    /// </summary>
    /// <param name="volume">볼륨 (0~1)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        if (audioSource != null)
        {
            audioSource.volume = masterVolume * microphoneVolume;
        }
        
        Debug.Log($"🔊 마스터 볼륨 설정: {masterVolume:F2}");
    }
    
    /// <summary>
    /// 에코 지연 시간 설정
    /// </summary>
    /// <param name="delayTime">지연 시간 (초)</param>
    public void SetEchoDelay(float delayTime)
    {
        delay = Mathf.Clamp01(delayTime);
        ApplyEchoSettings();
        
        Debug.Log($"🔄 에코 지연 설정: {delay:F2}초");
    }
    
    /// <summary>
    /// 에코 감쇠율 설정
    /// </summary>
    /// <param name="decayRate">감쇠율 (0~0.9)</param>
    public void SetEchoDecay(float decayRate)
    {
        decay = Mathf.Clamp(decayRate, 0f, 0.9f);
        ApplyEchoSettings();
        
        Debug.Log($"📉 에코 감쇠 설정: {decay:F2}");
    }
    
    /// <summary>
    /// 마이크 상태 확인
    /// </summary>
    /// <returns>마이크 에코 활성화 상태</returns>
    public bool IsMicrophoneEchoActive()
    {
        return isMicrophoneEchoActive;
    }
    
    /// <summary>
    /// VoiceAnalyzer 연결 확인
    /// </summary>
    /// <returns>VoiceAnalyzer 연결 상태</returns>
    public bool IsVoiceAnalyzerConnected()
    {
        return voiceAnalyzer != null;
    }
    
    void OnDestroy()
    {
        // 마이크 에코 중지
        if (isMicrophoneEchoActive)
        {
            StopMicrophoneEcho();
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // 앱이 일시정지되면 마이크 에코 중지
        if (pauseStatus && isMicrophoneEchoActive)
        {
            StopMicrophoneEcho();
        }
    }
}
