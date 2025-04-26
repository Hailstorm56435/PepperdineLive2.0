using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BuildingFloorController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private TMP_Text aliasText;              // ← displays first alias or "none"
    [SerializeField] private Button nextFloorButton;
    [SerializeField] private Button prevFloorButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private TMP_Text tooltipText;

    [Header("UI Animation")]
    [Tooltip("Animator with Show/Hide triggers for the entire panel")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private string showTrigger = "Show";
    [SerializeField] private string hideTrigger = "Hide";

    [Header("Floor Settings")]
    [SerializeField] private int minFloor = 1;
    [SerializeField] private int maxFloor = 5;

    // —————————————————————————
    // Internal state
    // —————————————————————————
    private BuildingInfo focusedBuilding;
    private Animator    buildingAnimator;
    private int         currentFloor = 1;
    private bool        panelVisible  = false;

    private void Awake()
    {
        if (nextFloorButton  != null) nextFloorButton.onClick .AddListener(NextFloor);
        if (prevFloorButton  != null) prevFloorButton.onClick .AddListener(PreviousFloor);
        if (closeButton      != null) closeButton.onClick     .AddListener(OnCloseButtonClicked);

        // start hidden
        gameObject.SetActive(false);
    }

    public void SetFocusedBuilding(BuildingInfo building)
    {
        if (tooltipText != null)
{
    tooltipText.text = building.Tooltip ?? "";
    tooltipText.gameObject.SetActive(!string.IsNullOrWhiteSpace(tooltipText.text));
}
        if (building == null)
        {
            Debug.LogWarning("SetFocusedBuilding: building is null");
            return;
        }

        // if we're already showing this same building, do nothing
        if (panelVisible && focusedBuilding == building)
            return;

        // if panel was open for a different building, unfocus it first
        if (panelVisible)
            InternalUnfocus();

        focusedBuilding  = building;
        buildingAnimator = building.GetAnimator();

        // name & alias
        buildingNameText.text = building.CanonicalName;
        if (aliasText != null)
        {
            var aliases = building.Aliases;
            aliasText.text = (aliases != null && aliases.Count > 0)
                ? aliases[0]
                : "none";
            aliasText.gameObject.SetActive(true);
        }

        // floor buttons
        bool supportsFloor = building.animateFloors;
        nextFloorButton.gameObject.SetActive(supportsFloor);
        prevFloorButton.gameObject.SetActive(supportsFloor);

        if (supportsFloor)
            GoToFloor(building.targetFloor);
        else
            ResetAnimation();

        ShowPanel();
    }

    public void UnfocusBuilding()
    {
        OnCloseButtonClicked();
    }

    private void ShowPanel()
    {
        gameObject.SetActive(true);
        if (uiAnimator != null)
            uiAnimator.SetTrigger(showTrigger);
        panelVisible = true;
    }

    private void OnCloseButtonClicked()
    {
        if (!panelVisible) return;

        HidePanel();
        InternalUnfocus();
    }

    private void HidePanel()
    {
        if (uiAnimator != null)
            uiAnimator.SetTrigger(hideTrigger);
        panelVisible = false;
    }

    private void InternalUnfocus()
    {
        if (focusedBuilding != null)
        {
            focusedBuilding.OnFocusLost();
            Debug.Log("Unfocused building: " + focusedBuilding.CanonicalName);
        }
        focusedBuilding  = null;
        buildingAnimator = null;
    }

    public void ResetAnimation()
    {
        if (buildingAnimator != null)
            buildingAnimator.SetTrigger("ResetFloor");
    }

    public BuildingInfo GetFocusedBuilding() => focusedBuilding;

    // —————————————————————————
    // Floor‐nav controls
    // —————————————————————————
    public void NextFloor()
    {
        if (currentFloor < maxFloor)
        {
            currentFloor++;
            UpdateFloorAnimation();
        }
    }

    public void PreviousFloor()
    {
        if (currentFloor > minFloor)
        {
            currentFloor--;
            UpdateFloorAnimation();
        }
    }

    public void GoToFloor(int floor)
    {
        currentFloor = Mathf.Clamp(floor, minFloor, maxFloor);
        UpdateFloorAnimation();
    }

    private void UpdateFloorAnimation()
    {
        if (buildingAnimator != null)
        {
            buildingAnimator.SetInteger("TargetFloor", currentFloor);
            buildingAnimator.SetTrigger("AnimateFloor");
        }
    }
}
