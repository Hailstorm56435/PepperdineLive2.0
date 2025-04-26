using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.EventSystems; // for EventSystem checks if needed

public class EventScraperUIGrouped : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Enable heavy debug logs to trace logic in the Console.")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("RSS URL")]
    [SerializeField] private string rssUrl = "https://www.trumba.com/calendars/pepperdine-university.rss";

    [Header("UI References")]
    public TMP_Text eventsText;
    public EventDetailsPanel detailsPanel;
    [Tooltip("RectTransform of the panel (parent) you want to hide if clicked outside.")]
    public RectTransform detailsPanelRect;

    [Header("Building Lookup")]
    public BuildingLookupManager buildingLookupManager;

    [Header("Directions UI")]
    [Tooltip("Reference to the DirectionsUI component that handles the 'TO' field.")]
    public DirectionsUI directionsUI;

    // Internal dictionaries to keep track of events and building links.
    private Dictionary<string, EventInfo> eventLookup = new Dictionary<string, EventInfo>();
    // We store the actual BuildingInfo object rather than its name.
    private Dictionary<string, BuildingInfo> locationLookupBuilding = new Dictionary<string, BuildingInfo>();

    // Flag used to mark that the current click was consumed by a link.
    private bool linkClickConsumed = false;

    [Serializable]
    public class EventInfo
    {
        public string Title;
        public string Description;
        public string PubDate;
        public string Location;
        public DateTime DtStart;
        public DateTime DtEnd;
    }

    void Start()
    {
        // Auto-assign components if not already set.
        if (buildingLookupManager == null)
            buildingLookupManager = UnityEngine.Object.FindFirstObjectByType<BuildingLookupManager>();
        if (directionsUI == null)
            directionsUI = UnityEngine.Object.FindFirstObjectByType<DirectionsUI>();

        if (enableDebugLogs)
            Debug.Log("[EventScraper] Starting to fetch RSS feed from: " + rssUrl);

        StartCoroutine(FetchAndParseRSS());
    }

    void Update()
    {
        // Check for a mouse click.
        if (Input.GetMouseButtonDown(0))
        {
            // If a link click was just handled, skip the outside-panel logic this frame.
            if (linkClickConsumed)
            {
                if (enableDebugLogs)
                    Debug.Log("[EventScraper] Click was consumed by a link. Skipping outside-panel check.");
                linkClickConsumed = false; // Reset flag for the next click
                return;
            }
            
            // Close the details panel if the click is outside its designated Rect.
            if (detailsPanelRect != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(detailsPanelRect, Input.mousePosition))
            {
                if (detailsPanel != null && detailsPanel.transform.parent != null)
                {
                    if (enableDebugLogs)
                        Debug.Log("[EventScraper] Click detected outside details panel. Hiding details panel.");
                    detailsPanel.transform.parent.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Called by the clickable text handler when a link is clicked.
    /// </summary>
    public void HandleLinkClick(string linkID)
    {
        // Mark that a link click was consumed.
        linkClickConsumed = true;
        if (enableDebugLogs)
            Debug.Log("[EventScraper] Link clicked: " + linkID);

        // Process event title links.
        if (linkID.StartsWith("event_"))
        {
            if (TryGetEventInfo(linkID, out EventInfo ev))
            {
                if (enableDebugLogs)
                    Debug.Log("[EventScraper] Event link clicked: " + ev.Title);
                ShowEventPopup(ev);
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EventScraper] No event found for link: " + linkID);
            }
        }
        // Process building location links.
        else if (linkID.StartsWith("location_"))
        {
            if (locationLookupBuilding.TryGetValue(linkID, out BuildingInfo building))
            {
                if (enableDebugLogs)
                    Debug.Log("[EventScraper] Building link clicked: " + building.CanonicalName);
                buildingLookupManager.FocusBuilding(building);
                if (directionsUI != null)
                    directionsUI.SetToBuilding(building);
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EventScraper] No building found for link: " + linkID);
            }
        }
    }

    IEnumerator FetchAndParseRSS()
    {
        if (enableDebugLogs)
            Debug.Log("[EventScraper] Fetching RSS feed from: " + rssUrl);

        UnityWebRequest request = UnityWebRequest.Get(rssUrl);
        request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[EventScraper] Error fetching RSS feed: " + request.error);
            yield break;
        }

        string xmlData = request.downloadHandler.text.Trim();
        // Remove BOM if present.
        if (xmlData.Length > 0 && xmlData[0] == '\uFEFF')
            xmlData = xmlData.Substring(1);

        XmlDocument xmlDoc = new XmlDocument();
        try
        {
            xmlDoc.LoadXml(xmlData);
        }
        catch (Exception e)
        {
            Debug.LogError("[EventScraper] Error parsing XML: " + e.Message);
            yield break;
        }

        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        nsManager.AddNamespace("xCal", "urn:ietf:params:xml:ns:xcal");
        nsManager.AddNamespace("x-trumba", "http://schemas.trumba.com/rss/x-trumba");
        nsManager.AddNamespace("x-microsoft", "http://schemas.microsoft.com/x-microsoft");

        XmlNodeList itemNodes = xmlDoc.GetElementsByTagName("item");
        if (itemNodes.Count == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[EventScraper] No event items found in the RSS feed.");
        }

        List<EventInfo> events = new List<EventInfo>();

        foreach (XmlNode item in itemNodes)
        {
            string title = GetNodeText(item, "title", nsManager);
            if (title.ToLower().Contains("cancelled"))
                continue;

            string rawDescription = GetNodeText(item, "description", nsManager);
            string description = ExtractEventDetails(rawDescription);

            string pubDate = GetNodeText(item, "pubDate", nsManager);
            string location = GetNodeText(item, "location", nsManager);
            string dtstartStr = GetNodeText(item, "dtstart", nsManager);
            string dtendStr = GetNodeText(item, "dtend", nsManager);

            if (!DateTime.TryParse(dtstartStr, out DateTime dtstart))
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EventScraper] Could not parse dtstart: " + dtstartStr);
                continue;
            }
            DateTime.TryParse(dtendStr, out DateTime dtend);

            // Only include future events.
            if (dtstart.ToLocalTime() <= DateTime.Now)
            {
                if (enableDebugLogs)
                    Debug.Log("[EventScraper] Skipping past event: " + title);
                continue;
            }

            events.Add(new EventInfo
            {
                Title = title,
                Description = description,
                PubDate = pubDate,
                Location = location,
                DtStart = dtstart,
                DtEnd = dtend
            });
        }

        // Sort events by start date.
        events.Sort((a, b) => a.DtStart.CompareTo(b.DtStart));

        // Group events by date.
        var groupedEvents = new SortedDictionary<DateTime, List<EventInfo>>();
        foreach (EventInfo ev in events)
        {
            DateTime dateKey = ev.DtStart.ToLocalTime().Date;
            if (!groupedEvents.ContainsKey(dateKey))
                groupedEvents[dateKey] = new List<EventInfo>();
            groupedEvents[dateKey].Add(ev);
        }

        int eventIndex = 0;
        string output = "";
        foreach (var kvp in groupedEvents)
        {
            output += $"<b>{kvp.Key:dddd, MMMM dd, yyyy}:</b>\n\n";
            foreach (var ev in kvp.Value)
            {
                // Create a unique link ID for the event title.
                string eventLinkID = "event_" + eventIndex;
                eventLookup[eventLinkID] = ev;

                // Format event time.
                string timeStr = ev.DtStart.ToLocalTime().ToString("h:mm tt");

                // Make the event title clickable.
                string titleLink = $"<link=\"{eventLinkID}\"><u><color=#0000FF><b>{ev.Title}</b></color></u></link>";

                // Process location text and attempt to turn part of it into a clickable link.
                string locationDisplay = ev.Location;
                if (buildingLookupManager != null && !string.IsNullOrEmpty(ev.Location))
                {
                    int bestMatchStart = -1;
                    int bestMatchLength = 0;
                    bool bestExtended = false;
                    BuildingInfo bestBuilding = null;
                    string bestCandidate = "";

                    // Search both canonical name and aliases.
                    foreach (var building in buildingLookupManager.GetAllBuildings())
                    {
                        var candidates = new List<string>();
                        if (!string.IsNullOrEmpty(building.CanonicalName))
                            candidates.Add(building.CanonicalName);
                        if (building.Aliases != null)
                            candidates.AddRange(building.Aliases);

                        foreach (string candidate in candidates)
                        {
                            // Only match whole words (e.g. " AC " but not "act").
                            string pattern = $@"\b{Regex.Escape(candidate)}\b";
                            Match matchCandidate = Regex.Match(ev.Location, pattern, RegexOptions.IgnoreCase);
                            int index = matchCandidate.Success ? matchCandidate.Index : -1;

                            if (enableDebugLogs)
                                Debug.Log($"[EventScraper] Checking candidate '{candidate}' in location '{ev.Location}' => regex match index: {index}");

                            if (matchCandidate.Success)
                            {
                                int matchEnd = index + matchCandidate.Length;
                                // Extend match to include trailing spaces and digits.
                                while (matchEnd < ev.Location.Length && char.IsWhiteSpace(ev.Location[matchEnd]))
                                    matchEnd++;
                                while (matchEnd < ev.Location.Length && char.IsDigit(ev.Location[matchEnd]))
                                    matchEnd++;

                                int matchLength = matchEnd - index;
                                bool extended = matchLength > matchCandidate.Length;

                                if (bestMatchStart == -1 ||
                                    (extended && !bestExtended) ||
                                    (extended == bestExtended && matchLength > bestMatchLength))
                                {
                                    bestMatchStart = index;
                                    bestMatchLength = matchLength;
                                    bestExtended = extended;
                                    bestBuilding = building;
                                    bestCandidate = ev.Location.Substring(index, matchLength);
                                    if (enableDebugLogs)
                                        Debug.Log($"[EventScraper] New best candidate: '{bestCandidate}' for building '{building.CanonicalName}'");
                                }
                            }
                        }
                    }

                    // If a match was found, inject a clickable link into the location text.
                    if (bestMatchStart != -1 && bestBuilding != null)
                    {
                        string before = ev.Location.Substring(0, bestMatchStart);
                        string clickable = $"<link=\"location_{eventIndex}\"><u><color=#0000FF>{bestCandidate}</color></u></link>";
                        string after = ev.Location.Substring(bestMatchStart + bestMatchLength);
                        locationDisplay = before + clickable + after;

                        // Store the BuildingInfo reference for later lookup.
                        locationLookupBuilding["location_" + eventIndex] = bestBuilding;
                        if (enableDebugLogs)
                            Debug.Log($"[EventScraper] Matched building '{bestBuilding.CanonicalName}' using candidate '{bestCandidate}' in location '{ev.Location}'");
                    }
                    else if (enableDebugLogs)
                    {
                        Debug.Log($"[EventScraper] No building match found in location '{ev.Location}'");
                    }
                }

                output += $"{titleLink}\n{timeStr}\n{locationDisplay}\n\n";
                eventIndex++;
            }
            output += "====================\n\n";
        }

        if (eventsText != null)
            eventsText.text = output;
        else
            Debug.LogWarning("[EventScraper] TextMeshPro component is not assigned!");
    }

    /// <summary>
    /// Helper method to retrieve text from an XML node with the given local name.
    /// </summary>
    private string GetNodeText(XmlNode parent, string localName, XmlNamespaceManager nsManager)
    {
        XmlNode node = parent.SelectSingleNode($"*[local-name()='{localName}']", nsManager);
        return node != null ? node.InnerText : "";
    }

    /// <summary>
    /// Extract event details by stripping out timezone strings and extra text.
    /// </summary>
    private string ExtractEventDetails(string rawDescription)
    {
        if (string.IsNullOrEmpty(rawDescription))
            return "";

        string details = rawDescription.Trim();
        Regex tzRegex = new Regex(@"\b(?:PST|PDT|EST|EDT|CST|CDT|MST|MDT)\b", RegexOptions.IgnoreCase);
        Match tzMatch = tzRegex.Match(details);
        if (tzMatch.Success)
            details = details.Substring(tzMatch.Index + tzMatch.Length).Trim();

        int markerIndex = details.IndexOf("Event type:", StringComparison.OrdinalIgnoreCase);
        if (markerIndex != -1)
            details = details.Substring(0, markerIndex).Trim();

        return details;
    }

    /// <summary>
    /// Attempt to look up event info associated with the linkID.
    /// </summary>
    public bool TryGetEventInfo(string linkID, out EventInfo ev)
    {
        return eventLookup.TryGetValue(linkID, out ev);
    }

    /// <summary>
    /// Display the event popup using the details panel.
    /// </summary>
    public void ShowEventPopup(EventInfo ev)
    {
        if (detailsPanel == null)
            return;

        string timeStr = ev.DtStart.ToLocalTime().ToString("f");
        detailsPanel.ShowDetails(ev.Title, timeStr, ev.Location, ev.Description);

        if (detailsPanel.transform.parent != null)
            detailsPanel.transform.parent.gameObject.SetActive(true);
    }
}
