using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    /// <summary>
    /// 튜토리얼 강조 연출. 대상(HUD 요소 또는 월드 오브젝트)만 남기고 화면을 어둡게 덮은 뒤
    /// 테두리를 붙인다. 텍스트를 읽지 않는 플레이어도 무엇을 봐야 하는지 알 수 있게 하는 용도다.
    ///
    /// 연출용 오브젝트는 전부 런타임에 만든다. 기획자는 프리팹에 이 컴포넌트만 올리고,
    /// HUD 대상은 uiTargets 목록에 이름과 RectTransform을 등록하면 된다.
    ///
    /// 대상은 여러 개를 동시에 지정할 수 있다. 암전은 대상 사각형들을 피해 가로 띠 단위로 쪼갠
    /// 여러 장의 패널로 만든다. 비워 둔 자리는 아무것도 덮지 않으므로 대상은 원래 모습 그대로 보인다.
    /// </summary>
    public class TutorialHighlightController : MonoBehaviour
    {
        [System.Serializable]
        public struct UITarget
        {
            [Tooltip("VisualScripting의 HighlightTarget 노드에서 입력할 이름")]
            public string key;
            public RectTransform target;
        }

        [Header("대상")]
        [SerializeField] private List<UITarget> uiTargets = new();

        [Header("연출")]
        [SerializeField] private Color dimColor = new Color(0.0f, 0.0f, 0.0f, 0.72f);
        [Tooltip("대상 사각형 바깥으로 추가할 여백(px)")]
        [SerializeField] private float padding = 24.0f;

        [Header("화면 밖 대상 화살표")]
        [Tooltip("화살표 아트. UI_HUD_24를 넣는다. 비워 두면 화살표를 그리지 않는다.")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Color arrowColor = new Color(1.0f, 0.15f, 0.15f, 1.0f);
        [Tooltip("화살표 크기(px). 아트 비율은 유지된다.")]
        [SerializeField] private float arrowSize = 72.0f;
        [Tooltip("아트가 위쪽을 향하지 않을 때 보정할 각도")]
        [SerializeField] private float arrowAngleOffset = 0.0f;
        [Tooltip("화살표가 깜빡이는 한 주기(초). 0이면 깜빡이지 않는다.")]
        [SerializeField] private float blinkCycle = 1.0f;

        [Header("정렬")]
        [Tooltip("암전 패널의 Sorting Order. 일시정지 UI처럼 위에 떠야 하는 UI보다 낮게 둔다. " +
                 "화살표는 이 값 + 1로 그려진다.")]
        [SerializeField] private int dimSortingOrder = 3;
        [Tooltip("암전과 표시가 나타나고 사라지는 시간(초)")]
        [SerializeField] private float fadeTime = 0.25f;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Canvas _arrowCanvas;
        private RectTransform _arrowCanvasRect;
        private readonly List<Image> _dimPanels = new();
        private Image _arrow;

        private readonly List<RectTransform> _activeUITargets = new();
        private readonly List<Transform> _activeWorldTargets = new();
        private readonly List<Renderer[]> _worldRenderers = new();

        // 매 프레임 갱신 시 재사용하는 버퍼
        private readonly List<Rect> _rects = new();
        private readonly List<float> _bandEdges = new();
        private readonly List<Vector2> _holes = new();
        private readonly Vector3[] _corners = new Vector3[4];

        private bool _useDim = true;
        private float _autoHideTimer = 0.0f;
        private float _alpha = 0.0f;
        private float _blinkTime = 0.0f;
        private bool _isShowing = false;
        private int _usedDimPanels = 0;

        public bool IsShowing => _isShowing;

        private void Awake()
        {
            BuildVisuals();
            SetVisible(false);
        }

        /// <summary>uiTargets에 등록한 이름으로 HUD 요소를 강조한다.</summary>
        public void ShowUI(string key, bool dimScreen = true, float autoHideTime = 0.0f)
        {
            ClearTargets();
            AddUITarget(key);
            Begin(dimScreen, autoHideTime);
        }

        /// <summary>HUD 요소 여러 개를 동시에 강조한다.</summary>
        public void ShowUI(IList<string> keys, bool dimScreen = true, float autoHideTime = 0.0f)
        {
            ClearTargets();
            if (keys != null)
            {
                foreach (string key in keys) AddUITarget(key);
            }

            Begin(dimScreen, autoHideTime);
        }

        /// <summary>월드 오브젝트를 강조한다. 화면 밖에 있으면 화살표가 방향을 가리킨다.</summary>
        public void ShowWorld(Transform target, bool dimScreen = true, float autoHideTime = 0.0f)
        {
            ClearTargets();
            AddWorldTarget(target);
            Begin(dimScreen, autoHideTime);
        }

        /// <summary>월드 오브젝트 여러 개를 동시에 강조한다.</summary>
        public void ShowWorld(IList<Transform> targets, bool dimScreen = true, float autoHideTime = 0.0f)
        {
            ClearTargets();
            if (targets != null)
            {
                foreach (Transform target in targets) AddWorldTarget(target);
            }

            Begin(dimScreen, autoHideTime);
        }

        public void Hide()
        {
            // 대상은 바로 비우지 않는다. 페이드 아웃이 끝날 때까지 그려야 할 사각형이 필요하다.
            _isShowing = false;
        }

        private void ClearTargets()
        {
            _activeUITargets.Clear();
            _activeWorldTargets.Clear();
            _worldRenderers.Clear();
        }

        private void AddUITarget(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            foreach (UITarget entry in uiTargets)
            {
                if (entry.key != key) continue;
                if (entry.target != null) _activeUITargets.Add(entry.target);
                return;
            }

            Debug.LogWarning($"[TutorialHighlight] '{key}' 이름의 UI 대상이 등록되어 있지 않습니다.");
        }

        private void AddWorldTarget(Transform target)
        {
            if (target == null) return;

            _activeWorldTargets.Add(target);
            _worldRenderers.Add(target.GetComponentsInChildren<Renderer>());
        }

        private void Begin(bool dimScreen, float autoHideTime)
        {
            _useDim = dimScreen;
            _autoHideTimer = autoHideTime;
            _isShowing = _activeUITargets.Count > 0 || _activeWorldTargets.Count > 0;
        }

        // 대상이 움직이거나 카메라가 돌면 위치가 바뀌므로 매 프레임 갱신한다.
        private void LateUpdate()
        {
            float delta = DeltaTime;
            if (_isShowing && _autoHideTimer > 0.0f)
            {
                _autoHideTimer -= delta;
                if (_autoHideTimer <= 0.0f) Hide();
            }

            float target = _isShowing ? 1.0f : 0.0f;
            float step = fadeTime > 0.0f ? delta / fadeTime : 1.0f;
            _alpha = Mathf.MoveTowards(_alpha, target, step);

            if (_alpha <= 0.0f)
            {
                // 페이드 아웃이 끝난 뒤에 대상을 비운다.
                if (!_isShowing) ClearTargets();
                SetVisible(false);
                return;
            }

            CollectRects(out bool allOffScreen);
            if (_rects.Count == 0)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            UpdateDim(allOffScreen);
            UpdateArrow(allOffScreen);
        }

        /// <summary>현재 대상들이 차지하는 화면 사각형 목록을 만든다.</summary>
        private void CollectRects(out bool allOffScreen)
        {
            _rects.Clear();
            int offScreenCount = 0;

            foreach (RectTransform uiTarget in _activeUITargets)
            {
                if (uiTarget == null || !uiTarget.gameObject.activeInHierarchy) continue;
                if (!TryGetUIRect(uiTarget, out Rect rect)) continue;

                _rects.Add(rect);
                if (IsOffScreen(rect)) ++offScreenCount;
            }

            for (int i = 0; i < _activeWorldTargets.Count; ++i)
            {
                if (_activeWorldTargets[i] == null) continue;
                if (!TryGetWorldRect(i, out Rect rect, out bool isOffScreen)) continue;

                _rects.Add(rect);
                if (isOffScreen) ++offScreenCount;
            }

            allOffScreen = _rects.Count > 0 && offScreenCount == _rects.Count;
        }

        private bool TryGetUIRect(RectTransform uiTarget, out Rect rect)
        {
            rect = new Rect();
            uiTarget.GetWorldCorners(_corners);

            // Overlay 캔버스의 UI는 월드 좌표가 곧 화면 좌표다. 그 외에는 카메라로 변환한다.
            Canvas targetCanvas = uiTarget.GetComponentInParent<Canvas>();
            Camera uiCamera = (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? targetCanvas.worldCamera
                : null;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; ++i)
            {
                Vector2 point = uiCamera != null
                    ? RectTransformUtility.WorldToScreenPoint(uiCamera, _corners[i])
                    : (Vector2)_corners[i];
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            rect = Rect.MinMaxRect(min.x - padding, min.y - padding, max.x + padding, max.y + padding);
            return true;
        }

        private bool TryGetWorldRect(int index, out Rect rect, out bool isOffScreen)
        {
            rect = new Rect();
            isOffScreen = false;

            Transform worldTarget = _activeWorldTargets[index];
            Camera cam = Camera.main;
            if (cam == null) return false;

            Renderer[] renderers = index < _worldRenderers.Count ? _worldRenderers[index] : null;
            Bounds bounds;
            if (renderers != null && renderers.Length > 0 && renderers[0] != null)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; ++i)
                {
                    if (renderers[i] == null) continue;
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            else
            {
                bounds = new Bounds(worldTarget.position, Vector3.one);
            }

            Vector2 minPoint = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxPoint = new Vector2(float.MinValue, float.MinValue);
            int behindCount = 0;
            for (int i = 0; i < 8; ++i)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, CornerSigns[i]);
                Vector3 point = cam.WorldToScreenPoint(corner);
                if (point.z < 0.0f)
                {
                    // 카메라 뒤의 점은 화면 좌표가 뒤집히므로 사각형 계산에서 제외한다.
                    ++behindCount;
                    continue;
                }

                minPoint = Vector2.Min(minPoint, point);
                maxPoint = Vector2.Max(maxPoint, point);
            }

            if (behindCount == 8)
            {
                // 완전히 뒤에 있으면 방향만 알려준다.
                Vector3 direction = worldTarget.position - cam.transform.position;
                Vector2 screenDirection = new Vector2(Vector3.Dot(direction, cam.transform.right), -1.0f).normalized;
                Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f + screenDirection * Screen.height;
                rect = new Rect(center.x - 40.0f, center.y - 40.0f, 80.0f, 80.0f);
                isOffScreen = true;
                return true;
            }

            rect = Rect.MinMaxRect(minPoint.x - padding, minPoint.y - padding, maxPoint.x + padding, maxPoint.y + padding);
            isOffScreen = IsOffScreen(rect);
            return true;
        }

        private static bool IsOffScreen(Rect rect)
        {
            return rect.xMax < 0.0f || rect.xMin > Screen.width || rect.yMax < 0.0f || rect.yMin > Screen.height;
        }

        private static readonly Vector3[] CornerSigns =
        {
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(1, 1, -1),
            new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(-1, 1, 1), new Vector3(1, 1, 1),
        };

        /// <summary>
        /// 대상 사각형들을 피해 화면을 덮는다. 사각형들의 위아래 경계로 화면을 가로 띠로 나누고,
        /// 각 띠에서 대상이 가리는 구간만 건너뛰며 패널을 놓는다. 대상이 몇 개든 구멍이 정확히 남는다.
        /// </summary>
        private void UpdateDim(bool allOffScreen)
        {
            _usedDimPanels = 0;
            if (!_useDim)
            {
                HideUnusedDimPanels();
                return;
            }

            Color color = dimColor;
            color.a *= _alpha;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 대상이 전부 화면 밖이면 뚫을 구멍이 없다. 화면 전체를 덮고 화살표만 위에 남긴다.
            if (allOffScreen)
            {
                SetRect(GetDimPanel(color).rectTransform, 0.0f, 0.0f, screenWidth, screenHeight);
                HideUnusedDimPanels();
                return;
            }

            _bandEdges.Clear();
            _bandEdges.Add(0.0f);
            _bandEdges.Add(screenHeight);
            foreach (Rect rect in _rects)
            {
                float bottom = Mathf.Clamp(rect.yMin, 0.0f, screenHeight);
                float top = Mathf.Clamp(rect.yMax, 0.0f, screenHeight);
                if (!_bandEdges.Contains(bottom)) _bandEdges.Add(bottom);
                if (!_bandEdges.Contains(top)) _bandEdges.Add(top);
            }
            _bandEdges.Sort();

            for (int band = 0; band < _bandEdges.Count - 1; ++band)
            {
                float bandBottom = _bandEdges[band];
                float bandTop = _bandEdges[band + 1];
                float bandHeight = bandTop - bandBottom;
                if (bandHeight <= 0.01f) continue;

                // 이 띠에서 대상이 가리는 x 구간을 모아 정렬한 뒤, 구간 사이의 빈 곳만 덮는다.
                float bandCenter = (bandBottom + bandTop) * 0.5f;
                _holes.Clear();
                foreach (Rect rect in _rects)
                {
                    if (bandCenter < rect.yMin || bandCenter > rect.yMax) continue;

                    float holeStart = Mathf.Clamp(rect.xMin, 0.0f, screenWidth);
                    float holeEnd = Mathf.Clamp(rect.xMax, 0.0f, screenWidth);
                    if (holeEnd > holeStart) _holes.Add(new Vector2(holeStart, holeEnd));
                }
                _holes.Sort((a, b) => a.x.CompareTo(b.x));

                float cursor = 0.0f;
                foreach (Vector2 hole in _holes)
                {
                    // 앞선 구간과 겹치면 건너뛴다. (정렬했으므로 시작점은 항상 cursor 이상이거나 겹친 구간이다)
                    if (hole.x > cursor)
                    {
                        SetRect(GetDimPanel(color).rectTransform, cursor, bandBottom, hole.x - cursor, bandHeight);
                    }

                    cursor = Mathf.Max(cursor, hole.y);
                }

                if (cursor < screenWidth)
                {
                    SetRect(GetDimPanel(color).rectTransform, cursor, bandBottom, screenWidth - cursor, bandHeight);
                }
            }

            HideUnusedDimPanels();
        }

        // 화살표는 대상이 전부 화면 밖일 때 방향을 알려주는 용도로만 쓴다.
        private void UpdateArrow(bool allOffScreen)
        {
            _arrow.enabled = allOffScreen && arrowSprite != null;
            if (!_arrow.enabled) return;

            Color color = arrowColor;
            color.a *= _alpha * Blink();
            _arrow.color = color;

            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 direction = _rects[0].center - screenCenter;
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
            direction.Normalize();

            const float margin = 90.0f;
            Vector2 position = screenCenter + direction * (Mathf.Min(Screen.width, Screen.height) * 0.5f - margin);

            RectTransform rect = _arrow.rectTransform;
            rect.anchoredPosition = position / CanvasScale;
            rect.localRotation = Quaternion.Euler(
                0.0f, 0.0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f + arrowAngleOffset);
        }

        private Image GetDimPanel(Color color)
        {
            while (_dimPanels.Count <= _usedDimPanels) _dimPanels.Add(CreateImage("Dim"));

            Image panel = _dimPanels[_usedDimPanels++];
            panel.enabled = true;
            panel.color = color;
            return panel;
        }

        private void HideUnusedDimPanels()
        {
            for (int i = _usedDimPanels; i < _dimPanels.Count; ++i) _dimPanels[i].enabled = false;
        }

        private float Blink()
        {
            if (blinkCycle <= 0.0f) return 1.0f;

            // 0.45 ~ 1.0 사이를 오간다. 완전히 꺼지면 오히려 눈에 덜 들어온다.
            _blinkTime += DeltaTime;
            float t = Mathf.PingPong(_blinkTime / blinkCycle, 1.0f);
            return Mathf.Lerp(0.45f, 1.0f, t);
        }

        /// <summary>
        /// 연출 속도는 인게임 시간 기준이다.
        /// 다만 파츠 교체 메뉴처럼 시간을 느리게 만드는 구간에서는 연출까지 느려지면 답답해지므로
        /// 슬로우 모션 중에는 실제 시간을 쓴다. 완전히 멈춘 상태(일시정지)에서는 연출도 함께 멈춘다.
        /// </summary>
        private static float DeltaTime =>
            (Time.timeScale > 0.0f && Time.timeScale < 1.0f) ? Time.unscaledDeltaTime : Time.deltaTime;

        private void SetVisible(bool visible)
        {
            if (_canvas != null && _canvas.enabled != visible) _canvas.enabled = visible;
            if (_arrowCanvas != null && _arrowCanvas.enabled != visible) _arrowCanvas.enabled = visible;
        }

        /// <summary>
        /// 계산은 전부 화면 픽셀로 하고, RectTransform에 넣을 때만 캔버스 단위로 바꾼다.
        /// 부모 HUD 캔버스에 CanvasScaler가 붙어 있으면 캔버스 좌표가 픽셀과 달라지기 때문이다.
        /// </summary>
        private float CanvasScale => (_canvas != null && _canvas.scaleFactor > 0.0f) ? _canvas.scaleFactor : 1.0f;

        private void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            float scale = CanvasScale;
            rect.anchoredPosition = new Vector2(x / scale, y / scale);
            rect.sizeDelta = new Vector2(Mathf.Max(0.0f, width) / scale, Mathf.Max(0.0f, height) / scale);
        }

        private void BuildVisuals()
        {
            // 암전과 화살표를 각각 다른 캔버스에 둔다. 일시정지처럼 이 연출보다 위에 떠야 하는 UI가 있어
            // Sorting Order를 낮게 잡고, 화살표만 암전보다 한 단계 위로 올린다.
            _canvasRect = CreateCanvas("TutorialHighlightDim", dimSortingOrder, out _canvas);
            _arrowCanvasRect = CreateCanvas("TutorialHighlightArrow", dimSortingOrder + 1, out _arrowCanvas);

            _arrow = CreateImage("Arrow", _arrowCanvasRect);
            _arrow.sprite = arrowSprite;
            _arrow.preserveAspect = true;
            RectTransform arrowRect = _arrow.rectTransform;
            arrowRect.pivot = new Vector2(0.5f, 0.5f);

            float height = arrowSize;
            float width = arrowSize;
            if (arrowSprite != null && arrowSprite.rect.height > 0.0f)
            {
                width = arrowSize * (arrowSprite.rect.width / arrowSprite.rect.height);
            }
            arrowRect.sizeDelta = new Vector2(width, height);
        }

        private RectTransform CreateCanvas(string canvasName, int order, out Canvas canvas)
        {
            GameObject canvasObject = new GameObject(canvasName);
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;

            // CanvasScaler를 두지 않아 캔버스 좌표가 곧 화면 픽셀이 된다.
            return canvasObject.GetComponent<RectTransform>();
        }

        private Image CreateImage(string imageName, RectTransform parent = null)
        {
            GameObject imageObject = new GameObject(imageName, typeof(RectTransform));
            imageObject.transform.SetParent(parent != null ? parent : _canvasRect, false);

            RectTransform rect = (RectTransform)imageObject.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;

            Image image = imageObject.AddComponent<Image>();
            // 조작을 막지 않도록 레이캐스트 대상에서 제외한다.
            image.raycastTarget = false;
            return image;
        }
    }
}
