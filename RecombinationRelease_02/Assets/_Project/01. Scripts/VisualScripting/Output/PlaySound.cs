using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class PlaySound : ProcessBase
    {
        [SerializeField] private List<AudioSource> audioSources;
        [SerializeField] private AudioClip audioClip;

        private void Start()
        {
            if (audioClip != null)
            {
                foreach (var audioSource in audioSources)
                {
                    if (audioSource == null) continue;
                    audioSource.clip = audioClip;
                }
            }
        }

        public override void Execute()
        {
            //if (IsOn) return;

            if (audioSources.Count <= 0) return;

            foreach (var audioSource in audioSources)
            {
                if (audioSource == null) continue;
                audioSource.Play();
            }

            //IsOn = true;
        }
    }
}