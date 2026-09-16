using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Managers
{
    public class GameUIController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject GUI;

        [Header("Image Settings")] [SerializeField]
        private Image crosshead; // 조준점 이미지         

        [SerializeField] private Image fadeInOutPanel; // 카메라 연출 이미지

        [Header("Test Part System UI")] [SerializeField]
        private TextMeshProUGUI testPartSystemUILeft;

        [SerializeField] private TextMeshProUGUI testPartSystemUIRight;
        [SerializeField] private TextMeshProUGUI testPartSystemUILegs;

        [Header("Test HP Bar UI")] [SerializeField]
        private Slider testHpBar;
        [SerializeField] private TextMeshProUGUI playerHpText;

        [Header("Test GameOver UI")] [SerializeField]
        private Image testGameOverPanel;

        private Coroutine _runningFadeCoroutine; // 카메라 연출 코루틴 저장

        [Header("Test Crosshair UI")]
        [SerializeField] private Slider ammoLeftBar;
        [SerializeField] private Slider ammoRightBar;
        [SerializeField] private Image fillLeft;
        [SerializeField] private Image fillRight;
        [SerializeField] private GameObject hitCrosshair;
        private Coroutine crosshairRoutine = null;

        [Header("Test Skill UI")]
        [SerializeField] private Image legsSkillCooldownImage;
        [SerializeField] private Image backSkillCooldownImage;
        [SerializeField] private TextMeshProUGUI legsSkillCooldownText;
        [SerializeField] private TextMeshProUGUI backSkillCooldownText;

        [Header("Interaction UI")]
        [SerializeField] private GameObject interactionUI;
        [SerializeField] private TextMeshProUGUI interactionText;

        [Header("Object UI")]
        [SerializeField] private TextMeshProUGUI objectText;

        [Header("For Base Parts")]
        [SerializeField] private GameObject shoulderSkillImage;
        [SerializeField] private GameObject rightArmRadialImage;

        [Header("Indicator UI")]
        [SerializeField] private Image indicatorImage;
        [SerializeField] private UI_Indicator indicator;
        [SerializeField] private UI_Indicator partIndicator;
        private Coroutine _indicatorFadeRoutine = null;

        [Header("Buff UI")]
        [SerializeField] private GameObject buffImage;

        [Header("Radial UI")]
        // 꽤 급하게 작업하였으므로 리팩토링 필요
        [SerializeField] private GameObject radialUI;
        [SerializeField] private List<Image> selectedCircles = new();
        [SerializeField] private List<Image> partIcons = new();
        [SerializeField] private Image baseIcon;
        [SerializeField] private List<Button> laserParts = new();
        [SerializeField] private List<Button> rapidParts = new();
        [SerializeField] private List<Button> heavyParts = new();
        [SerializeField] private TextMeshProUGUI constraintText;
        [SerializeField] private List<Button> setButtons = new();
        [SerializeField] private List<Image> partSetIcons = new();
        [SerializeField] private GameObject radialDescription;
        private int _selectedIndex = -1;
        private int _selectedPartIndex = -1;
        private Color originalColor = Color.white;
        private Color selectedColor = new Color(0.09411765f, 0.1411765f, 0.1411765f);
        private List<bool> _unlockSets = new();

        [Header("Help UI")]
        [SerializeField] private GameObject helpUI;

        [Header("Pasue UI")]
        [SerializeField] private GameObject pauseUI;

        [Header("Current Part UI")]
        [SerializeField] private List<GameObject> backParts = new();
        [SerializeField] private List<GameObject> armLParts = new();
        [SerializeField] private List<GameObject> armRParts = new();
        [SerializeField] private List<GameObject> legsParts = new();

        [Header("Notice UI")]
        [SerializeField] private GameObject redDot;
        [SerializeField] private GameObject messageObject;
        [SerializeField] private Image messageImage;
        [SerializeField] private TextMeshProUGUI messageText;
        private Coroutine _messageRoutine = null;
        private Color _originalMessageColor;
        private Color _originalMessageTextColor;

        [Header("Map")]
        [SerializeField] private GameObject worldMap;

        [Header("Boss")]
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private Slider bossHpBar;

        [Header("Skill")]
        [SerializeField] private GameObject rapidInfo;
        [SerializeField] private TextMeshProUGUI rapidCooldownText;

        [Header("Menu UI")]
        [SerializeField] private GameObject tutorial;
        [SerializeField] private GameObject option;

        private static Coroutine _hapticCoroutine;

        public GameObject HUD
        {
            get => GUI;
            set => GUI = value;
        }

        public GameObject RapidInfo
        {
            get => rapidInfo;
            set => rapidInfo = value;
        }

        public GameObject WorldMap
        {
            get => worldMap;
            set => worldMap = value;
        }

        public GameObject Tutorial
        {
            get => tutorial;
        }

        public GameObject Option
        {
            get => option;
        }

        public Image IndicatorUI
        {
            get => indicatorImage;
            set => indicatorImage = value;
        }

        public GameObject InteractionUI
        {
            get => interactionUI;
            set => interactionUI = value;
        }

        public TextMeshProUGUI InteractionText
        {
            get => interactionText;
            set => interactionText = value;
        }

        public TextMeshProUGUI ObjectText
        {
            get => objectText;
            set => objectText = value;
        }

        public GameObject ShoulderIcon
        {
            get => shoulderSkillImage;
            set => shoulderSkillImage = value;
        }

        public GameObject RightArmRadial
        {
            get => rightArmRadialImage;
            set => rightArmRadialImage = value;
        }

        public GameObject BuffIcon
        {
            get => buffImage;
            set => buffImage = value;
        }

        public GameObject RadialUI
        {
            get => radialUI;
            set => radialUI = value;
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => _selectedIndex = value;
        }

        public int SelectedPartIndex
        {
            get => _selectedPartIndex;
            set => _selectedPartIndex = value;
        }

        public Image FillLeft
        {
            get => fillLeft;
            set => fillLeft = value;
        }

        public Image FillRight
        {
            get => fillRight;
            set => fillRight = value;
        }

        public List<bool> UnlockSets
        {
            get => _unlockSets;
        }

        public GameObject HelpUI
        {
            get => helpUI;
            set => helpUI = value;
        }

        public GameObject PauseUI
        {
            get => pauseUI;
            set => pauseUI = value;
        }

        #region Initialization
        public void Awake()
        {
            _originalMessageColor = messageImage.color;
            _originalMessageTextColor = messageText.color;

            //for (int i = 0; i < setButtons.Count; ++i)
            //{
            //    _unlockSets.Add(false);
            //}

            for (int i = 0; i < 3; ++i)
            {
                _unlockSets.Add(false);
            }
        }

        // UI_Global Scene 에서 Init Method 가 실행 되면 이후에 호출 되는 Initialization
        public void Initialization()
        {
            if (!HelpUI.activeSelf)
            {
                HelpUI.SetActive(true);
                HUD.SetActive(false);

                Time.timeScale = 0.0f;

                var player = MonsterManager.Instance.GetComponent<PlayerController>();
                if (player)
                {
                    player.FollowCamera.OnUIOpen();
                }
            }
        }
        
        #endregion

        public void ToggleCrosshead()
        {
            // crosshead.SetActive(!crosshead.activeSelf);
            crosshead.enabled = !crosshead.enabled;
        }

        public void SetLeftPartText(string leftPart)
        {
            //testPartSystemUILeft.text = leftPart;
        }

        public void SetRightPartText(string rightPart)
        {
            //testPartSystemUIRight.text = rightPart;
        }

        public void SetLegsText(string legs)
        {
            //testPartSystemUILegs.text = legs;
        }

        public void SetHpSlider(float currentHealth, float maxHealth)
        {
            float rate = currentHealth / maxHealth;

            testHpBar.value = rate;
            playerHpText.text = string.Format("{0:F0} / {1:F0}", currentHealth, maxHealth);
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

        public void SetAmmoColor(EPartType type, Color color)
        {
            Image image = null;
            if (type == EPartType.ArmL)
            {
                image = fillLeft.GetComponentInChildren<Image>();
            }
            else
            {
                image = fillRight.GetComponentInChildren<Image>();
            }
            if (image == null) return;

            image.color = color;
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
                image.color = Color.black;
            }
            else
            {
                image.color = Color.white;
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

        public void SetLegsSkillTimer(Color color)
        {
            legsSkillCooldownText.color = color;
        }

        public void ResetSkillCooldown()
        {
            SetLegsSkillIcon(false);
            SetLegsSkillCooldown(false);
            SetBackSkillIcon(false);
            SetBackSkillCooldown(false);
        }

        public void ToggleRadialUI(bool isOpen)
        {
            var player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();

            if (isOpen)
            {
                if (player)
                {
                    player.FollowCamera.OnUIOpen();
                }

                radialUI.gameObject.SetActive(true);
            }
            else
            {
                if (player)
                {
                    player.FollowCamera.OnUIClose();
                }

                for (int i = 0; i < selectedCircles.Count; ++i)
                {
                    selectedCircles[i].gameObject.SetActive(false);
                    partIcons[i].color = originalColor;
                }
                ToggleBasePartButton(false);

                _selectedIndex = -1;
                _selectedPartIndex = -1;

                Time.timeScale = 1.0f;
                
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (player)
                {
                    player.SetPlayerState(EPlayerState.Toggle, false);
                    player.SetMovable(true);
                    player.FollowCamera.SetCameraRotatable(true);
                }

                radialUI.gameObject.SetActive(false);
            }
        }

        public void SelectPartPosition(int type)
        {
            for (int i = 0; i < selectedCircles.Count; ++i)
            {
                if (i == type)
                {
                    selectedCircles[type].gameObject.SetActive(true);
                    partIcons[type].color = selectedColor;
                    continue;
                }

                selectedCircles[i].gameObject.SetActive(false);
                partIcons[i].color = originalColor;
            }

            // 왼팔 기본 파츠 X
            if (type != 3)
            {
                ToggleBasePartButton(true);
            }
            else
            {
                ToggleBasePartButton(false);
            }

            _selectedIndex = type;
        }

        public void UnselectPartPosition(int type)
        {
            selectedCircles[type].gameObject.SetActive(false);
            partIcons[type].color = originalColor;
            ToggleBasePartButton(false);
        }

        public void SelectShoulderPartType(int attackType)
        {
            PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.Inven.EquipItem(EPartType.Back, (EAttackType)(1 << attackType));
                player.Inven.EquipItem(EPartType.Shoulder, (EAttackType)(1 << attackType));
                player.Inven.EquipItem(EPartType.Mask, (EAttackType)(1 << attackType));
            }

            ToggleRadialUI(false);
        }

        public void SelectLegsPartType(int attackType)
        {
            PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.Inven.EquipItem(EPartType.Legs, (EAttackType)(1 << attackType));
            }

            ToggleRadialUI(false);
        }

        public void SelectArmLPartType(int attackType)
        {
            PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.Inven.EquipItem(EPartType.ArmL, (EAttackType)(1 << attackType));
            }

            ToggleRadialUI(false);
        }

        public void SelectArmRPartType(int attackType)
        {
            PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.Inven.EquipItem(EPartType.ArmR, (EAttackType)(1 << attackType));
            }

            ToggleRadialUI(false);
        }

        public void ToggleBasePartButton(bool isActivate)
        {
            if (isActivate) baseIcon.gameObject.SetActive(true);
            else baseIcon.gameObject.SetActive(false);
        }

        public void SelectBasePart()
        {
            // 현재 selectedIndex에 맞는 부위를 기본 파츠로 교체 (오른팔 제외)
            // 0: 등/어깨, 1: 다리, 2: 왼팔, 3: 오른팔
            PlayerController player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                switch (_selectedIndex)
                {
                    case 0:
                        player.Inven.EquipItem(EPartType.Back, (EAttackType)(1 << 0));
                        player.Inven.EquipItem(EPartType.Shoulder, (EAttackType)(1 << 0));
                        player.Inven.EquipItem(EPartType.Mask, (EAttackType)(1 << 0));
                        break;
                    case 1:
                        player.Inven.EquipItem(EPartType.Legs, (EAttackType)(1 << 0));
                        break;
                    case 2:
                        player.Inven.EquipItem(EPartType.ArmL, (EAttackType)(1 << 0));
                        break;
                    case 3:
                        break;
                }
            }

            ToggleRadialUI(false);
        }

        public void UnlockParts(int index)
        {
            if (index < 0 || index > 2) return;

            ActivateRedDot(true);

            switch (index)
            {
                case 0:
                    foreach (var button in laserParts)
                    {
                        button.gameObject.SetActive(true);
                        button.interactable = true;
                    }
                    break;
                case 1:
                    foreach (var button in rapidParts)
                    {
                        button.gameObject.SetActive(true);
                        button.interactable = true;
                    }
                    break;
                case 2:
                    foreach (var button in heavyParts)
                    {
                        button.gameObject.SetActive(true);
                        button.interactable = true;
                    }
                    break;
            }

            setButtons[index].interactable = true;
            _unlockSets[index] = true;
        }

        public void ActivateRedDot(bool isActivate)
        {
            redDot.SetActive(isActivate);
        }

        public void SetObjectText(string text)
        {
            objectText.text = text;
        }

        public void SetIndicator(bool isActivate, bool isNaviOn = true)
        {
            // 인디케이터 켜는 소리 필요
            if (isActivate)
            {
                //if (_indicatorFadeRoutine != null)
                //{
                //    StopCoroutine(_indicatorFadeRoutine);
                //    _indicatorFadeRoutine = null;
                //}

                //indicatorImage.color = Color.white;
                indicator.IsOn = true;
                partIndicator.IsOn = true;

                //if (isNaviOn)
                //{
                //    var player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
                //    if (player)
                //    {
                //        player.Navi.gameObject.SetActive(true);
                //        player.Navi.MoveToTarget();
                //    }
                //}
            }
            else
            {
                //_indicatorFadeRoutine = StartCoroutine(CoFadeIndicator(0.5f));
                indicator.IsOn = false;
                partIndicator.IsOn = false;

                //var player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
                //if (player)
                //{
                //    player.Navi.StopEffect();
                //}
            }
        }

        public void SetIndicatorTarget(Transform target)
        {
            indicator.Target = target;

            var player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.Navi.SetTarget(target);
            }
        }

        public void SetPartIndicatorTarget(Transform target)
        {
            partIndicator.Target = target;
        }

        public void SetConstraintMessage(string message)
        {
            constraintText.text = message;
        }

        public void SetPartSetIcon(int index)
        {
            for (int i = 0; i < partSetIcons.Count; ++i)
            {
                if (i == index) continue;
                partSetIcons[i].gameObject.SetActive(false);
            }

            partSetIcons[index].gameObject.SetActive(true);
        }

        public void StartHitCrosshair()
        {
            if (crosshairRoutine != null)
            {
                StopCoroutine(crosshairRoutine);
                crosshairRoutine = null;
            }

            crosshairRoutine = StartCoroutine(CoOnHitCrosshair());

            // 햅틱용 함수
            //PlayVibration(0.02f, 0.3f, 0.06f);
        }

        public void ActivateRadialMessage(bool isActivate)
        {
            radialDescription.SetActive(isActivate);
        }

        public void SetCurrentPartIcon(int posIndex, int attackIndex)
        {
            //if (posIndex < 0 || attackIndex < 0  || posIndex + 1 >= legsParts.Count || attackIndex + 1 >= legsParts.Count) return;

            // 기본 파츠일 경우
            if (attackIndex <= 0)
            {
                for (int i = 0; i < legsParts.Count; ++i)
                {
                    switch (posIndex)
                    {
                        case 0:
                            // 등/어깨
                            backParts[i].gameObject.SetActive(false);
                            backParts[i + 3].gameObject.SetActive(false);
                            break;
                        case 1:
                            // 다리
                            legsParts[i].gameObject.SetActive(false);
                            break;
                        case 2:
                            // 왼팔
                            armLParts[i].gameObject.SetActive(false);
                            break;
                        case 3:
                            // 오른팔
                            armRParts[i].gameObject.SetActive(false);
                            break;
                    }
                }

                return;
            }

            for (int i = 0; i < legsParts.Count; ++i)
            {
                if (i == (attackIndex - 1)) continue;

                switch (posIndex)
                {
                    case 0:
                        // 등/어깨
                        backParts[i].gameObject.SetActive(false);
                        backParts[i + 3].gameObject.SetActive(false);
                        break;
                    case 1:
                        // 다리
                        legsParts[i].gameObject.SetActive(false);
                        break;
                    case 2:
                        // 왼팔
                        armLParts[i].gameObject.SetActive(false);
                        break;
                    case 3:
                        // 오른팔
                        armRParts[i].gameObject.SetActive(false);
                        break;
                }
            }

            switch (posIndex)
            {
                case 0:
                    // 등/어깨
                    backParts[attackIndex - 1].gameObject.SetActive(true);
                    backParts[attackIndex - 1 + 3].gameObject.SetActive(true);
                    break;
                case 1:
                    // 다리
                    legsParts[attackIndex - 1].gameObject.SetActive(true);
                    break;
                case 2:
                    // 왼팔
                    armLParts[attackIndex - 1].gameObject.SetActive(true);
                    break;
                case 3:
                    // 오른팔
                    armRParts[attackIndex - 1].gameObject.SetActive(true);
                    break;
            }
        }

        private IEnumerator CoOnHitCrosshair()
        {
            hitCrosshair.SetActive(true);

            yield return new WaitForSeconds(0.1f);

            hitCrosshair.SetActive(false);
        }

        private IEnumerator CoFadeIndicator(float duration)
        {
            float elapsed = 0f;
            Color color = indicatorImage.color;
            float startAlpha = color.a;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                indicatorImage.color = color;
                yield return null;
            }
            // 마지막에 완전히 0으로 세팅
            color.a = 0f;
            indicatorImage.color = color;
            indicator.IsOn = false;
            _indicatorFadeRoutine = null;
        }
        
        public void Resume()
        {
            PlayerController player = MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.FollowCamera.OnUIClose();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            GUI.SetActive(true);
            pauseUI.SetActive(false);
            Time.timeScale = 1.0f;
        }

        // 게임 종료
        public void Shutdown()
        {
            GameManager.Instance.ExitGame();
        }

        // 타이틀로 복귀 (데모 빌드에서 게임 종료 버튼 대신 사용)
        // 클리어 후 크레딧이 끝났을 때와 같은 경로로 세션 상태를 초기화한다.
        public void ReturnToTitle()
        {
            // 복귀가 진행 중이면 CurrentState가 Loading이다. 버튼 연타로 정리가 중복 실행되는 것을 막는다.
            if (GameManager.Instance.CurrentState == GameManager.GameState.Loading) return;

            // 일시정지 UI에서 호출되므로 timeScale이 0인 상태다.
            // 되돌리지 않으면 Time.deltaTime 기반인 프롤로그 연출이 다음 판에서 멈춘다.
            Time.timeScale = 1.0f;
            StopVibration();

            GameManager.Instance.ReturnToTitleFromDemo();
        }

        public void OpenKeyGuide()
        {
            var player = MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.FollowCamera.OnUIOpen();
            }

            GUIManager.Instance.GameUIController.HelpUI.SetActive(true);
        }

        public void ActivateMessage(string message, float activeTime = 5.0f)
        {
            if (_messageRoutine != null)
            {
                StopCoroutine(_messageRoutine);
                _messageRoutine = null;

                messageImage.color = new Color(_originalMessageColor.r, _originalMessageColor.g, _originalMessageColor.b, 0f);
                messageText.color = new Color(_originalMessageTextColor.r, _originalMessageTextColor.g, _originalMessageTextColor.b, 0f);
                messageObject.SetActive(false);
            }

            messageText.text = message;
            _messageRoutine = StartCoroutine(CoActivateMessage(activeTime));
        }

        private IEnumerator CoActivateMessage(float activeTime)
        {
            Color startColor = _originalMessageColor;
            startColor.a = 0.0f;

            Color startTextColor = _originalMessageTextColor;
            _originalMessageTextColor.a = 0.0f;
            messageObject.SetActive(true);

            // 1초 동안 alpha 증가 (페이드 인)
            float fadeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
                messageImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                messageText.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, alpha);
                yield return null;
            }
            messageImage.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
            messageText.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, 1.0f);

            // 활성화 상태 유지
            yield return new WaitForSeconds(activeTime);

            // 1초 동안 alpha 감소 (페이드 아웃)
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                messageImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                messageText.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, alpha);
                yield return null;
            }
            messageImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
            messageText.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, 0.0f);

            messageObject.SetActive(false);

            _messageRoutine = null;
        }

        public void ActivateWorldMap()
        {
            var player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();

            if (worldMap.activeSelf)
            {
                if (player)
                {
                    player.FollowCamera.OnUIClose();
                }

                HUD.SetActive(true);
                worldMap.SetActive(false);
                Time.timeScale = 1.0f;
            }
            else
            {
                if (player)
                {
                    player.FollowCamera.OnUIOpen();
                }

                HUD.SetActive(false);
                worldMap.SetActive(true);
                Time.timeScale = 0.0f;
            }
        }

        public void UpdateBossHpBar(string name, float currentHp, float maxHp)
        {
            if (!bossNameText.gameObject.activeSelf)
            {
                bossNameText.text = name;
                bossNameText.gameObject.SetActive(true);
            }

            bossHpBar.value = currentHp / maxHp;

            if (bossHpBar.value <= 0.0f)
            {
                bossNameText.gameObject.SetActive(false);
            }
        }

        public void UpdateBossHpBar(float currentHp, float maxHp)
        {
            bossHpBar.value = currentHp / maxHp;

            if (bossHpBar.value <= 0.0f)
            {
                bossNameText.gameObject.SetActive(false);
            }
        }

        public void SetBossName(string name)
        {
            bossNameText.text = name;
        }

        public void ToggleBossHp(bool isOn)
        {
            if (isOn)
            {
                // 체력바 채움량은 피격 시 UpdateBossHpBar에서만 갱신되므로,
                // 여기서 초기화하지 않으면 첫 타격 전까지 프리팹에 저장된 값이 그대로 보인다.
                // 보스전 진입 시점은 항상 최대 체력이므로 가득 채운 상태로 시작한다.
                bossHpBar.value = 1.0f;

                bossNameText.gameObject.SetActive(true);
            }
            else
            {
                bossNameText.gameObject.SetActive(false);
            }
        }

        public void ChnagePartSet(int index)
        {
            if (index > 0 && !UnlockSets[index - 1]) return;

            var player = Managers.MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                for (int i = 0; i < 4; ++i)
                {
                    player.SelectAndChangePart(i, index);
                }
            }
            ToggleRadialUI(false);
        }

        public void SetRapidCooldownText(float remainingTime)
        {
            rapidCooldownText.text = $"{remainingTime:F0}";
        }

        public void OpenTutorial()
        {
            var player = MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.FollowCamera.OnUIOpen();
            }

            tutorial.SetActive(true);
        }

        public void OpenOption()
        {
            var player = MonsterManager.Instance.Player.GetComponent<PlayerController>();
            if (player)
            {
                player.FollowCamera.OnUIOpen();
            }

            option.SetActive(true);
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

        /// <summary>
        /// 현재 연결된 게임패드에 원하는 세기와 시간만큼 진동을 발생시킵니다.
        /// </summary>
        /// <param name="lowFrequency">왼쪽 모터의 묵직한 저주파 진동 세기 (0.0 ~ 1.0)</param>
        /// <param name="highFrequency">오른쪽 모터의 날카로운 고주파 진동 세기 (0.0 ~ 1.0)</param>
        /// <param name="duration">진동이 지속될 시간 (초 단위)</param>
        public void PlayVibration(float lowFrequency, float highFrequency, float duration)
        {
            // 1. 패드가 연결되어 있지 않다면 연산을 무시합니다.
            if (Gamepad.current == null) return;

            // 2. 새로운 진동이 들어오면 이전의 타이머 코루틴을 안전하게 멈춰서 진동이 무한히 지속되는 버그를 방지합니다.
            if (_hapticCoroutine != null)
            {
                StopCoroutine(_hapticCoroutine);
            }

            // 3. 인풋 시스템 내부 클램핑 값 안정성을 위해 범위를 보정합니다.
            lowFrequency = Mathf.Clamp01(lowFrequency);
            highFrequency = Mathf.Clamp01(highFrequency);

            // 4. 새로운 진동 타이머를 구동합니다.
            _hapticCoroutine = StartCoroutine(CoVibrationRoutine(lowFrequency, highFrequency, duration));
        }

        /// <summary>
        /// 게임패드의 진동을 즉시 정지시킵니다. 일시정지 메뉴 진입 시 필수적으로 호출해야 합니다.
        /// </summary>
        public void StopVibration()
        {
            if (_hapticCoroutine != null)
            {
                StopCoroutine(_hapticCoroutine);
                _hapticCoroutine = null;
            }

            if (Gamepad.current == null) return;
            Gamepad.current.SetMotorSpeeds(0.0f, 0.0f);
        }

        /// <summary>
        /// 지정된 세기로 모터를 가동하고, 타임스케일에 영향받지 않는 리얼타임 기준으로 대기 후 모터를 끄는 코루틴입니다.
        /// </summary>
        private IEnumerator CoVibrationRoutine(float lowFreq, float highFreq, float duration)
        {
            if (Gamepad.current == null) yield break;

            // 양쪽 모터 구동 시작
            Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);

            // 일시정지 UI 호출 시 Time.timeScale이 0이 되더라도, 
            // 미사일 발사 직후 컷씬 등에서 정상적으로 시간이 흘러 진동이 꺼지도록 Realtime 처리를 합니다.
            yield return new WaitForSecondsRealtime(duration);

            // 대기 시간이 끝나면 모터 출력 초기화 및 코루틴 참조 제거
            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(0.0f, 0.0f);
            }
            _hapticCoroutine = null;
        }

        //private void OnDisable()
        //{
        //    // 씬 전환, 애플리케이션 종료, 컴포넌트 비활성화 시 패드가 계속 우는 현상을 원천 방지합니다.
        //    if (Gamepad.current != null)
        //    {
        //        Gamepad.current.SetMotorSpeeds(0.0f, 0.0f);
        //    }
        //}

        //private void OnApplicationFocus(bool hasFocus)
        //{
        //    // 게임 창이 포커스를 잃어 전체화면에서 내려가거나 알트탭을 했을 때 진동을 차단합니다.
        //    if (!hasFocus)
        //    {
        //        StopVibration();
        //    }
        //}
    }
}
