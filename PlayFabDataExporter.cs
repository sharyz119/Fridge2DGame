using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.IO;
using System;

public class PlayFabDataExporter : MonoBehaviour
{
    public static PlayFabDataExporter Instance { get; private set; }
    
    [Tooltip("Enable to automatically export data when the game quits")]
    public bool autoExportOnQuit = false;
    
    [Tooltip("Location to save export data (defaults to Application.persistentDataPath)")]
    public string exportPath = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Set default export path if none provided
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = Application.persistentDataPath;
        }
    }
    
    private void OnApplicationQuit()
    {
        if (autoExportOnQuit && PlayFabManager.Instance != null && PlayFabManager.Instance.IsInitialized)
        {
            Debug.Log("Auto-exporting PlayFab data on quit...");
            ExportAllData();
        }
    }
    
    // Public method to export all data at once
    public void ExportAllData()
    {
        ExportUserEvents();
        ExportUserStatistics();
        ExportUserData();
    }

    public void ExportAllPlayerData()
    {
        PlayFabClientAPI.GetAllUsersCharacters(new ListUsersCharactersRequest(), OnUserDataRetrieved, OnError);
    }

    // Export user data from PlayFab, including the ExperimentUserId
    public void ExportUserData()
    {
        PlayFabClientAPI.GetUserData(new PlayFab.ClientModels.GetUserDataRequest(),
        result =>
        {
            var userData = new Dictionary<string, object>();
            bool foundExperimentUserId = false;
            
            if (result.Data != null)
            {
                foreach (var item in result.Data)
                {
                    userData.Add(item.Key, item.Value.Value);
                    
                    if (item.Key == "ExperimentUserId")
                    {
                        foundExperimentUserId = true;
                        Debug.Log($"Found ExperimentUserId: {item.Value.Value}");
                    }
                }
            }
            
            if (!foundExperimentUserId)
            {
                Debug.Log("No ExperimentUserId found in PlayFab data. User may not have entered their ID yet.");
            }
            
            SaveToFile(userData, "UserData.json");
        },
        error =>
        {
            Debug.LogError($"Error retrieving user data: {error.ErrorMessage}");
        });
    }

    private void OnUserDataRetrieved(ListUsersCharactersResult result)
    {
        if (result.Characters == null || result.Characters.Count == 0)
        {
            Debug.Log("No user data found.");
            return;
        }

        var allData = new List<Dictionary<string, object>>();
        
        foreach (var character in result.Characters)
        {
            var data = new Dictionary<string, object>
            {
                { "CharacterId", character.CharacterId },
                { "CharacterName", character.CharacterName },
                { "CharacterType", character.CharacterType }
            };
            allData.Add(data);
        }

        SaveToFile(allData, "AllPlayerData.json");
    }

    public void ExportUserStatistics()
    {
        // Skip if statistics API is known to be disabled
        if (PlayFabManager.Instance != null && !PlayFabManager.Instance.IsStatisticsEnabled)
        {
            Debug.LogWarning("Statistics API is disabled. Skipping statistics export.");
            return;
        }
        
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(),
        result =>
        {
            var statistics = new Dictionary<string, object>();

            foreach (var stat in result.Statistics)
            {
                statistics.Add(stat.StatisticName, stat.Value);
            }

            SaveToFile(statistics, "UserStatistics.json");
        },
        error => 
        {
            // If the API is not enabled, don't treat as an error
            if (error.Error == PlayFabErrorCode.APINotEnabledForGameClientAccess)
            {
                Debug.LogWarning("Statistics API is not enabled. Statistics export skipped.");
            }
            else
            {
                OnError(error);
            }
        });
    }

    public void ExportUserEvents()
    {
        // Get the actual PlayFab ID from PlayFabManager
        string playFabId = "";
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsInitialized)
        {
            playFabId = PlayFabManager.Instance.PlayFabId;
        }
        
        if (string.IsNullOrEmpty(playFabId))
        {
            Debug.LogWarning("No PlayFab ID available. Using default Title ID.");
            playFabId = PlayFabSettings.TitleId; // Fallback to title ID
        }
        
        PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest()
        {
            PlayFabId = playFabId,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true,
                ShowStatistics = true
            }
        },
        result =>
        {
            var eventsData = new Dictionary<string, object>
            {
                { "PlayFabId", result.PlayerProfile.PlayerId },
                { "DisplayName", result.PlayerProfile.DisplayName },
                { "Statistics", result.PlayerProfile.Statistics }
            };

            SaveToFile(eventsData, "UserEvents.json");
        },
        OnError);
    }
    
    // Export event history if available
    public void ExportEventHistory()
    {
        // Export session information
        if (PlayFabManager.Instance != null)
        {
            var sessionInfo = new Dictionary<string, object>
            {
                { "SessionId", PlayFabManager.Instance.SessionId },
                { "UserId", PlayFabManager.Instance.UserId },
                { "SessionDuration", Time.realtimeSinceStartup }
            };
            
            SaveToFile(sessionInfo, "SessionInfo.json");
        }
    }

    public void SaveToFile(object data, string fileName)
    {
        try
        {
            // Create a safer filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
            
            // Replace Newtonsoft.Json serialization with Unity's JsonUtility
            string json = "";
            
            // Handle different data types
            if (data is Dictionary<string, object> dictObj)
            {
                // Convert dictionary to serializable class
                SerializableDictionary serDict = new SerializableDictionary();
                foreach (var kvp in dictObj)
                {
                    serDict.keys.Add(kvp.Key);
                    serDict.values.Add(kvp.Value != null ? kvp.Value.ToString() : "null");
                }
                json = JsonUtility.ToJson(serDict, true); // true for pretty print
            }
            else if (data is List<Dictionary<string, object>> listDict)
            {
                // Convert list of dictionaries to serializable class
                SerializableListOfDictionaries serList = new SerializableListOfDictionaries();
                foreach (var dict in listDict)
                {
                    SerializableDictionary serDict = new SerializableDictionary();
                    foreach (var kvp in dict)
                    {
                        serDict.keys.Add(kvp.Key);
                        serDict.values.Add(kvp.Value != null ? kvp.Value.ToString() : "null");
                    }
                    serList.items.Add(serDict);
                }
                json = JsonUtility.ToJson(serList, true);
            }
            else
            {
                // Try direct serialization for other types
                json = JsonUtility.ToJson(data, true);
            }
            
            string path = Path.Combine(exportPath, safeFileName);
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            
            File.WriteAllText(path, json);
            Debug.Log($"PlayFab data saved to: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving PlayFab data to file: {e.Message}");
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("Error exporting PlayFab data: " + error.GenerateErrorReport());
    }
    
    // WebGL-friendly method to generate a download link in browser
    #if UNITY_WEBGL && !UNITY_EDITOR
    public void ExportForWebGL(string dataName)
    {
        // Simplified export for WebGL
        Debug.Log($"WebGL export requested for: {dataName}");
        
        Dictionary<string, object> exportData = null;
        
        if (dataName == "statistics" && PlayFabManager.Instance != null)
        {
            exportData = new Dictionary<string, object>
            {
                { "UserId", PlayFabManager.Instance.UserId },
                { "SessionId", PlayFabManager.Instance.SessionId },
                { "SessionDuration", Time.realtimeSinceStartup }
            };
        }
        
        if (exportData != null)
        {
            // Convert to SerializableDictionary for Unity's JsonUtility
            SerializableDictionary serDict = new SerializableDictionary();
            foreach (var kvp in exportData)
            {
                serDict.keys.Add(kvp.Key);
                serDict.values.Add(kvp.Value != null ? kvp.Value.ToString() : "null");
            }
            string json = JsonUtility.ToJson(serDict, true);
            
            // In a real implementation, you would use a plugin like FileSaver.js 
            // to trigger a download in the browser
            Debug.Log($"WebGL export data: {json}");
        }
    }
    #endif
    
    // Serializable classes for Unity's JsonUtility
    [Serializable]
    private class SerializableDictionary
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();
    }
    
    [Serializable]
    private class SerializableListOfDictionaries
    {
        public List<SerializableDictionary> items = new List<SerializableDictionary>();
    }
}
