using NUnit.Framework;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class GUIManager : Singleton<GUIManager>
    {
        [Header("Image Settings")] [SerializeField]
        private Image crosshead; // 조준점 이미지         

        [SerializeField] private Image fadeInOutPanel; // 카메라 연출 이미지

        [Header("Test Part System UI")] [SerializeField]
        private TextMeshProUGUI testPartSystemUILeft;

        [SerializeField] private TextMeshProUGUI testPartSystemUIRight;
        [SerializeField] private TextMeshProUGUI testPartSystemUILegs;

        [Header("Test HP Bar UI")] [SerializeField]
        private Slider testHpBar;

        [Header("Test GameOver UI")] [SerializeField]
        private Image testGameOverPanel;

        private Coroutine _runningFadeCoroutine; // 카메라 연출 코루틴 저장

        [Header("Test Crosshair UI")]
        [SerializeField] private Slider ammoLeftBar;
        [SerializeField] private Slider ammoRightBar;

        [Header("Test Skill UI")]
        [SerializeField] private Image legsSkillCooldownImage;
        [SerializeField] private Image backSkillCooldownImage;
        [SerializeField] private TextMeshProUGUI legsSkillCooldownText;
        [SerializeField] private TextMeshProUGUI backSkillCooldownText;

        public void ToggleCrosshead()
        {
            // crosshead.SetActive(!crosshead.activeSelf);
            crosshead.enabled = !crosshead.enabled;
        }

        public void SetLeftPartText(string leftPart)
        {
            testPartSystemUILeft.text = leftPart;
        }

        public void SetRightPartText(string rightPart)
        {
            testPartSystemUIRight.text = rightPart;
        }

        public void SetLegsText(string legs)
        {
            testPartSystemUILegs.text = legs;
        }

        public void SetHpSlider(float currentHealth, float maxHealth)
        {
            float rate = currentHealth / maxHealth;

            testHpBar.value = rate;
        }

        public void OnGameOverPanel()
        {
            testGameOverPanel.enabled = true;
        }

        public void CloseGameOverPanel()
        {
            testGameOverPanel.enabled = false;
        }

        public void SetAmmoLeftSlider(float current, float max)
        {
            float rate = current / max;
            ammoLeftBar.value = rate;
        }

        public void SetAmmoRightSlider(float current, float max)
        {
            float rate = current / max;
            ammoRightBar.value = rate;
        }

        public void SetAmmoColor(EPartType type, bool isOverheat)
        {
            Image image = null;
            if (type == EPartType.ArmL)
            {
                image = ammoLeftBar.GetComponentInChildren<Image>();
            }
            else
            {
                image = ammoRightBar.GetComponentInChildren<Image>();
            }
            if (image == null) return;

            if (isOverheat)
            {
                image.color = Color.white;
            }
            else
            {
                image.color = Color.black;
            }
        }

        public void SetLegsSkillIcon(bool isOn)
        {
            legsSkillCooldownImage.gameObject.SetActive(isOn);
        }

        public void SetLegsSkillCooldown(bool isOn)
        {
            legsSkillCooldownText.gameObject.SetActive(isOn);
        }

        public void SetBackSkillIcon(bool isOn)
        {
            backSkillCooldownImage.gameObject.SetActive(isOn);
        }

        public void SetBackSkillCooldown(bool isOn)
        {
            backSkillCooldownText.gameObject.SetActive(isOn);
        }

        public void SetLegsSkillCooldown(float cooldownTime)
        {
            legsSkillCooldownText.text = cooldownTime.ToString("F0");
        }

        public void SetBackSkillCooldown(float cooldownTime)
        {
            backSkillCooldownText.text = cooldownTime.ToString("F0");
        }

        public void ResetSkillCooldown()
        {
            SetLegsSkillIcon(false);
            SetLegsSkillCooldown(false);
            SetBackSkillIcon(false);
            SetBackSkillCooldown(false);
        }

        #region Fade In/Out

        private IEnumerator CoroutineFadeInOut(float startAlpha, float targetAlpha, float duration = 1f)
        {
            var timer = 0f;
            var panelColor = fadeInOutPanel.color;

            if (duration <= 0f)
            {
                panelColor.a = targetAlpha;
                fadeInOutPanel.color = panelColor;
                yield break;
            }

            while (timer < duration)
            {
                timer += Time.deltaTime;
                var progress = timer / duration;
                panelColor.a = Mathf.Lerp(startAlpha, targetAlpha, progress);
                fadeInOutPanel.color = panelColor;
                yield return null;
            }

            panelColor.a = targetAlpha;
            fadeInOutPanel.color = panelColor;
        }

        public void FadeIn(float duration = 1.0f)
        {
            if (fadeInOutPanel.color.a > 0)
            {
                if (_runningFadeCoroutine != null)
                    StopCoroutine(_runningFadeCoroutine);
                _runningFadeCoroutine = StartCoroutine(CoroutineFadeInOut(1, 0, duration));
            }
            else
            {
                Debug.Log("Fade In Complete");
            }
        }

        public void FadeOut(float duration = 1.0f)
        {
            if (fadeInOutPanel.color.a < 1)
            {
                if (_runningFadeCoroutine != null)
                    StopCoroutine(_runningFadeCoroutine);
                _runningFadeCoroutine = StartCoroutine(CoroutineFadeInOut(0, 1, duration));
            }
            else
            {
                Debug.Log("Fade In Complete");
            }
        }

        public void FadeTo(float targetAlpha, float duration = 1.0f)
        {
            if (_runningFadeCoroutine != null)
                StopCoroutine(_runningFadeCoroutine);
            _runningFadeCoroutine = StartCoroutine(CoroutineFadeInOut(fadeInOutPanel.color.a, targetAlpha, duration));
        }

        #endregion
    }
}
