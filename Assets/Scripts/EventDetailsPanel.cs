using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

public class EventDetailsPanel : MonoBehaviour
{
    public GameObject parent;
    public TMP_Text titleText;
    public TMP_Text timeText;
    public TMP_Text locationText;
    public TMP_Text descriptionText;

    public void ShowDetails(string title, string time, string location, string description)
    {
        titleText.text = title;
        timeText.text  = time;
        locationText.text = location;

        // Get the cleaned-up description
        string cleanedDescription = StripHtml(description);

        // If the cleaned description is empty or null, show a default message
        if (string.IsNullOrWhiteSpace(cleanedDescription))
        {
            cleanedDescription = "No description available for this event.";
        }

        // Finally assign to the descriptionText
        descriptionText.text = cleanedDescription;

        gameObject.SetActive(true);
    }

    private string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        // Replace <br/> with a newline (optional)
        string withNewlines = input.Replace("<br/>", "\n");

        // Remove remaining HTML tags
        string noHtml = Regex.Replace(withNewlines, "<.*?>", "");

        // Decode HTML entities (e.g. &nbsp; -> space)
        noHtml = WebUtility.HtmlDecode(noHtml);

        // Condense multiple whitespaces into one
        noHtml = Regex.Replace(noHtml, @"\s+", " ").Trim();
        
        // Remove anything after "Event type" (case-insensitive), if present
        int index = noHtml.IndexOf("Event type", System.StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            noHtml = noHtml.Substring(0, index).Trim();
        }

        return noHtml;
    }

    public void Hide()
    {
        parent.SetActive(false);
    }
}
