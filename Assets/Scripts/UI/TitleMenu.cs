using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public GameObject mainMenuObject;
    public GameObject settingsMenuObject;

    Settings settings;

    [Header("Main menu UI")]
    public TextMeshProUGUI seedField;

    [Header("Settings menu UI")]
    public Slider viewDstSlider;
    public TextMeshProUGUI viewDstText;
    public Slider mouseSlider;
    public TextMeshProUGUI mouseSliderText;
    public Toggle threadingToggle;
    public Toggle chunkAnimationToggle;
    public TMP_Dropdown clouds;

    private void Awake()
    {
        if(!File.Exists(Application.dataPath + "/settings.cfg"))
        {
            Debug.Log("No settings file, creating one from scratch.");
            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        }
        else
        {
            Debug.Log("Loaded external settings file.");
            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
    }

    public void StartGame()
    {
        VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.WorldSizeInChunks;
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void EnterSettings()
    {
        viewDstSlider.value = settings.viewDistance;
        UpdateViewDstSlider();

        mouseSlider.value = settings.mouseSensitivity;
        UpdateMouseSlider();

        clouds.value = (int)settings.cloudStyle;

        threadingToggle.isOn = settings.enableThreading;
        chunkAnimationToggle.isOn = settings.enableAnimatedChunkLoading;

        mainMenuObject.SetActive(false);
        settingsMenuObject.SetActive(true);
    }
    
    public void LeaveSettings()
    {
        settings.viewDistance = (int)viewDstSlider.value;
        settings.mouseSensitivity = mouseSlider.value;
        settings.enableThreading = threadingToggle.isOn;
        settings.enableAnimatedChunkLoading = chunkAnimationToggle.isOn;
        settings.cloudStyle = (CloudStyle)clouds.value;

        string jsonExport = JsonUtility.ToJson(settings);
        Debug.Log(jsonExport);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        mainMenuObject.SetActive(true);
        settingsMenuObject.SetActive(false);
    }

    public void UpdateViewDstSlider()
    {
        viewDstText.text = $"View distance: {viewDstSlider.value}";
    }

    public void UpdateMouseSlider()
    {
        mouseSliderText.text = $"Mouse sensitivity: {mouseSlider.value:F1}";
    }
}
