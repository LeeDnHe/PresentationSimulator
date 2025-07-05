using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class TransitionManager : MonoBehaviour
{
    [Header("발표 자료 설정")]
    public List<Sprite> slideImages = new List<Sprite>(); // 발표 자료 이미지 리스트 (Sprite)
    public Image slideDisplay; // 발표 자료를 표시할 Image
    public Sprite defaultSlideImage; // 기본 슬라이드 이미지 (발표 시작 전 표시)
    
    [Header("컨트롤러 설정")]
    public XRBaseController rightController; // 오른쪽 컨트롤러
    public InputActionReference rightTriggerAction; // 오른쪽 트리거 입력 액션 (다음 슬라이드)
    public InputActionReference leftGripAction; // 왼쪽 그랩 입력 액션 (이전 슬라이드)
    
    [Header("슬라이드 정보")]
    public int currentSlideIndex = 0;
    public bool isPresenting = false;
    public bool isPresentationReady = false; // 발표 준비 상태
    public bool isOnLastSlide = false; // 마지막 슬라이드 도달 상태
    
    [Header("이벤트")]
    public System.Action<int> OnSlideChanged; // 슬라이드 변경 이벤트
    public System.Action OnPresentationStart; // 발표 시작 이벤트
    public System.Action OnPresentationEnd; // 발표 종료 이벤트
    
    private bool rightTriggerPressed = false;
    private bool leftGripPressed = false;
    
    // Start is called before the first frame update
    void Start()
    {
        // InputActionReference 설정 확인
        CheckInputActionReferences();
        
        Debug.Log("발표 대기 중... 시작 버튼을 눌러주세요.");
        
        // UI 시스템 초기화 후 기본 이미지 설정 (지연 호출)
        StartCoroutine(SetDefaultSlideDelayed());
    }
    
    /// <summary>
    /// 지연된 기본 슬라이드 설정
    /// </summary>
    private IEnumerator SetDefaultSlideDelayed()
    {
        // 한 프레임 대기 (UI 시스템 초기화 완료 대기)
        yield return null;
        
        // 기본 이미지 설정
        SetDefaultSlide();
    }

    // Update is called once per frame
    void Update()
    {
        HandleControllerInput();
    }
    
    /// <summary>
    /// 컨트롤러 입력 처리
    /// </summary>
    private void HandleControllerInput()
    {
        // 발표가 시작된 후에만 컨트롤러 입력 처리
        if (!isPresenting) return;
        
        // 오른쪽 트리거 버튼 처리 (다음 슬라이드)
        HandleRightTrigger();
        
        // 왼쪽 그랩 버튼 처리 (이전 슬라이드)  
        HandleLeftGrip();
    }
    
    /// <summary>
    /// 오른쪽 트리거 버튼 처리 (다음 슬라이드)
    /// </summary>
    private void HandleRightTrigger()
    {
        if (rightController == null) return;
        
        // 트리거 버튼 입력 감지
        bool triggerPressed = false;
        
        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            // InputActionReference를 통한 입력 감지
            triggerPressed = rightTriggerAction.action.ReadValue<float>() > 0.5f;
        }
        else
        {
            // InputActionReference가 없으면 경고 메시지 출력
            if (rightTriggerAction == null)
            {
                Debug.LogWarning("Right Trigger Action이 설정되지 않았습니다. Inspector에서 설정해주세요.");
            }
            return;
        }
        
        if (triggerPressed && !rightTriggerPressed)
        {
            NextSlide();
        }
        rightTriggerPressed = triggerPressed;
    }
    
    /// <summary>
    /// 왼쪽 그랩 버튼 처리 (이전 슬라이드)
    /// </summary>
    private void HandleLeftGrip()
    {
        // 그랩 버튼 입력 감지
        bool gripPressed = false;
        
        if (leftGripAction != null && leftGripAction.action != null)
        {
            // InputActionReference를 통한 입력 감지
            gripPressed = leftGripAction.action.ReadValue<float>() > 0.5f;
        }
        else
        {
            // InputActionReference가 없으면 경고 메시지 출력
            if (leftGripAction == null)
            {
                Debug.LogWarning("Left Grip Action이 설정되지 않았습니다. Inspector에서 설정해주세요.");
            }
            return;
        }
        
        if (gripPressed && !leftGripPressed)
        {
            PreviousSlide();
        }
        leftGripPressed = gripPressed;
    }
    
    /// <summary>
    /// 다음 슬라이드로 넘어가기
    /// </summary>
    public void NextSlide()
    {
        if (slideImages.Count == 0 || !isPresenting) return;
        
        // 마지막 슬라이드에서 한 번 더 클릭하면 발표 종료
        if (isOnLastSlide)
        {
            Debug.Log("마지막 슬라이드에서 추가 클릭 감지. 발표를 종료합니다.");
            EndPresentation();
            return;
        }
        
        // 더 많은 슬라이드가 있는지 확인
        if (currentSlideIndex < slideImages.Count - 1)
        {
            currentSlideIndex++;
            DisplaySlide(currentSlideIndex);
            
            // 마지막 슬라이드 도달 체크
            if (currentSlideIndex == slideImages.Count - 1)
            {
                isOnLastSlide = true;
                Debug.Log($"마지막 슬라이드 도달: {currentSlideIndex + 1}/{slideImages.Count}");
                Debug.Log("한 번 더 클릭하면 발표가 종료됩니다.");
            }
            else
            {
                Debug.Log($"다음 슬라이드로 이동: {currentSlideIndex + 1}/{slideImages.Count}");
            }
        }
    }
    
    /// <summary>
    /// 이전 슬라이드로 돌아가기
    /// </summary>
    public void PreviousSlide()
    {
        if (slideImages.Count == 0 || !isPresenting) return;
        
        // 0번 슬라이드보다 뒤로 갈 수 있는지 확인
        if (currentSlideIndex > 0)
        {
            currentSlideIndex--;
            DisplaySlide(currentSlideIndex);
            
            // 마지막 슬라이드에서 벗어나면 isOnLastSlide 해제
            if (isOnLastSlide && currentSlideIndex < slideImages.Count - 1)
            {
                isOnLastSlide = false;
                Debug.Log("마지막 슬라이드에서 벗어남. 종료 모드 해제.");
            }
            
            Debug.Log($"이전 슬라이드로 이동: {currentSlideIndex + 1}/{slideImages.Count}");
        }
        else
        {
            // 첫 번째 슬라이드에서는 뒤로 갈 수 없음
            Debug.Log("첫 번째 슬라이드입니다. 더 이상 뒤로 갈 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 특정 슬라이드 표시
    /// </summary>
    /// <param name="index">슬라이드 인덱스</param>
    public void DisplaySlide(int index)
    {
        if (index < 0 || index >= slideImages.Count || slideDisplay == null) return;
        
        // 발표가 시작된 경우에만 슬라이드 표시
        if (!isPresenting) return;
        
        currentSlideIndex = index;
        slideDisplay.sprite = slideImages[index];
        slideDisplay.enabled = true; // 슬라이드 표시 시 이미지 활성화
        
        // 슬라이드 변경 이벤트 발생
        OnSlideChanged?.Invoke(currentSlideIndex);
        
        Debug.Log($"슬라이드 {currentSlideIndex + 1}/{slideImages.Count} 표시");
    }
    
    /// <summary>
    /// 기본 슬라이드 설정
    /// </summary>
    private void SetDefaultSlide()
    {
        if (slideDisplay == null) 
        {
            Debug.LogWarning("slideDisplay가 할당되지 않았습니다.");
            return;
        }
        
        // 기본 이미지가 설정되어 있으면 표시, 없으면 빈 화면
        if (defaultSlideImage != null)
        {
            slideDisplay.sprite = defaultSlideImage;
            slideDisplay.enabled = true;
            Debug.Log("기본 슬라이드 이미지 표시");
        }
        else
        {
            // null 대신 빈 이미지를 표시하거나 이미지 비활성화
            slideDisplay.sprite = null;
            slideDisplay.enabled = false;
            Debug.Log("기본 슬라이드 상태 (이미지 비활성화)");
        }
    }
    
    /// <summary>
    /// 발표 시작
    /// </summary>
    public void StartPresentation()
    {
        if (isPresenting)
        {
            Debug.LogWarning("이미 발표가 진행 중입니다.");
            return;
        }
        
        if (slideImages.Count == 0)
        {
            Debug.LogError("발표할 슬라이드가 없습니다!");
            return;
        }
        
        // 발표 상태 설정
        isPresenting = true;
        isPresentationReady = true;
        currentSlideIndex = 0;
        isOnLastSlide = false; // 마지막 슬라이드 상태 초기화
        
        // 첫 번째 슬라이드 표시
        DisplaySlide(0);
        
        // 발표 시작 이벤트 발생
        OnPresentationStart?.Invoke();
        
        Debug.Log($"발표 시작! 총 {slideImages.Count}개의 슬라이드");
        Debug.Log("오른손 트리거: 다음 슬라이드, 왼손 그랩: 이전 슬라이드");
    }
    
    /// <summary>
    /// 발표 종료
    /// </summary>
    public void EndPresentation()
    {
        if (!isPresenting)
        {
            Debug.LogWarning("발표가 진행 중이지 않습니다.");
            return;
        }
        
        // 발표 상태 해제
        isPresenting = false;
        isPresentationReady = false;
        isOnLastSlide = false; // 마지막 슬라이드 상태 초기화
        
        // 슬라이드를 기본 상태로 되돌리기 (화면은 유지하되 슬라이드만 초기화)
        SetDefaultSlide();
        
        // 발표 종료 이벤트 발생
        OnPresentationEnd?.Invoke();
        
        Debug.Log("발표 종료");
        Debug.Log("새로운 발표를 시작하려면 시작 버튼을 눌러주세요.");
    }
    
    /// <summary>
    /// 슬라이드 이미지 추가
    /// </summary>
    /// <param name="sprite">추가할 스프라이트</param>
    public void AddSlide(Sprite sprite)
    {
        slideImages.Add(sprite);
    }
    
    /// <summary>
    /// 모든 슬라이드 제거
    /// </summary>
    public void ClearSlides()
    {
        slideImages.Clear();
        currentSlideIndex = 0;
    }
    
    /// <summary>
    /// 현재 슬라이드 정보 반환
    /// </summary>
    /// <returns>현재 슬라이드 인덱스와 전체 슬라이드 수</returns>
    public (int current, int total) GetSlideInfo()
    {
        return (currentSlideIndex + 1, slideImages.Count);
    }
    
    /// <summary>
    /// InputActionReference 설정 확인
    /// </summary>
    private void CheckInputActionReferences()
    {
        // 오른쪽 트리거 액션 확인
        if (rightTriggerAction == null)
        {
            Debug.LogError("Right Trigger Action이 설정되지 않았습니다! Inspector에서 'XRI RightHand Interaction/Activate'를 설정해주세요.");
        }
        else
        {
            Debug.Log($"Right Trigger Action 설정됨: {rightTriggerAction.action.name}");
        }
        
        // 왼쪽 그랩 액션 확인
        if (leftGripAction == null)
        {
            Debug.LogError("Left Grip Action이 설정되지 않았습니다! Inspector에서 'XRI LeftHand Interaction/Select'를 설정해주세요.");
        }
        else
        {
            Debug.Log($"Left Grip Action 설정됨: {leftGripAction.action.name}");
        }
    }
}
