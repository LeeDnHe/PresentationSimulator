using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudienceReactionManager : MonoBehaviour
{
    [Header("청중 애니메이션 설정")]
    public Animator[] audienceAnimators; // 청중 애니메이터들
    public string animationTrigger = "Animation_On"; // 애니메이션 트리거 이름
    public string clapTrigger = "Clap_On"; // 박수 애니메이션 트리거 이름
    public float animationInterval = 15f; // 애니메이션 발동 간격 (초)
    public int animationCount = 2; // 한 번에 발동할 애니메이션 개수
    
    [Header("효과음 설정")]
    public AudioClip[] soundEffects; // 효과음 리스트
    public AudioSource audioSource; // 오디오 소스
    public float baseVolume = 0.7f;
    public float soundInterval = 30f; // 효과음 발동 간격 (초)
    public float soundProbability = 0.5f; // 효과음 발동 확률
    public float soundDuration = 3f; // 음향 재생 시간 (초)
    
    [Header("박수 소리 설정")]
    public AudioClip clapSound; // 박수 소리 클립
    public float clapVolume = 0.8f; // 박수 소리 볼륨
    
    [Header("게임 제어")]
    public bool isActive = false; // 반응 시스템 활성화 상태
    
    private Coroutine animationCoroutine;
    private Coroutine soundCoroutine;
    private Coroutine currentRandomSoundCoroutine; // 현재 재생 중인 랜덤 사운드 코루틴
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
        
        // 박수 소리 클립 확인
        if (clapSound == null)
        {
            Debug.LogWarning("⚠️ 박수 소리가 설정되지 않았습니다!");
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
        
        // 게임 종료 시 모든 청중이 박수치는 애니메이션 발동
        TriggerAllClapAnimations();
        
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
        
        // 현재 재생 중인 랜덤 사운드 정지
        if (currentRandomSoundCoroutine != null)
        {
            StopCoroutine(currentRandomSoundCoroutine);
            currentRandomSoundCoroutine = null;
        }
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        Debug.Log("🎭 청중 반응 정지! (모든 청중 박수 애니메이션 발동)");
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
                    // 애니메이션 On 상태로 설정
                    animator.SetBool(animationTrigger, true);
                    Debug.Log($"🎭 애니메이션 발동: {animator.name}");
                    
                    // 2초 후 애니메이션 Off 상태로 되돌리기
                    StartCoroutine(ResetAnimationAfterDelay(animator, 2f));
                }
                usedAnimatorIndices.Add(randomIndex);
            }
        }
    }
    
    /// <summary>
    /// 지연 후 애니메이션 리셋
    /// </summary>
    /// <param name="animator">대상 애니메이터</param>
    /// <param name="delay">지연 시간</param>
    private IEnumerator ResetAnimationAfterDelay(Animator animator, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (animator != null)
        {
            animator.SetBool(animationTrigger, false);
            Debug.Log($"🎭 애니메이션 리셋: {animator.name}");
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
    /// 랜덤 효과음 재생 (3초 후 자동 정지)
    /// </summary>
    private void TriggerRandomSound()
    {
        if (soundEffects == null || soundEffects.Length == 0 || audioSource == null) return;
        
        // 이전에 재생 중인 랜덤 사운드가 있으면 정지
        if (currentRandomSoundCoroutine != null)
        {
            StopCoroutine(currentRandomSoundCoroutine);
            currentRandomSoundCoroutine = null;
        }
        
        // 랜덤 효과음 선택
        int randomIndex = Random.Range(0, soundEffects.Length);
        AudioClip selectedSound = soundEffects[randomIndex];
        
        if (selectedSound != null)
        {
            // 랜덤 사운드 재생 및 3초 후 정지 코루틴 시작
            currentRandomSoundCoroutine = StartCoroutine(PlayRandomSoundWithDuration(selectedSound));
            Debug.Log($"🔊 효과음 재생: {selectedSound.name} ({soundDuration}초 후 정지)");
        }
    }
    
    /// <summary>
    /// 랜덤 사운드 재생 및 지정 시간 후 정지
    /// </summary>
    /// <param name="soundClip">재생할 음향 클립</param>
    private IEnumerator PlayRandomSoundWithDuration(AudioClip soundClip)
    {
        if (audioSource == null || soundClip == null) yield break;
        
        // 음향 재생
        audioSource.clip = soundClip;
        audioSource.volume = baseVolume;
        audioSource.Play();
        
        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(soundDuration);
        
        // 음향 정지
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log($"🔇 효과음 정지: {soundClip.name}");
        }
        
        // 코루틴 참조 정리
        currentRandomSoundCoroutine = null;
    }
    
    /// <summary>
    /// 모든 청중 박수 애니메이션 발동 (게임 종료 시)
    /// </summary>
    private void TriggerAllClapAnimations()
    {
        if (audienceAnimators == null || audienceAnimators.Length == 0) return;
        
        int triggeredCount = 0;
        
        // 모든 청중 애니메이터에게 박수 애니메이션 시작
        for (int i = 0; i < audienceAnimators.Length; i++)
        {
            Animator animator = audienceAnimators[i];
            if (animator != null)
            {
                // 박수 애니메이션 On 상태로 설정
                animator.SetBool(clapTrigger, true);
                triggeredCount++;
                Debug.Log($"👏 박수 애니메이션 발동: {animator.name}");
            }
        }
        
        // 박수 소리 즉시 재생
        PlayClapSound();
        
        // 5초 후 박수 애니메이션 및 사운드 정지
        StartCoroutine(StopClapAfterDelay(5f));
        
        Debug.Log($"🎉 게임 종료 - 총 {triggeredCount}명의 청중이 박수를 칩니다! (5초 후 정지)");
    }
    
    /// <summary>
    /// 지연 후 박수 애니메이션 및 사운드 정지
    /// </summary>
    /// <param name="delay">지연 시간</param>
    private IEnumerator StopClapAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 모든 청중 박수 애니메이션 정지
        if (audienceAnimators != null)
        {
            for (int i = 0; i < audienceAnimators.Length; i++)
            {
                Animator animator = audienceAnimators[i];
                if (animator != null)
                {
                    animator.SetBool(clapTrigger, false);
                    Debug.Log($"👏 박수 애니메이션 정지: {animator.name}");
                }
            }
        }
        
        // 오디오 소스 정지
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("🔇 박수 소리 정지");
        }
        
        Debug.Log("🎉 박수 애니메이션 및 사운드 정지 완료");
    }
    
    /// <summary>
    /// 박수 소리 재생
    /// </summary>
    private void PlayClapSound()
    {
        if (clapSound == null || audioSource == null) 
        {
            Debug.LogWarning("⚠️ 박수 소리를 재생할 수 없습니다!");
            return;
        }
        
        // 박수 소리 재생
        audioSource.PlayOneShot(clapSound, clapVolume);
        Debug.Log($"👏 박수 소리 재생: {clapSound.name}");
    }
    
    /// <summary>
    /// 수동으로 모든 청중 박수 애니메이션 발동 (공개 메서드)
    /// </summary>
    public void TriggerClapAnimations()
    {
        TriggerAllClapAnimations();
    }
    
    /// <summary>
    /// 박수 볼륨 설정
    /// </summary>
    /// <param name="volume">볼륨 (0~1)</param>
    public void SetClapVolume(float volume)
    {
        clapVolume = Mathf.Clamp01(volume);
        Debug.Log($"👏 박수 볼륨 설정: {clapVolume:F2}");
    }
    
    /// <summary>
    /// 랜덤 사운드 볼륨 설정
    /// </summary>
    /// <param name="volume">볼륨 (0~1)</param>
    public void SetRandomSoundVolume(float volume)
    {
        baseVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = baseVolume;
        }
        Debug.Log($"🔊 랜덤 사운드 볼륨 설정: {baseVolume:F2}");
    }
    
    /// <summary>
    /// 랜덤 사운드 재생 시간 설정
    /// </summary>
    /// <param name="duration">재생 시간 (초)</param>
    public void SetSoundDuration(float duration)
    {
        soundDuration = Mathf.Max(0.1f, duration);
        Debug.Log($"⏱️ 랜덤 사운드 재생 시간 설정: {soundDuration:F1}초");
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
        
        // 추가 랜덤 사운드 정리
        if (currentRandomSoundCoroutine != null)
        {
            StopCoroutine(currentRandomSoundCoroutine);
            currentRandomSoundCoroutine = null;
        }
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // 이벤트 해제
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart -= StartAudienceReactions;
            transitionManager.OnPresentationEnd -= StopAudienceReactions;
        }
        
        Debug.Log("🎭 AudienceReactionManager 리소스 정리 완료");
    }
} 