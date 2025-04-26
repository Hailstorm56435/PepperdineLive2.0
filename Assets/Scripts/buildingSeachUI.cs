using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BuildingSearchUI : MonoBehaviour
{
    public BuildingLookupManager buildingLookupManager;
    public TMP_InputField searchInput;
    public Button searchButton;

    [Header("Suggestions Panel Setup")]
    public RectTransform suggestionsPanel;
    public GameObject suggestionItemPrefab;

    public TextMeshProUGUI feedbackText;

    private void Start()
    {
        if (searchButton != null)
            searchButton.onClick.AddListener(OnSearchClicked);

        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnInputChanged);
            searchInput.onSubmit.AddListener(OnInputSubmit);
        }

        if (suggestionsPanel != null)
            suggestionsPanel.gameObject.SetActive(false);
    }

    private void OnInputChanged(string currentText)
    {
        UpdateSuggestions(currentText);
    }

    private void UpdateSuggestions(string input)
    {
        // Clear existing suggestions.
        foreach (Transform child in suggestionsPanel)
        {
            Destroy(child.gameObject);
        }

        if (string.IsNullOrEmpty(input))
        {
            suggestionsPanel.gameObject.SetActive(false);
            return;
        }

        // Get the raw suggestions from your manager.
        List<string> allSuggestions = buildingLookupManager.GetSuggestions(input);

        // Filter and limit to 6.
        List<string> filteredSuggestions = allSuggestions
            .Where(s => s.StartsWith(input, System.StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();

        if (filteredSuggestions.Count == 0)
        {
            // No matching suggestions, hide the panel.
            suggestionsPanel.gameObject.SetActive(false);
            return;
        }

        // Instantiate each suggestion item.
        foreach (string suggestion in filteredSuggestions)
        {
            GameObject item = Instantiate(suggestionItemPrefab, suggestionsPanel);
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = suggestion;

            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                string capturedSuggestion = suggestion;
                btn.onClick.AddListener(() => OnSuggestionClicked(capturedSuggestion));
            }
        }

        // Show the suggestions panel.
        suggestionsPanel.gameObject.SetActive(true);
    }

    private void OnSuggestionClicked(string suggestion)
    {
        searchInput.text = suggestion;
        suggestionsPanel.gameObject.SetActive(false);

        // Optionally reset focus and caret.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(searchInput.gameObject);
            searchInput.MoveTextEnd(false);
        }
    }

    private void OnInputSubmit(string input)
    {
        OnSearchClicked();
    }

    private void OnSearchClicked()
    {
        suggestionsPanel.gameObject.SetActive(false);

        string query = searchInput.text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            if (feedbackText != null)
                feedbackText.text = "Please enter a location name.";
            return;
        }

        var building = buildingLookupManager.GetBuildingByName(query);
        if (building != null)
        {
            if (feedbackText != null)
                feedbackText.text = "";
            buildingLookupManager.FocusBuilding(building);
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = $"No location found for '{query}'";
        }
    }
}
