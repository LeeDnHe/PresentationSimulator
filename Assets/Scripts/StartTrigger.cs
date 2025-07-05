using UnityEngine;

public class StartTrigger : MonoBehaviour
{
    [Header("트리거 설정")]
    public string playerTag = "Player"; // 플레이어 태그
    public bool usePlayerTag = true; // 플레이어 태그 사용 여부
    
    [Header("연결할 매니저")]
    public FeedbackManager feedbackManager; // 피드백 매니저 참조
    
    [Header("라이팅 설정")]
    public GameObject lightingObject; // 비활성화할 라이팅 게임 오브젝트
    public bool deactivateLighting = true; // 라이팅 비활성화 여부
    
    private bool hasTriggered = false; // 중복 트리거 방지
    
    void Start()
    {
        Debug.Log("🎯 StartTrigger 초기화됨");
        
        // FeedbackManager 자동 찾기
        if (feedbackManager == null)
        {
            feedbackManager = FindObjectOfType<FeedbackManager>();
            if (feedbackManager != null)
            {
                Debug.Log($"✅ FeedbackManager 자동 찾기 성공: {feedbackManager.name}");
            }
            else
            {
                Debug.LogError("❌ FeedbackManager를 찾을 수 없습니다!");
            }
        }
        
        // 콜라이더 설정 확인
        CheckColliderSetup();
        
        // 라이팅 설정 확인
        CheckLightingSetup();
    }
    
    /// <summary>
    /// 콜라이더 설정 확인
    /// </summary>
    private void CheckColliderSetup()
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider == null)
        {
            Debug.LogError("❌ 이 GameObject에 콜라이더가 없습니다! BoxCollider를 추가해주세요.");
            return;
        }
        
        if (!myCollider.isTrigger)
        {
            Debug.LogWarning("⚠️ 콜라이더가 Trigger로 설정되어 있지 않습니다. Trigger로 설정합니다.");
            myCollider.isTrigger = true;
        }
        
        Debug.Log($"✅ 콜라이더 설정 완료 - 타입: {myCollider.GetType().Name}, isTrigger: {myCollider.isTrigger}");
    }
    
    /// <summary>
    /// 라이팅 설정 확인
    /// </summary>
    private void CheckLightingSetup()
    {
        if (deactivateLighting)
        {
            if (lightingObject != null)
            {
                // 초기에는 라이팅 활성화 상태로 유지
                lightingObject.SetActive(true);
                Debug.Log($"✅ 라이팅 오브젝트 설정 완료: {lightingObject.name} (초기 상태: 활성화)");
            }
            else
            {
                Debug.LogWarning("⚠️ 라이팅 비활성화가 활성화되어 있지만 라이팅 오브젝트가 할당되지 않았습니다!");
            }
        }
        else
        {
            Debug.Log("💡 라이팅 비활성화 기능이 비활성화되어 있습니다.");
        }
    }
    
    /// <summary>
    /// 트리거 진입 시 발표 시작
    /// </summary>
    /// <param name="other">진입한 콜라이더</param>
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔥 OnTriggerEnter 감지! 진입 객체: {other.name}, 태그: {other.tag}");
        
        // 이미 트리거된 경우 무시
        if (hasTriggered)
        {
            Debug.Log("❌ 이미 트리거되었으므로 무시");
            return;
        }
        
        // 플레이어 태그 확인 (옵션)
        if (usePlayerTag && !other.CompareTag(playerTag))
        {
            Debug.Log($"❌ 태그 불일치 - 필요: {playerTag}, 실제: {other.tag}");
            return;
        }
        
        // FeedbackManager 확인
        if (feedbackManager == null)
        {
            Debug.LogError("❌ FeedbackManager가 연결되지 않았습니다!");
            return;
        }
        
        // 발표 시작
        Debug.Log($"✅ 시작 조건 충족! 진입 객체: {other.name}");
        hasTriggered = true;
        
        // 라이팅 비활성화
        if (deactivateLighting && lightingObject != null)
        {
            lightingObject.SetActive(false);
            Debug.Log($"💡 라이팅 오브젝트 비활성화: {lightingObject.name}");
        }
        
        feedbackManager.StartPresentationPublic();
    }
    
    /// <summary>
    /// 트리거 초기화 (재사용을 위해)
    /// </summary>
    [ContextMenu("트리거 초기화")]
    public void ResetTrigger()
    {
        hasTriggered = false;
        
        // 라이팅 다시 활성화
        if (deactivateLighting && lightingObject != null)
        {
            lightingObject.SetActive(true);
            Debug.Log($"💡 라이팅 오브젝트 다시 활성화: {lightingObject.name}");
        }
        
        Debug.Log("🔄 트리거 상태 초기화됨");
    }
    
    /// <summary>
    /// 강제 트리거 테스트
    /// </summary>
    [ContextMenu("강제 트리거 테스트")]
    public void TestTrigger()
    {
        Debug.Log("🧪 강제 트리거 테스트 실행");
        if (feedbackManager != null)
        {
            hasTriggered = true;
            
            // 라이팅 비활성화 (테스트에도 적용)
            if (deactivateLighting && lightingObject != null)
            {
                lightingObject.SetActive(false);
                Debug.Log($"💡 [테스트] 라이팅 오브젝트 비활성화: {lightingObject.name}");
            }
            
            feedbackManager.StartPresentationPublic();
        }
        else
        {
            Debug.LogError("❌ FeedbackManager가 연결되지 않았습니다!");
        }
    }
} 