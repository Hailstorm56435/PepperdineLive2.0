using UnityEngine;
using UnityEngine.UI;        // For Button
using UnityEngine.AI;       // For NavMeshPath
using TMPro;                // For TMP_InputField

public class DirectionsUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your BuildingLookupManager here. If left empty, it will be found at runtime.")]
    public BuildingLookupManager buildingLookupManager;

    [Tooltip("TMP Input field for the 'From' building name.")]
    public TMP_InputField fromInputField;

    [Tooltip("TMP Input field for the 'To' building name.")]
    public TMP_InputField toInputField;

    [Tooltip("Button used to get directions. (Optional if you wire it up via Inspector events.)")]
    public Button getDirectionsButton;

    [Tooltip("LineRenderer used to visualize the path.")]
    public LineRenderer pathLineRenderer;

    [Header("Path Visualization")]
    [Tooltip("How high above the terrain each corner of the path should be.")]
    public float yOffset = 2f;

    private void Start()
    {
        // Attempt to assign BuildingLookupManager if not set.
        if (buildingLookupManager == null)
        {
            buildingLookupManager = UnityEngine.Object.FindFirstObjectByType<BuildingLookupManager>();
            if (buildingLookupManager != null)
                Debug.Log("Found BuildingLookupManager: " + buildingLookupManager.name);
            else
                Debug.LogWarning("BuildingLookupManager not found in the scene.");
        }

        if (getDirectionsButton != null)
        {
            getDirectionsButton.onClick.AddListener(OnGetDirectionsButtonClicked);
        }
        else
        {
            Debug.LogWarning("No button reference assigned. You can wire up OnGetDirectionsButtonClicked in the Inspector.");
        }
        
        // Subscribe to the OnBuildingFocused event.
        if (buildingLookupManager != null)
        {
            buildingLookupManager.OnBuildingFocused += SetToBuilding;
            Debug.Log("Subscribed to OnBuildingFocused event in DirectionsUI.");
        }
        else
        {
            Debug.LogWarning("BuildingLookupManager is not assigned in DirectionsUI.");
        }
    }

    /// <summary>
    /// Public method to update the "TO" TMP input field with the building's canonical name.
    /// This method can be called externally (e.g., from EventScraper) or via event subscription.
    /// </summary>
    public void SetToBuilding(BuildingInfo building)
    {
        if (toInputField != null && building != null)
        {
            toInputField.text = building.CanonicalName;
            Debug.Log("SetToBuilding called. Updated TO field to: " + building.CanonicalName);
        }
        else
        {
            Debug.LogWarning("SetToBuilding could not update the field (toInputField or building is null).");
        }
    }

    /// <summary>
    /// Called when the "Get Directions" button is clicked.
    /// Calculates and visualizes a NavMesh path between the "From" and "TO" buildings.
    /// </summary>
    public void OnGetDirectionsButtonClicked()
    {
        if (buildingLookupManager == null)
        {
            Debug.LogWarning("No BuildingLookupManager assigned.");
            return;
        }
        
        string fromName = (fromInputField != null) ? fromInputField.text.Trim() : "";
        string toName   = (toInputField != null) ? toInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(fromName) || string.IsNullOrEmpty(toName))
        {
            Debug.LogWarning("Please enter both 'From' and 'To' building names.");
            return;
        }

        BuildingInfo fromBuilding = buildingLookupManager.GetBuildingByName(fromName);
        BuildingInfo toBuilding   = buildingLookupManager.GetBuildingByName(toName);

        if (fromBuilding == null || toBuilding == null)
        {
            Debug.LogWarning($"Invalid building name(s). From: '{fromName}', To: '{toName}'");
            return;
        }

        NavMeshPath navPath = buildingLookupManager.FindNavMeshPathBetweenBuildings(fromName, toName);
        if (navPath == null)
        {
            Debug.LogWarning($"No path found on the NavMesh between '{fromName}' and '{toName}'.");
            if (pathLineRenderer != null)
                pathLineRenderer.positionCount = 0;
            return;
        }

        if (pathLineRenderer != null)
        {
            Vector3[] corners = navPath.corners;
            pathLineRenderer.positionCount = corners.Length;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 cornerPos = corners[i];
                cornerPos.y += yOffset;
                pathLineRenderer.SetPosition(i, cornerPos);
            }
        }
        else
        {
            Debug.LogWarning("No LineRenderer assigned for path visualization.");
        }
    }
}
