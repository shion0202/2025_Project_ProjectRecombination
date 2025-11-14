using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class PlaySoundSimple : ProcessBase
    {
        [SerializeField] private bool isLoop = false;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip audioClip;


        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
            }

            if (audioClip != null)
            {
                audioSource.clip = audioClip;
            }
        }
        public override void Execute()
        {
            if (!isLoop && IsOn) return;

            if (audioSource != null)
            {
                audioSource.Play();
            }

            if (!isLoop)
            {
                IsOn = true;
            }
        }
    }
}