using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class WeatherFetcher : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text weatherText;
    public Image weatherIcon; // Assign in Inspector

    [Header("Weather Settings")]
    public float latitude = 34.033f;
    public float longitude = -118.692f;

    [Header("Weather Icons")]
    public Sprite clearSkyIcon;
    public Sprite partlyCloudyIcon;
    public Sprite overcastIcon;
    public Sprite rainIcon;
    public Sprite snowIcon;
    public Sprite thunderstormIcon;
    public Sprite unknownIcon;

    void Start()
    {
        StartCoroutine(GetWeather());
    }

    IEnumerator GetWeather()
    {
        string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            weatherText.text = "Error fetching weather: " + request.error;
        }
        else
        {
            WeatherResponse response = JsonUtility.FromJson<WeatherResponse>(request.downloadHandler.text);
            if (response != null && response.current_weather != null)
            {
                int code = response.current_weather.weathercode;
                string condition = GetWeatherDescription(code);
                int tempF = Mathf.RoundToInt(response.current_weather.temperature * 9f / 5f + 32f);
                weatherText.text = $"{tempF}°F\n{condition}";
                weatherIcon.sprite = GetWeatherIcon(code);
            }
            else
            {
                weatherText.text = "Error parsing weather data.";
                weatherIcon.sprite = unknownIcon;
            }
        }
    }

    string GetWeatherDescription(int code)
    {
        switch (code)
        {
            case 0: return "Clear sky";
            case 1: return "Mostly clear";
            case 2: return "Partly cloudy";
            case 3: return "Overcast";
            case 45:
            case 48: return "Fog";
            case 51:
            case 53:
            case 55: return "Drizzle";
            case 56:
            case 57: return "Freezing drizzle";
            case 61:
            case 63:
            case 65: return "Rain";
            case 66:
            case 67: return "Freezing rain";
            case 71:
            case 73:
            case 75: return "Snow fall";
            case 77: return "Snow grains";
            case 80:
            case 81:
            case 82: return "Rain showers";
            case 85:
            case 86: return "Snow showers";
            case 95: return "Thunderstorm";
            case 96:
            case 99: return "Thunderstorm with hail";
            default: return "Unknown";
        }
    }

    Sprite GetWeatherIcon(int code)
    {
        switch (code)
        {
            case 0:
            case 1: return clearSkyIcon;
            case 2: return partlyCloudyIcon;
            case 3: return overcastIcon;
            case 61:
            case 63:
            case 65:
            case 80:
            case 81:
            case 82: return rainIcon;
            case 71:
            case 73:
            case 75:
            case 85:
            case 86: return snowIcon;
            case 95:
            case 96:
            case 99: return thunderstormIcon;
            default: return unknownIcon;
        }
    }
}

[System.Serializable]
public class WeatherResponse
{
    public CurrentWeather current_weather;
}

[System.Serializable]
public class CurrentWeather
{
    public float temperature;   // Temperature in Celsius
    public float windspeed;     // Windspeed in km/h
    public int weathercode;     // Weather condition code
    public string time;         // Timestamp of the data
}