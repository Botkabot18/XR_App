using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SequentialAudioPlayer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Drag your audio clips here in the order you want them played.")]
    public List<AudioClip> audioClips;

    [Tooltip("Should the sequence start automatically when the game begins?")]
    public bool playOnStart = true;

    private AudioSource audioSource;

    void Awake()
    {
        // Automatically find the Audio Source on this object
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (playOnStart)
        {
            PlaySequence();
        }
    }

    // Call this function to start the playlist
    public void PlaySequence()
    {
        if (audioClips.Count > 0)
        {
            StartCoroutine(PlayAudioRoutine());
        }
        else
        {
            Debug.LogWarning("SequentialAudioPlayer: No clips assigned in the list!");
        }
    }

    // The Coroutine that handles the timing
    IEnumerator PlayAudioRoutine()
    {
        foreach (AudioClip clip in audioClips)
        {
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();

                // Wait exactly as long as the clip's duration before moving to the next
                yield return new WaitForSeconds(clip.length);
            }
        }

        Debug.Log("Audio sequence finished.");
    }

    // Optional: Call this to stop the sequence early
    public void StopSequence()
    {
        StopAllCoroutines();
        audioSource.Stop();
    }
}