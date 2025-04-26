using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AI;

public class BuildingLookupManager : MonoBehaviour
{
    [Header("General Settings")]
    public bool debug = true;
    public float doubleClickThreshold = 0.5f;

    [Header("Smooth Zoom Settings (Double-Click)")]
    public Vector3 focusOffset = new Vector3(0, 10f, -10f);
    public float focusSpeed = 2f;

    [Header("Popup Settings")]
    public FloatingPopup popupPrefab;
    private Dictionary<BuildingInfo, FloatingPopup> activePopups = new Dictionary<BuildingInfo, FloatingPopup>();
    private Dictionary<BuildingInfo, float> hoverTimers = new Dictionary<BuildingInfo, float>();

    [Header("Hover Popup Settings")]
    public float hoverDelay = 0.5f;

    [Header("Highlight Settings")]
    // Assign your defaultHDparticle highlight material in the inspector.
    [SerializeField] private Material defaultHDparticle;

    [Header("Highlight Popup Settings")]
    // Assign a special popup prefab to display while a building is highlighted.
    [SerializeField] private FloatingPopup highlightPopupPrefab;
    private FloatingPopup currentHighlightPopup;

    private Dictionary<string, BuildingInfo> buildingDict = new(StringComparer.OrdinalIgnoreCase);
    private float lastClickTime = -1f;
    private BuildingInfo lastClickedBuilding;

    // Reference to your CameraController2.
    private CameraController2 cameraController2;
    public event Action<BuildingInfo> OnBuildingFocused;

    // Reference to the BuildingFloorController (assigned via the Inspector)
    public BuildingFloorController floorController;

    // The currently highlighted building.
    private BuildingInfo currentHighlightedBuilding = null;

    void Awake()
    {
        // Register all buildings (and their aliases)
        var allBuildings = UnityEngine.Object.FindObjectsByType<BuildingInfo>(FindObjectsSortMode.None);
        foreach (var b in allBuildings)
        {
            RegisterBuilding(b.CanonicalName, b);
            foreach (var alias in b.Aliases)
                RegisterBuilding(alias, b);
        }

        if (Camera.main != null)
            cameraController2 = Camera.main.GetComponent<CameraController2>();
        else
            Debug.LogWarning("No main camera found.");
    }

    void Update()
    {
        ProcessHover(Input.mousePosition);

        if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            ProcessClick(Input.mousePosition);
        }
    }

    private void ProcessHover(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        BuildingInfo hoveredBuilding = null;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            hoveredBuilding = hit.collider.GetComponent<BuildingInfo>();
        }

        // Remove hover timers for buildings no longer under the cursor.
        foreach (var key in hoverTimers.Keys.ToList())
        {
            if (key != hoveredBuilding)
            {
                hoverTimers.Remove(key);
            }
        }

        // Trigger fade-out for active popups that do not belong to the hovered building.
        foreach (var b in activePopups.Keys.ToList())
        {
            if (b != hoveredBuilding && activePopups[b] != null)
            {
                activePopups[b].OnHoverExit();
                if (debug)
                    Debug.Log("OnHoverExit called for: " + b.CanonicalName);
            }
        }

        if (hoveredBuilding != null)
        {
            if (!hoverTimers.ContainsKey(hoveredBuilding))
            {
                hoverTimers[hoveredBuilding] = Time.time;
                if (debug)
                    Debug.Log("Started hover timer for: " + hoveredBuilding.CanonicalName);

                // Instantiate popup if one is not already active.
                if (!activePopups.ContainsKey(hoveredBuilding) || activePopups[hoveredBuilding] == null)
                {
                    FloatingPopup newPopup = Instantiate(popupPrefab);
                    newPopup.SetTarget(hoveredBuilding.transform, hoveredBuilding.CanonicalName);
                    activePopups[hoveredBuilding] = newPopup;
                    if (debug)
                        Debug.Log("Popup instantiated for: " + hoveredBuilding.CanonicalName);
                }
            }

            float elapsed = Time.time - hoverTimers[hoveredBuilding];
            float progress = Mathf.Clamp01(elapsed / hoverDelay);
            activePopups[hoveredBuilding].UpdateAppearProgress(progress);
        }
        else
        {
            hoverTimers.Clear();
        }
    }

    /// <summary>
    /// Processes a click. If a BuildingInfo is clicked:
    /// - For room objects (isRoom == true): only highlight on single click.
    /// - For non-room objects: double-click triggers full focus.
    /// Clicking on empty space no longer clears the current highlight.
    /// </summary>
    private void ProcessClick(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            BuildingInfo building = hit.collider.GetComponent<BuildingInfo>();
            if (building != null)
            {
                // If isRoom is true, ignore double-click logic.
                if (building.isRoom)
                {
                    HighlightBuildingOnly(building);
                }
                else
                {
                    float timeSinceLastClick = Time.time - lastClickTime;
                    if (lastClickedBuilding == building && timeSinceLastClick < doubleClickThreshold)
                    {
                        // Double-click: fully focus the building.
                        FocusBuilding(building);
                    }
                    else
                    {
                        // Single click: only highlight the building.
                        HighlightBuildingOnly(building);
                    }
                }
                lastClickTime = Time.time;
                lastClickedBuilding = building;
            }
            // If a collider that isn’t a BuildingInfo is hit, do nothing.
        }
        // If nothing is hit, we leave the current highlight intact.
    }

    /// <summary>
    /// Highlights the building (sets the highlight material and instantiates a persistent highlight popup)
    /// without triggering additional focus behaviors.
    /// </summary>
    public void HighlightBuildingOnly(BuildingInfo building)
    {
        if (building != null)
        {
            // Remove highlight from the previous building if needed.
            if (currentHighlightedBuilding != null && currentHighlightedBuilding != building)
            {
                currentHighlightedBuilding.RemoveHighlight();
            }
            currentHighlightedBuilding = building;
            if (defaultHDparticle != null)
            {
                currentHighlightedBuilding.SetHighlight(defaultHDparticle);
            }

            // Instantiate the persistent highlight popup.
            if (currentHighlightPopup != null)
            {
                Destroy(currentHighlightPopup.gameObject);
            }
            if (highlightPopupPrefab != null)
            {
                currentHighlightPopup = Instantiate(highlightPopupPrefab);
                currentHighlightPopup.SetTarget(building.transform, building.CanonicalName);
            }
        }
    }

    /// <summary>
    /// Fully focuses the specified building. This method first ensures the building is highlighted,
    /// then performs additional actions (e.g. smooth zoom, floor animation, disabling the collider).
    /// For buildings where isRoom is false, full focus is allowed.
    /// </summary>
    public void FocusBuilding(BuildingInfo building)
    {
        if (building != null)
        {
            // Even if this is a room, FocusBuilding will only be called from non-room objects.
            HighlightBuildingOnly(building);

            // Unfocus any currently focused building via the floor controller (if applicable).
            if (floorController != null)
            {
                BuildingInfo currentFocused = floorController.GetFocusedBuilding();
                if (currentFocused != null && currentFocused != building)
                {
                    floorController.UnfocusBuilding();
                }
            }

            Debug.Log("FocusBuilding: Zooming into building: " + building.CanonicalName);
            OnBuildingFocused?.Invoke(building);
            StartCoroutine(SmoothZoomToBuilding(building));
            building.OnFocused();

            // Enable and update the BuildingFloorController UI.
            if (floorController != null)
            {
                floorController.gameObject.SetActive(true);
                floorController.SetFocusedBuilding(building);
            }
        }
    }

    /// <summary>
    /// (Optional) ClearHighlight would now only be called explicitly (for example, via an 'X' button).
    /// This method is kept here for potential use.
    /// </summary>
    private void ClearHighlight()
    {
        if (currentHighlightedBuilding != null)
        {
            currentHighlightedBuilding.RemoveHighlight();
            currentHighlightedBuilding = null;
        }
        if (currentHighlightPopup != null)
        {
            Destroy(currentHighlightPopup.gameObject);
            currentHighlightPopup = null;
        }
    }

    private IEnumerator SmoothZoomToBuilding(BuildingInfo building)
    {
        Camera cam = Camera.main;
        if (cam == null || cameraController2 == null)
        {
            Debug.LogWarning("Camera or CameraController2 not found.");
            yield break;
        }

        Vector3 initialPos = cam.transform.position;
        Quaternion initialRot = cam.transform.rotation;

        Vector3 targetPos = building.transform.position + focusOffset;
        Vector3 offsetFromCenter = targetPos - cameraController2.mapCenter.position;
        offsetFromCenter.y = 0f;
        if (offsetFromCenter.magnitude > cameraController2.movementRadius)
        {
            offsetFromCenter = offsetFromCenter.normalized * cameraController2.movementRadius;
            targetPos = new Vector3(cameraController2.mapCenter.position.x + offsetFromCenter.x, targetPos.y, cameraController2.mapCenter.position.z + offsetFromCenter.z);
        }
        targetPos.y = Mathf.Clamp(targetPos.y, cameraController2.minHeight, cameraController2.maxHeight);

        Vector3 direction = (building.transform.position - targetPos).normalized;
        float t = Mathf.InverseLerp(cameraController2.minHeight, cameraController2.maxHeight, targetPos.y);
        float targetPitch = Mathf.Lerp(cameraController2.minPitch, cameraController2.maxPitch, t);

        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(targetPitch, yaw, 0f);

        float elapsed = 0f;
        float duration = 1f / focusSpeed;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float tAnim = Mathf.Clamp01(elapsed / duration);
            cam.transform.position = Vector3.Lerp(initialPos, targetPos, tAnim);
            cam.transform.rotation = Quaternion.Slerp(initialRot, targetRot, tAnim);
            yield return null;
        }

        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    private void RegisterBuilding(string name, BuildingInfo info)
    {
        if (!string.IsNullOrEmpty(name) && !buildingDict.ContainsKey(name))
        {
            buildingDict[name] = info;
        }
    }

    public BuildingInfo GetBuildingByName(string name)
    {
        buildingDict.TryGetValue(name, out var info);
        return info;
    }

    public void SearchAndFocus(string query)
    {
        BuildingInfo building = GetBuildingByName(query);
        if (building != null)
        {
            FocusBuilding(building);
            return;
        }
        Debug.LogWarning($"No match found for '{query}'");
    }

    public NavMeshPath FindNavMeshPathBetweenBuildings(string startName, string endName)
    {
        BuildingInfo startBuilding = GetBuildingByName(startName);
        BuildingInfo endBuilding = GetBuildingByName(endName);

        if (startBuilding == null || endBuilding == null)
        {
            Debug.LogWarning($"Could not find start or end building. start={startName}, end={endName}");
            return null;
        }

        NavMeshPath path = new NavMeshPath();
        bool pathFound = NavMesh.CalculatePath(startBuilding.transform.position, endBuilding.transform.position, NavMesh.AllAreas, path);

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning("No complete path found on the NavMesh.");
            return null;
        }

        return path;
    }

    public IEnumerable<BuildingInfo> GetAllBuildings()
    {
        return buildingDict.Values.Distinct();
    }

    public List<string> GetSuggestions(string partial)
    {
        if (string.IsNullOrEmpty(partial))
            return new List<string>();

        return buildingDict.Keys
            .Where(name => name.IndexOf(partial, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }
}
