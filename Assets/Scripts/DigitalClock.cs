using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class DigitalClock : MonoBehaviour
{
    public TMP_Text clockText;

    void Start()
    {
        StartCoroutine(UpdateClock());
    }

    IEnumerator UpdateClock()
    {
        while (true)
        {
            DateTime now = DateTime.Now;
            clockText.text = now.ToString("MMMM dd") + "\n" + now.ToString("h:mm tt");

            // Calculate the seconds remaining until the start of the next minute
            int secondsUntilNextMinute = 60 - now.Second;
            yield return new WaitForSeconds(secondsUntilNextMinute);
        }
    }
}
