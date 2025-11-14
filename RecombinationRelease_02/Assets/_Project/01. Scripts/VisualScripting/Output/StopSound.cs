using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class StopSound : ProcessBase
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float fadeTime = 1.0f;
        [SerializeField] private bool isFade = false;

        public override void Execute()
        {
            if (IsOn) return;
            if (audioSource == null) return;

            if (isFade)
            {
                StartCoroutine(FadeOut(audioSource, fadeTime));
            }
            else
            {
                audioSource.Stop();
            }

            IsOn = true;
        }

        private IEnumerator FadeOut(AudioSource audioSource, float fadeTime)
        {
            float startVolume = audioSource.volume;

            // volume이 0이 될 때까지 서서히 감소
            while (audioSource.volume > 0)
            {
                audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume; // 다음 재생 대비 초기화
        }
    }
}