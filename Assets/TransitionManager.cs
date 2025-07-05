using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class TransitionManager : MonoBehaviour
{
    [Header("발표 자료 설정")]
    public List<Texture2D> slideImages = new List<Texture2D>(); // 발표 자료 이미지 리스트
    public RawImage slideDisplay; // 발표 자료를 표시할 RawImage
    
    [Header("컨트롤러 설정")]
    public XRBaseController rightController; // 오른쪽 컨트롤러
    public InputActionReference rightTriggerAction; // 오른쪽 트리거 입력 액션
    
    [Header("슬라이드 정보")]
    public int currentSlideIndex = 0;
    public bool isPresenting = false;
    
    [Header("이벤트")]
    public System.Action<int> OnSlideChanged; // 슬라이드 변경 이벤트
    public System.Action OnPresentationStart; // 발표 시작 이벤트
    public System.Action OnPresentationEnd; // 발표 종료 이벤트
    
    private bool rightTriggerPressed = false;
    
    // Start is called before the first frame update
    void Start()
    {
        // 초기 슬라이드 설정
        if (slideImages.Count > 0)
        {
            DisplaySlide(0);
        }
        
        // InputActionReference 설정 확인
        CheckInputActionReferences();
    }

    // Update is called once per frame
    void Update()
    {
        HandleRightControllerInput();
    }
    
    /// <summary>
    /// 오른쪽 컨트롤러 입력 처리
    /// </summary>
    private void HandleRightControllerInput()
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
    /// 다음 슬라이드로 넘어가기
    /// </summary>
    public void NextSlide()
    {
        if (slideImages.Count == 0) return;
        
        currentSlideIndex++;
        if (currentSlideIndex >= slideImages.Count)
        {
            currentSlideIndex = slideImages.Count - 1;
            EndPresentation();
            return;
        }
        
        DisplaySlide(currentSlideIndex);
    }
    
    /// <summary>
    /// 이전 슬라이드로 돌아가기
    /// </summary>
    public void PreviousSlide()
    {
        if (slideImages.Count == 0) return;
        
        currentSlideIndex--;
        if (currentSlideIndex < 0)
        {
            currentSlideIndex = 0;
        }
        
        DisplaySlide(currentSlideIndex);
    }
    
    /// <summary>
    /// 특정 슬라이드 표시
    /// </summary>
    /// <param name="index">슬라이드 인덱스</param>
    public void DisplaySlide(int index)
    {
        if (index < 0 || index >= slideImages.Count || slideDisplay == null) return;
        
        currentSlideIndex = index;
        slideDisplay.texture = slideImages[index];
        
        // 슬라이드 변경 이벤트 발생
        OnSlideChanged?.Invoke(currentSlideIndex);
        
        Debug.Log($"슬라이드 {currentSlideIndex + 1}/{slideImages.Count} 표시");
    }
    
    /// <summary>
    /// 발표 시작
    /// </summary>
    public void StartPresentation()
    {
        isPresenting = true;
        currentSlideIndex = 0;
        DisplaySlide(0);
        OnPresentationStart?.Invoke();
        Debug.Log("발표 시작");
    }
    
    /// <summary>
    /// 발표 종료
    /// </summary>
    public void EndPresentation()
    {
        isPresenting = false;
        OnPresentationEnd?.Invoke();
        Debug.Log("발표 종료");
    }
    
    /// <summary>
    /// 슬라이드 이미지 추가
    /// </summary>
    /// <param name="texture">추가할 텍스처</param>
    public void AddSlide(Texture2D texture)
    {
        slideImages.Add(texture);
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
        if (rightTriggerAction == null)
        {
            Debug.LogError("Right Trigger Action이 설정되지 않았습니다! Inspector에서 'XRI RightHand/Trigger'를 설정해주세요.");
        }
        else
        {
            Debug.Log($"Right Trigger Action 설정됨: {rightTriggerAction.action.name}");
        }
    }
}
