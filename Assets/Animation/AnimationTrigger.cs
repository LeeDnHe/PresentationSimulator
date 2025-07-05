using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    private Animator animator;

    // 애니메이션 유지 시간 (2초)

    void Start()
    {
        // Animator 컴포넌트 가져오기
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // R 키 누르면 애니메이션 시작
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.SetBool("Animation_On", true);
            // 일정 시간 후 다시 false로 설정
        }
    }

    // 애니메이션 파라미터 되돌리기

}

