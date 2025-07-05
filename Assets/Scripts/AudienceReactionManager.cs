using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudienceReactionManager : MonoBehaviour
{
    [Header("청중 애니메이션 설정")]
    public Animator[] audienceAnimators; // 청중 애니메이터들
    public string animationTrigger = "Animation_On"; // 애니메이션 트리거 이름
    public float animationInterval = 15f; // 애니메이션 발동 간격 (초)
    public int animationCount = 2; // 한 번에 발동할 애니메이션 개수
    
    [Header("효과음 설정")]
    public AudioClip[] soundEffects; // 효과음 리스트
    public AudioSource audioSource;
    public float baseVolume = 0.7f;
    public float soundInterval = 30f; // 효과음 발동 간격 (초)
    public float soundProbability = 0.5f; // 효과음 발동 확률
    
    [Header("게임 제어")]
    public bool isActive = false; // 반응 시스템 활성화 상태
    
    private Coroutine animationCoroutine;
    private Coroutine soundCoroutine;
    private TransitionManager transitionManager;
    private List<int> usedAnimatorIndices = new List<int>(); // 사용된 애니메이터 인덱스
    
    void Start()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 전환 관리자 연결
        transitionManager = FindObjectOfType<TransitionManager>();
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart += StartAudienceReactions;
            transitionManager.OnPresentationEnd += StopAudienceReactions;
        }
        
        Debug.Log("🎭 AudienceReactionManager 초기화 완료");
    }
    
    void Update()
    {
        // 현재는 업데이트에서 처리할 내용이 없음
        // 모든 반응은 코루틴으로 처리
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
        
        // 애니메이터 배열 확인
        if (audienceAnimators == null || audienceAnimators.Length == 0)
        {
            Debug.LogWarning("⚠️ 청중 애니메이터가 설정되지 않았습니다!");
        }
        
        // 효과음 배열 확인
        if (soundEffects == null || soundEffects.Length == 0)
        {
            Debug.LogWarning("⚠️ 효과음이 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 청중 반응 시작 (게임 시작 시 호출)
    /// </summary>
    public void StartAudienceReactions()
    {
        if (isActive) return;
        
        isActive = true;
        
        // 애니메이션 코루틴 시작
        if (audienceAnimators != null && audienceAnimators.Length > 0)
        {
            animationCoroutine = StartCoroutine(AnimationCoroutine());
        }
        
        // 효과음 코루틴 시작
        if (soundEffects != null && soundEffects.Length > 0)
        {
            soundCoroutine = StartCoroutine(SoundCoroutine());
        }
        
        Debug.Log("🎭 청중 반응 시작!");
    }
    
    /// <summary>
    /// 청중 반응 정지 (게임 종료 시 호출)
    /// </summary>
    public void StopAudienceReactions()
    {
        if (!isActive) return;
        
        isActive = false;
        
        // 애니메이션 코루틴 중지
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        // 효과음 코루틴 중지
        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
            soundCoroutine = null;
        }
        
        Debug.Log("🎭 청중 반응 정지!");
    }
    
    /// <summary>
    /// 애니메이션 코루틴 (15초마다 실행)
    /// </summary>
    private IEnumerator AnimationCoroutine()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(animationInterval);
            
            if (isActive)
            {
                TriggerRandomAnimations();
            }
        }
    }
    
    /// <summary>
    /// 효과음 코루틴 (30초마다 실행)
    /// </summary>
    private IEnumerator SoundCoroutine()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(soundInterval);
            
            if (isActive)
            {
                // 확률에 따라 효과음 재생
                if (Random.Range(0f, 1f) <= soundProbability)
                {
                    TriggerRandomSound();
                }
            }
        }
    }
    
    /// <summary>
    /// 랜덤 애니메이션 발동 (2명 선택)
    /// </summary>
    private void TriggerRandomAnimations()
    {
        if (audienceAnimators == null || audienceAnimators.Length == 0) return;
        
        // 중복 방지를 위해 인덱스 리스트 초기화
        usedAnimatorIndices.Clear();
        
        // 발동할 애니메이션 개수 제한
        int targetCount = Mathf.Min(animationCount, audienceAnimators.Length);
        
        for (int i = 0; i < targetCount; i++)
        {
            int randomIndex = GetRandomAnimatorIndex();
            if (randomIndex != -1)
            {
                Animator animator = audienceAnimators[randomIndex];
                if (animator != null)
                {
                    animator.SetTrigger(animationTrigger);
                    Debug.Log($"🎭 애니메이션 발동: {animator.name}");
                }
                usedAnimatorIndices.Add(randomIndex);
            }
        }
    }
    
    /// <summary>
    /// 중복되지 않는 랜덤 애니메이터 인덱스 반환
    /// </summary>
    /// <returns>애니메이터 인덱스</returns>
    private int GetRandomAnimatorIndex()
    {
        // 사용 가능한 인덱스 리스트 생성
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < audienceAnimators.Length; i++)
        {
            if (!usedAnimatorIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }
        
        // 사용 가능한 인덱스가 없으면 -1 반환
        if (availableIndices.Count == 0) return -1;
        
        // 랜덤 인덱스 선택
        int randomIndex = Random.Range(0, availableIndices.Count);
        return availableIndices[randomIndex];
    }
    
    /// <summary>
    /// 랜덤 효과음 재생
    /// </summary>
    private void TriggerRandomSound()
    {
        if (soundEffects == null || soundEffects.Length == 0 || audioSource == null) return;
        
        // 랜덤 효과음 선택
        int randomIndex = Random.Range(0, soundEffects.Length);
        AudioClip selectedSound = soundEffects[randomIndex];
        
        if (selectedSound != null)
        {
            audioSource.PlayOneShot(selectedSound, baseVolume);
            Debug.Log($"🔊 효과음 재생: {selectedSound.name}");
        }
    }
    
    /// <summary>
    /// 반응 큐 초기화 (호환성 유지)
    /// </summary>
    public void ClearReactionQueue()
    {
        // 새로운 구조에서는 큐를 사용하지 않지만 호환성을 위해 유지
        Debug.Log("🧹 반응 큐 초기화 (호환성 유지)");
    }
    
    /// <summary>
    /// 모든 VFX 제거 (호환성 유지)
    /// </summary>
    public void ClearAllVFX()
    {
        // 새로운 구조에서는 VFX를 사용하지 않지만 호환성을 위해 유지
        Debug.Log("🧹 VFX 초기화 (호환성 유지)");
    }
    
    void OnDestroy()
    {
        // 반응 시스템 정지
        StopAudienceReactions();
        
        // 이벤트 해제
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart -= StartAudienceReactions;
            transitionManager.OnPresentationEnd -= StopAudienceReactions;
        }
        
        Debug.Log("🎭 AudienceReactionManager 리소스 정리 완료");
    }
} 