using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("------------------AudioMixers-----------------------")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("------------------AudioSource-----------------------")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] public AudioSource ambientSource;

    [Header("----------------AudioClip--------------------------")]
    [SerializeField] private AudioClip mainTheme;

    [Space]
    [SerializeField] private List<AudioClip> sfxClip;
    [SerializeField] private List<AudioClip> ambientClip;

    [Header("------------------UI-------------------------------")]
    [SerializeField] private Animator volumePanel;

    [SerializeField] private float uiCloseDelay = 0.583f;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    public static AudioManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadAudioSettings();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        LoadAudioSettings();
        InitializeSliders();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedVolumes();

        InitializeSliders();

        PlayBGM(mainTheme);
        PlayAmbient(0);
    }

    private void OnApplicationQuit()
    {
        SaveAudioSettings();
    }

    public void PlayAmbient(int clipIndex)
    {
        if (ambientSource.clip == ambientClip[clipIndex] && ambientSource.isPlaying) return;

        ambientSource.Stop();
        ambientSource.clip = ambientClip[clipIndex];
        ambientSource.Play();
    }

    private void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(int clipIndex)
    {
        sfxSource.PlayOneShot(sfxClip[clipIndex]);
    }

    public void SetVolume(string groupName, float volume)
    {
        if (volume == 0)
        {
            audioMixer.SetFloat(groupName, -80f);
        }
        else
        {
            audioMixer.SetFloat(groupName, Mathf.Log10(volume) * 20);
        }
    }

    private void LoadAudioSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat("Master", 1f);
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.3f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.3f);

        SetVolume("Master", masterVolume);
        SetVolume("BGMVolume", bgmVolume);
        SetVolume("SFXVolume", sfxVolume);

        if (masterSlider != null) masterSlider.value = masterVolume;
        if (bgmSlider != null) bgmSlider.value = bgmVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
    }

    private void ApplySavedVolumes()
    {
        float masterVolume = PlayerPrefs.GetFloat("Master", 1f);
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.3f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.3f);

        SetVolume("Master", masterVolume);
        SetVolume("BGMVolume", bgmVolume);
        SetVolume("SFXVolume", sfxVolume);

        if (masterSlider != null) masterSlider.value = masterVolume;
        if (bgmSlider != null) bgmSlider.value = bgmVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
    }

    public void SaveAudioSettings()
    {
        float masterVolume = masterSlider != null ? masterSlider.value : PlayerPrefs.GetFloat("Master", 1f);
        float bgmVolume = bgmSlider != null ? bgmSlider.value : PlayerPrefs.GetFloat("BGMVolume", 0.3f);
        float sfxVolume = sfxSlider != null ? sfxSlider.value : PlayerPrefs.GetFloat("SFXVolume", 0.3f);

        PlayerPrefs.SetFloat("Master", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    private void InitializeSliders()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(value =>
            {
                SetVolume("Master", value);
                SaveAudioSettings();
            });
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(value =>
            {
                SetVolume("BGMVolume", value);
                SaveAudioSettings();
            });
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(value =>
            {
                SetVolume("SFXVolume", value);
                SaveAudioSettings();
            });
        }
    }

    public void OpenVolumeUI(bool isOpen = true)
    {
        PlaySFX(5);
        
        if (!isOpen) StartCoroutine(CloseVolumeUIDelay());
        else volumePanel.gameObject.SetActive(isOpen);
        
        Debug.Log($"isOpen:{isOpen}");
        volumePanel.SetBool("isOpen",isOpen);
    }

    private IEnumerator CloseVolumeUIDelay()
    {
        yield return new WaitForSeconds(uiCloseDelay);
        volumePanel.gameObject.SetActive(false);
    }
}
