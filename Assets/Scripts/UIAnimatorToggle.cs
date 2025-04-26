using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIAnimatorToggle : MonoBehaviour
{

    public Button toggleButton;
    
    public Animator targetAnimator;

    public bool startMinimized = true;

    void Awake()
    {
        if (toggleButton == null)
            Debug.LogError($"[{nameof(UIAnimatorToggle)}] No Button assigned on {name}!", this);
        if (targetAnimator == null)
            Debug.LogError($"[{nameof(UIAnimatorToggle)}] No Animator assigned on {name}!", this);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);
    }

    
    public void Toggle()
    {
	targetAnimator.SetTrigger("toggle");
    }
}
