using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using TMPro;

public class HandPresentationController : MonoBehaviour
{
    [Header("왼손 발표 자료 설정")]
    public List<string> scriptTexts = new List<string>(); // 대본 리스트
    public GameObject leftHandPanel; // 왼손에 표시될 패널
    public TextMeshProUGUI scriptDisplayText; // 대본을 표시할 텍스트 (TextMeshPro)
    
    [Header("컨트롤러 설정")]
    public XRBaseController leftController; // 왼쪽 컨트롤러
    public InputActionReference leftTriggerAction; // 왼쪽 트리거 입력 액션
    public InputActionReference leftPrimaryButtonAction; // 왼쪽 A 버튼 입력 액션
    
    [Header("스크립트 정보")]
    public int currentScriptIndex = 0;
    public bool isHandPanelActive = false;
    
    [Header("이벤트")]
    public System.Action<int> OnScriptChanged; // 스크립트 변경 이벤트
    
    private bool leftTriggerPressed = false;
    private bool leftAButtonPressed = false;
    
    void Start()
    {
        // 초기 상태 설정
        if (leftHandPanel != null)
        {
            leftHandPanel.SetActive(false);
        }
        
        // 초기 스크립트 설정
        if (scriptTexts.Count > 0)
        {
            DisplayScript(0);
        }
        
        // InputActionReference 설정 확인
        CheckInputActionReferences();
    }
    
    /// <summary>
    /// InputActionReference 설정 확인
    /// </summary>
    private void CheckInputActionReferences()
    {
        if (leftTriggerAction == null)
        {
            Debug.LogError("Left Trigger Action이 설정되지 않았습니다! Inspector에서 'XRI LeftHand/Trigger'를 설정해주세요.");
        }
        else
        {
            Debug.Log($"Left Trigger Action 설정됨: {leftTriggerAction.action.name}");
        }
        
        if (leftPrimaryButtonAction == null)
        {
            Debug.LogError("Left Primary Button Action이 설정되지 않았습니다! Inspector에서 'XRI LeftHand/Primary Button'을 설정해주세요.");
        }
        else
        {
            Debug.Log($"Left Primary Button Action 설정됨: {leftPrimaryButtonAction.action.name}");
        }
    }
    
    void Update()
    {
        HandleLeftControllerInput();
    }
    
    /// <summary>
    /// 왼쪽 컨트롤러 입력 처리
    /// </summary>
    private void HandleLeftControllerInput()
    {
        if (leftController == null) return;
        
        // 트리거 버튼 입력 감지 (누르고 있을때 패널 활성화)
        bool triggerPressed = false;
        
        if (leftTriggerAction != null && leftTriggerAction.action != null)
        {
            // InputActionReference를 통한 입력 감지
            triggerPressed = leftTriggerAction.action.ReadValue<float>() > 0.5f;
        }
        else
        {
            // InputActionReference가 없으면 경고 메시지 출력
            if (leftTriggerAction == null)
            {
                Debug.LogWarning("Left Trigger Action이 설정되지 않았습니다. Inspector에서 설정해주세요.");
            }
            return;
        }
        
        if (triggerPressed != leftTriggerPressed)
        {
            SetHandPanelActive(triggerPressed);
            leftTriggerPressed = triggerPressed;
        }
        
        // A 버튼 입력 감지 (다음 페이지로 넘어가기)
        bool aButtonPressed = false;
        
        if (leftPrimaryButtonAction != null && leftPrimaryButtonAction.action != null)
        {
            // InputActionReference를 통한 입력 감지
            aButtonPressed = leftPrimaryButtonAction.action.ReadValue<float>() > 0.5f;
        }
        else
        {
            // InputActionReference가 없으면 경고 메시지 출력
            if (leftPrimaryButtonAction == null)
            {
                Debug.LogWarning("Left Primary Button Action이 설정되지 않았습니다. Inspector에서 설정해주세요.");
            }
            return;
        }
        
        if (aButtonPressed && !leftAButtonPressed)
        {
            NextScript();
        }
        leftAButtonPressed = aButtonPressed;
    }
    
    /// <summary>
    /// 왼손 패널 활성화/비활성화
    /// </summary>
    /// <param name="active">활성화 여부</param>
    private void SetHandPanelActive(bool active)
    {
        if (leftHandPanel == null) return;
        
        isHandPanelActive = active;
        leftHandPanel.SetActive(active);
        
        Debug.Log($"왼손 패널 {(active ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 다음 스크립트로 넘어가기
    /// </summary>
    public void NextScript()
    {
        if (scriptTexts.Count == 0) return;
        
        currentScriptIndex++;
        if (currentScriptIndex >= scriptTexts.Count)
        {
            currentScriptIndex = scriptTexts.Count - 1;
            return;
        }
        
        DisplayScript(currentScriptIndex);
    }
    
    /// <summary>
    /// 이전 스크립트로 돌아가기
    /// </summary>
    public void PreviousScript()
    {
        if (scriptTexts.Count == 0) return;
        
        currentScriptIndex--;
        if (currentScriptIndex < 0)
        {
            currentScriptIndex = 0;
        }
        
        DisplayScript(currentScriptIndex);
    }
    
    /// <summary>
    /// 특정 스크립트 표시
    /// </summary>
    /// <param name="index">스크립트 인덱스</param>
    public void DisplayScript(int index)
    {
        if (index < 0 || index >= scriptTexts.Count || scriptDisplayText == null) return;
        
        currentScriptIndex = index;
        scriptDisplayText.text = scriptTexts[index];
        
        // 스크립트 변경 이벤트 발생
        OnScriptChanged?.Invoke(currentScriptIndex);
        
        Debug.Log($"스크립트 {currentScriptIndex + 1}/{scriptTexts.Count} 표시");
    }
    
    /// <summary>
    /// 스크립트 텍스트 추가
    /// </summary>
    /// <param name="text">추가할 텍스트</param>
    public void AddScript(string text)
    {
        scriptTexts.Add(text);
    }
    
    /// <summary>
    /// 모든 스크립트 제거
    /// </summary>
    public void ClearScripts()
    {
        scriptTexts.Clear();
        currentScriptIndex = 0;
    }
    
    /// <summary>
    /// 현재 스크립트 정보 반환
    /// </summary>
    /// <returns>현재 스크립트 인덱스와 전체 스크립트 수</returns>
    public (int current, int total) GetScriptInfo()
    {
        return (currentScriptIndex + 1, scriptTexts.Count);
    }
} 