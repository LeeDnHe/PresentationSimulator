using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    private Animator animator;

    public float animationDuration = 2f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.SetBool("Animation_On", true);
            Invoke("ResetAnimationFlag", animationDuration);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            animator.SetBool("Clap_On", true);  // �� ���� true ����
        }
    }

    void ResetAnimationFlag()
    {
        animator.SetBool("Animation_On", false);
    }
}


