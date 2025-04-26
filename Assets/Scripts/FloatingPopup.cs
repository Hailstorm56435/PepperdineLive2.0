using UnityEngine;
using TMPro;

public class FloatingPopup : MonoBehaviour
{
    [Header("Popup Settings")]
    // The TMP_Text component used to display the label.
    public TMP_Text popupText;
    // Default offset if not overridden.
    public Vector3 defaultOffset = new Vector3(0, 2f, 0);

    [Header("Fade Settings")]
    // Time to fade out completely when mouse is not hovering.
    public float fadeDuration = 1.0f;

    [Header("Mode Settings")]
    // When true the popup will persist (i.e. not fade out automatically)
    public bool isPersistent = false;  

    private Transform target;
    private Vector3 offset;
    private bool isHovering = false;
    private bool isFading = false;
    private float fadeTimer = 0f;
    private float currentAlpha = 0f;

    void Awake()
    {
        // Ensure the popup text has a unique material instance.
        if (popupText != null)
        {
            popupText.fontMaterial = new Material(popupText.fontMaterial);
        }
    }

    void Update()
    {
        // Make the popup follow the target.
        if (target != null)
            transform.position = target.position + offset;

        // Only run fade logic for non-persistent popups.
        if (!isPersistent)
        {
            if (!isHovering && isFading)
            {
                fadeTimer += Time.deltaTime;
                // Lerp from the currentAlpha (captured when fade started) to 0.
                float newAlpha = Mathf.Lerp(currentAlpha, 0f, fadeTimer / fadeDuration);
                SetAlpha(newAlpha);
                if (fadeTimer >= fadeDuration)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Sets the current alpha for the popup text.
    /// </summary>
    public void SetAlpha(float alpha)
    {
        currentAlpha = alpha;
        if (popupText != null)
        {
            Color c = popupText.color;
            c.a = alpha;
            popupText.color = c;
        }
    }

    /// <summary>
    /// Initializes the popup by setting its target and text.
    /// For persistent popups, the alpha is immediately set to 1.
    /// </summary>
    public void SetTarget(Transform newTarget, string buildingName)
    {
        target = newTarget;
        if (popupText != null)
        {
            popupText.text = buildingName;
        }
        // Use default offset initially.
        offset = defaultOffset;

        // Check for building-specific overrides.
        BuildingInfo bInfo = newTarget.GetComponent<BuildingInfo>();
        if (bInfo != null)
        {
            if (bInfo.popupOffset != Vector3.zero)
            {
                offset = bInfo.popupOffset;
            }
            if (bInfo.popupFontSize != 0)
            {
                popupText.fontSize = bInfo.popupFontSize;
            }
        }
        // Start fully transparent for non-persistent popups.
        SetAlpha(0f);
        // For persistent popups, force full opacity.
        if (isPersistent)
        {
            SetAlpha(1f);
        }

        isHovering = true;
        isFading = false;
        fadeTimer = 0f;
    }

    /// <summary>
    /// Called by the manager while hovering. Progress should be between 0 (just started) and 1 (fully visible).
    /// For persistent popups, we simply lock alpha at 1.
    /// </summary>
    public void UpdateAppearProgress(float progress)
    {
        if (isPersistent)
        {
            SetAlpha(1f);
            return;
        }
        // Cancel any ongoing fade-out.
        if (isFading)
        {
            isFading = false;
            fadeTimer = 0f;
        }
        isHovering = true;
        SetAlpha(Mathf.Clamp01(progress));
    }

    /// <summary>
    /// Called when the mouse is no longer over the target.
    /// For non-persistent popups, this begins the fade-out process.
    /// </summary>
    public void OnHoverExit()
    {
        if (isPersistent)
            return;

        isHovering = false;
        if (!isFading)
        {
            isFading = true;
            fadeTimer = 0f;
        }
    }
}
