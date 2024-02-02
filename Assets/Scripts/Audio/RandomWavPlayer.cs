using UnityEngine;
using System.Collections.Generic;

namespace doppelganger
{
    public class RandomWavPlayer : MonoBehaviour
    {
        private List<AudioClip> wavFiles;
        private AudioSource audioSource;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            LoadWavFiles();
            PlayRandomWavFile();
        }

        void LoadWavFiles()
        {
            wavFiles = new List<AudioClip>(Resources.LoadAll<AudioClip>("Audio"));
        }

        void PlayRandomWavFile()
        {
            if (wavFiles.Count == 0) return;

            int randomIndex = Random.Range(0, wavFiles.Count);
            audioSource.clip = wavFiles[randomIndex];
            audioSource.Play();
        }
    }
}