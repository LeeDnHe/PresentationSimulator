using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    private Animator animator;

    // �ִϸ��̼� ���� �ð� (2��)

    void Start()
    {
        // Animator ������Ʈ ��������
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // R Ű ������ �ִϸ��̼� ����
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.SetBool("Animation_On", true);
            // ���� �ð� �� �ٽ� false�� ����
        }
    }

    // �ִϸ��̼� �Ķ���� �ǵ�����

}

