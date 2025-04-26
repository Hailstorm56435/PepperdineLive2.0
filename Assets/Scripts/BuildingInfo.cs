using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildingInfo : MonoBehaviour
{
    [Header("Main/Canonical Name")]
    [SerializeField] private string canonicalName;

    [Header("Aliases")]
    [SerializeField] private List<string> aliases = new();

    public string CanonicalName =>
        canonicalName.StartsWith("the ", System.StringComparison.OrdinalIgnoreCase)
            ? canonicalName.Substring(4).Trim()
            : canonicalName;

    public IReadOnlyList<string> Aliases =>
        aliases.Select(alias =>
            alias.StartsWith("the ", System.StringComparison.OrdinalIgnoreCase)
                ? alias.Substring(4).Trim()
                : alias
        ).ToList();

    [Header("Building Settings")]
    public bool animateFloors = false;
    public bool isRoom = true;
    public int targetFloor;

    [TextArea(2, 5)]
[SerializeField] private string tooltip;
public string Tooltip => tooltip;


    [SerializeField] private Animator animator;
    [SerializeField] private BuildingFloorController buildingFloorController;

    [Header("Floating Popup Settings")]
    public Vector3 popupOffset = Vector3.zero;
    public float popupFontSize = 0f;

    private Collider buildingCollider;
    private Renderer buildingRenderer;
    private Material[] originalMaterials;

    private void Awake()
    {
        if (buildingFloorController == null)
            buildingFloorController = Object.FindFirstObjectByType<BuildingFloorController>();

        buildingCollider = GetComponent<Collider>();
        buildingRenderer = GetComponent<Renderer>();
        if (buildingRenderer != null)
            originalMaterials = buildingRenderer.materials;
    }

    public void OnFocused()
    {
        if (animateFloors && buildingFloorController != null)
        {
            buildingFloorController.SetFocusedBuilding(this);
        }
        else
        {
            Debug.Log("OnFocused: standard building or no controller assigned.");
        }

        if (buildingCollider != null)
            buildingCollider.enabled = false;
    }

    public void OnFocusLost()
    {
        if (animateFloors && buildingFloorController != null)
            buildingFloorController.ResetAnimation();

        if (buildingCollider != null)
            buildingCollider.enabled = true;
    }

    public Animator GetAnimator() => animator;

    public void SetHighlight(Material highlightMaterial)
    {
        if (buildingRenderer != null && highlightMaterial != null)
        {
            var mats = new Material[buildingRenderer.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = highlightMaterial;
            buildingRenderer.materials = mats;
        }
    }

    public void RemoveHighlight()
    {
        if (buildingRenderer != null && originalMaterials != null)
            buildingRenderer.materials = originalMaterials;
    }
}
