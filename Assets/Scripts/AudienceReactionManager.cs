using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudienceReaction
{
    public string reactionType; // 반응 유형 (박수, 웃음, 기침 등)
    public AudioClip soundClip; // 반응 소리
    public GameObject vfxPrefab; // 반응 VFX 프리팹
    public float probability; // 발생 확률 (0-1)
    public float minScore; // 최소 점수 (이 점수 이상에서 발생)
    public float maxScore; // 최대 점수 (이 점수 이하에서 발생)
}

public class AudienceReactionManager : MonoBehaviour
{
    [Header("청중 반응 설정")]
    public List<AudienceReaction> reactions = new List<AudienceReaction>();
    public Transform[] audiencePositions; // 청중 위치들
    public float reactionDelay = 1f; // 반응 지연 시간
    
    [Header("오디오 설정")]
    public AudioSource audioSource;
    public float baseVolume = 0.7f;
    public float randomVolumeVariation = 0.2f;
    
    [Header("VFX 설정")]
    public Transform vfxParent; // VFX 부모 오브젝트
    public float vfxLifetime = 3f; // VFX 지속 시간
    
    [Header("반응 확률 설정")]
    public AnimationCurve scoreToReactionCurve; // 점수에 따른 반응 확률 곡선
    public float baseReactionChance = 0.3f; // 기본 반응 확률
    public float maxReactionChance = 0.8f; // 최대 반응 확률
    
    [Header("이벤트")]
    public System.Action<AudienceReaction> OnReactionTriggered; // 반응 발생 이벤트
    
    private VoiceAnalyzer voiceAnalyzer;
    private FeedbackManager feedbackManager;
    private Queue<AnalysisResult> reactionQueue = new Queue<AnalysisResult>();
    private List<GameObject> activeVFX = new List<GameObject>();
    
    void Start()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 음성 분석기 연결
        voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted += HandleAnalysisResult;
        }
        
        // 피드백 매니저 연결
        feedbackManager = FindObjectOfType<FeedbackManager>();
        if (feedbackManager != null)
        {
            feedbackManager.OnFeedbackDisplayed += HandleFeedbackDisplayed;
        }
        
        // 기본 반응 설정
        SetupDefaultReactions();
    }
    
    void Update()
    {
        // 큐에 대기 중인 반응 처리
        if (reactionQueue.Count > 0)
        {
            AnalysisResult result = reactionQueue.Dequeue();
            StartCoroutine(ProcessReaction(result));
        }
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 오디오 소스 설정
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // VFX 부모 설정
        if (vfxParent == null)
        {
            vfxParent = transform;
        }
        
        // 점수-반응 곡선 초기화
        if (scoreToReactionCurve == null || scoreToReactionCurve.keys.Length == 0)
        {
            scoreToReactionCurve = AnimationCurve.EaseInOut(0f, 0f, 100f, 1f);
        }
    }
    
    /// <summary>
    /// 기본 반응 설정
    /// </summary>
    private void SetupDefaultReactions()
    {
        if (reactions.Count > 0) return;
        
        // 기본 반응 추가 (실제 오디오 클립과 VFX는 따로 설정 필요)
        reactions.Add(new AudienceReaction
        {
            reactionType = "박수",
            probability = 0.7f,
            minScore = 70f,
            maxScore = 100f
        });
        
        reactions.Add(new AudienceReaction
        {
            reactionType = "웃음",
            probability = 0.5f,
            minScore = 60f,
            maxScore = 90f
        });
        
        reactions.Add(new AudienceReaction
        {
            reactionType = "기침",
            probability = 0.3f,
            minScore = 0f,
            maxScore = 50f
        });
        
        reactions.Add(new AudienceReaction
        {
            reactionType = "속삭임",
            probability = 0.4f,
            minScore = 20f,
            maxScore = 60f
        });
    }
    
    /// <summary>
    /// 분석 결과 처리
    /// </summary>
    /// <param name="result">분석 결과</param>
    private void HandleAnalysisResult(AnalysisResult result)
    {
        // 반응 큐에 추가
        reactionQueue.Enqueue(result);
    }
    
    /// <summary>
    /// 피드백 표시 처리
    /// </summary>
    /// <param name="result">분석 결과</param>
    private void HandleFeedbackDisplayed(AnalysisResult result)
    {
        // 추가 반응 처리 (필요한 경우)
        Debug.Log($"피드백 표시됨: {result.feedback}");
    }
    
    /// <summary>
    /// 반응 처리 코루틴
    /// </summary>
    /// <param name="result">분석 결과</param>
    private IEnumerator ProcessReaction(AnalysisResult result)
    {
        // 반응 지연
        yield return new WaitForSeconds(reactionDelay);
        
        // 점수에 따른 반응 확률 계산
        float reactionChance = CalculateReactionChance(result.overallScore);
        
        // 반응 발생 여부 결정
        if (Random.Range(0f, 1f) <= reactionChance)
        {
            // 적절한 반응 선택
            AudienceReaction selectedReaction = SelectReaction(result.overallScore);
            
            if (selectedReaction != null)
            {
                // 반응 실행
                TriggerReaction(selectedReaction);
            }
        }
    }
    
    /// <summary>
    /// 반응 확률 계산
    /// </summary>
    /// <param name="score">점수</param>
    /// <returns>반응 확률</returns>
    private float CalculateReactionChance(float score)
    {
        float normalizedScore = score / 100f;
        float curveValue = scoreToReactionCurve.Evaluate(score);
        
        return Mathf.Lerp(baseReactionChance, maxReactionChance, curveValue);
    }
    
    /// <summary>
    /// 반응 선택
    /// </summary>
    /// <param name="score">점수</param>
    /// <returns>선택된 반응</returns>
    private AudienceReaction SelectReaction(float score)
    {
        List<AudienceReaction> validReactions = new List<AudienceReaction>();
        
        // 점수 범위에 맞는 반응 찾기
        foreach (var reaction in reactions)
        {
            if (score >= reaction.minScore && score <= reaction.maxScore)
            {
                validReactions.Add(reaction);
            }
        }
        
        if (validReactions.Count == 0) return null;
        
        // 확률에 따라 반응 선택
        float totalProbability = 0f;
        foreach (var reaction in validReactions)
        {
            totalProbability += reaction.probability;
        }
        
        float randomValue = Random.Range(0f, totalProbability);
        float currentProbability = 0f;
        
        foreach (var reaction in validReactions)
        {
            currentProbability += reaction.probability;
            if (randomValue <= currentProbability)
            {
                return reaction;
            }
        }
        
        return validReactions[0]; // 기본값
    }
    
    /// <summary>
    /// 반응 실행
    /// </summary>
    /// <param name="reaction">반응</param>
    private void TriggerReaction(AudienceReaction reaction)
    {
        // 소리 재생
        if (reaction.soundClip != null && audioSource != null)
        {
            PlayReactionSound(reaction.soundClip);
        }
        
        // VFX 생성
        if (reaction.vfxPrefab != null)
        {
            CreateReactionVFX(reaction.vfxPrefab);
        }
        
        // 이벤트 발생
        OnReactionTriggered?.Invoke(reaction);
        
        Debug.Log($"청중 반응 발생: {reaction.reactionType}");
    }
    
    /// <summary>
    /// 반응 소리 재생
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    private void PlayReactionSound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        
        // 볼륨 변화 적용
        float volume = baseVolume + Random.Range(-randomVolumeVariation, randomVolumeVariation);
        volume = Mathf.Clamp01(volume);
        
        audioSource.PlayOneShot(clip, volume);
    }
    
    /// <summary>
    /// 반응 VFX 생성
    /// </summary>
    /// <param name="vfxPrefab">VFX 프리팹</param>
    private void CreateReactionVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null) return;
        
        // 랜덤 위치 선택
        Vector3 spawnPosition = GetRandomAudiencePosition();
        
        // VFX 생성
        GameObject vfx = Instantiate(vfxPrefab, spawnPosition, Quaternion.identity, vfxParent);
        activeVFX.Add(vfx);
        
        // 자동 제거
        StartCoroutine(RemoveVFXAfterDelay(vfx, vfxLifetime));
    }
    
    /// <summary>
    /// 랜덤 청중 위치 반환
    /// </summary>
    /// <returns>청중 위치</returns>
    private Vector3 GetRandomAudiencePosition()
    {
        if (audiencePositions == null || audiencePositions.Length == 0)
        {
            return transform.position + Random.insideUnitSphere * 3f;
        }
        
        int randomIndex = Random.Range(0, audiencePositions.Length);
        return audiencePositions[randomIndex].position;
    }
    
    /// <summary>
    /// VFX 지연 제거 코루틴
    /// </summary>
    /// <param name="vfx">VFX 오브젝트</param>
    /// <param name="delay">지연 시간</param>
    private IEnumerator RemoveVFXAfterDelay(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (vfx != null)
        {
            activeVFX.Remove(vfx);
            Destroy(vfx);
        }
    }
    
    /// <summary>
    /// 수동 반응 실행
    /// </summary>
    /// <param name="reactionType">반응 유형</param>
    public void TriggerManualReaction(string reactionType)
    {
        AudienceReaction reaction = reactions.Find(r => r.reactionType == reactionType);
        if (reaction != null)
        {
            TriggerReaction(reaction);
        }
    }
    
    /// <summary>
    /// 모든 VFX 제거
    /// </summary>
    public void ClearAllVFX()
    {
        foreach (var vfx in activeVFX)
        {
            if (vfx != null)
            {
                Destroy(vfx);
            }
        }
        activeVFX.Clear();
    }
    
    /// <summary>
    /// 반응 큐 초기화
    /// </summary>
    public void ClearReactionQueue()
    {
        reactionQueue.Clear();
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnAnalysisCompleted -= HandleAnalysisResult;
        }
        
        if (feedbackManager != null)
        {
            feedbackManager.OnFeedbackDisplayed -= HandleFeedbackDisplayed;
        }
        
        // 모든 VFX 제거
        ClearAllVFX();
    }
} 