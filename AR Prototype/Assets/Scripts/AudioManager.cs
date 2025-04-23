using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip buttonAudio;
    [SerializeField] private AudioClip winAudio;
    [SerializeField] private AudioClip music;

    public static AudioManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            PlayMusic();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayButton()
    {
        audioSource.PlayOneShot(buttonAudio);
    }

    public void PlayWinAudio()
    {
        audioSource.PlayOneShot(winAudio);
    }

    public void PlayMusic()
    {
        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.Play();
    }
}
