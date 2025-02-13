using UnityEngine;
using System;

public class UniqueIDManager : MonoBehaviour
{
    private const string UniqueIDKey = "UserUniqueID";

    public string GetUniqueDeviceID()
    {
        if (!PlayerPrefs.HasKey(UniqueIDKey))
        {
            string newUUID = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(UniqueIDKey, newUUID);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetString(UniqueIDKey);
    }

    void Start()
    {
        string uniqueID = GetUniqueDeviceID();
        Debug.Log("User Unique ID: " + uniqueID);
    }
}
