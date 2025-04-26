using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class ClickableText : MonoBehaviour, IPointerClickHandler
{
    public bool debug = false;
    private TMP_Text textMesh;
    public EventScraperUIGrouped eventScraper; // assign this reference in the Inspector

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMesh, eventData.position, null);
        if (debug)
        {
            Debug.Log("Link index: " + linkIndex);
        }
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = textMesh.textInfo.linkInfo[linkIndex];
            string linkID = linkInfo.GetLinkID();
            if (debug)
            {
                Debug.Log("Clicked link ID: " + linkID);
            }
            if (eventScraper != null)
            {
                eventScraper.HandleLinkClick(linkID);
            }
            else if (debug)
            {
                Debug.Log("EventScraper reference is null");
            }
        }
    }
}
