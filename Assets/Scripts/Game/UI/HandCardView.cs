using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Interaction;
using XHHY.Game.Motion;

namespace XHHY.Game.UI
{
    public class HandCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image iconImage;
        [SerializeField] Image typeIcon;
        [SerializeField] TMP_Text countText;
        [SerializeField] RectTransform attributeRow;
        [SerializeField] Image equipDot;

        public CardData Data { get; private set; }
        public CardInstance Instance { get; private set; }
        public HandTrayController HomeTray { get; private set; }
        public bool IsInSlot { get; private set; }
        public EventCardSlot CurrentSlot { get; private set; }
        public bool LayoutLocked { get; private set; }
        public int DisplayCount { get; private set; } = 1;

        HandCardDrag _drag;
        Spring2D _posSpring;
        Spring1D _scaleSpring;
        Spring1D _tiltSpring;
        Vector2 _targetPos;
        float _targetScale = 1f;
        float _targetTilt;
        bool _hasTarget;
        bool _springSynced;

        public void Bind(CardData data, HandTrayController tray)
        {
            Data = data;
            Instance = null;
            DisplayCount = 1;
            HomeTray = tray;
            IsInSlot = false;
            CurrentSlot = null;
            RefreshVisual();
            EnsureDrag();
        }

        public void Bind(CardInstance instance, HandTrayController tray, int displayCount = 1)
        {
            Instance = instance;
            Data = instance != null ? instance.Data : null;
            DisplayCount = Mathf.Max(1, displayCount);
            HomeTray = tray;
            IsInSlot = false;
            CurrentSlot = null;
            RefreshVisual();
            EnsureDrag();
        }

        void EnsureDrag()
        {
            _drag = GetComponent<HandCardDrag>();
            if (_drag == null)
                _drag = gameObject.AddComponent<HandCardDrag>();
            _drag.Bind(this);
        }

        public void SetDisplayCount(int count)
        {
            DisplayCount = Mathf.Max(1, count);
            RefreshCountBadge();
        }

        public void RefreshVisual()
        {
            if (nameText != null)
                nameText.text = Data != null ? Data.title : string.Empty;
            if (iconImage != null)
                iconImage.sprite = Data != null ? Data.icon : null;
            if (typeIcon != null)
            {
                typeIcon.sprite = Data != null && Data.cardType != null ? Data.cardType.icon : null;
                typeIcon.enabled = typeIcon.sprite != null;
            }

            RefreshCountBadge();
            RefreshAttributeChips();
            RefreshEquipDot();
        }

        void RefreshCountBadge()
        {
            if (countText == null)
                return;
            bool show = DisplayCount > 1;
            countText.gameObject.SetActive(show);
            if (show)
                countText.text = $"×{DisplayCount}";
        }

        void RefreshAttributeChips()
        {
            EnsureAttributeRow();
            if (attributeRow == null)
                return;

            var attrs = Data != null ? Data.GetSortedAttributes() : null;
            int count = attrs != null ? attrs.Count : 0;
            attributeRow.gameObject.SetActive(count > 0);
            if (count == 0)
                return;

            int show = Mathf.Min(3, count);
            int extra = count - show;
            int needed = show + (extra > 0 ? 1 : 0);
            for (int i = 0; i < needed; i++)
            {
                var chip = EnsureChip(i);
                chip.gameObject.SetActive(true);
                var label = LabelOf(chip);
                if (label == null)
                    continue;
                if (nameText != null && nameText.font != null)
                    label.font = nameText.font;
                if (i < show)
                {
                    var a = attrs[i];
                    label.text = $"{AttributeShort(a.attributeId)}{a.value}";
                }
                else
                {
                    label.text = $"+{extra}";
                }
            }

            for (int i = needed; i < attributeRow.childCount; i++)
                attributeRow.GetChild(i).gameObject.SetActive(false);

            attributeRow.SetAsLastSibling();
        }

        void EnsureAttributeRow()
        {
            if (attributeRow != null)
                return;

            var existing = transform.Find("AttrRow") as RectTransform;
            if (existing != null)
            {
                attributeRow = existing;
                return;
            }

            var go = new GameObject("AttrRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(transform, false);
            attributeRow = go.GetComponent<RectTransform>();
            attributeRow.anchorMin = new Vector2(0f, 0f);
            attributeRow.anchorMax = new Vector2(1f, 0f);
            attributeRow.pivot = new Vector2(0.5f, 0f);
            attributeRow.anchoredPosition = new Vector2(0f, 12f);
            attributeRow.sizeDelta = new Vector2(-12f, 28f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 4f;
            layout.padding = new RectOffset(2, 2, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        Transform EnsureChip(int index)
        {
            while (attributeRow.childCount <= index)
            {
                if (attributeRow.childCount == 0)
                    return attributeRow;

                var clone = Instantiate(attributeRow.GetChild(0).gameObject, attributeRow);
                clone.name = "Chip" + attributeRow.childCount;
            }

            return attributeRow.GetChild(index);
        }

        static TMP_Text LabelOf(Transform chip)
        {
            var texts = chip.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject != chip.gameObject)
                    return texts[i];
            }

            return chip.GetComponent<TMP_Text>();
        }

        static string AttributeShort(string id)
        {
            switch (id)
            {
                case "metal": return "金";
                case "wood": return "木";
                case "water": return "水";
                case "fire": return "火";
                case "earth": return "土";
                default:
                    return string.IsNullOrEmpty(id) ? "?" : id.Substring(0, 1);
            }
        }

        void RefreshEquipDot()
        {
            if (equipDot == null)
                return;
            bool show = Instance != null && Instance.EquippedCount > 0;
            equipDot.enabled = show;
        }

        public void SetLayoutLocked(bool locked) => LayoutLocked = locked;

        public void SetLayoutTarget(Vector2 anchoredPosition, float scale, bool snap)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            _targetPos = anchoredPosition;
            _targetScale = scale;
            if (snap || !_hasTarget)
            {
                rt.anchoredPosition = anchoredPosition;
                rt.localScale = new Vector3(scale, scale, 1f);
                rt.localRotation = Quaternion.identity;
                _posSpring.Snap(anchoredPosition);
                _scaleSpring.Snap(scale);
                _tiltSpring.Snap(0f);
                _targetTilt = 0f;
                _springSynced = true;
            }

            _hasTarget = true;
        }

        public void SetDragTiltTarget(float tiltDeg) => _targetTilt = tiltDeg;

        public void CaptureSpringFromTransform()
        {
            var rt = (RectTransform)transform;
            _posSpring.Snap(rt.anchoredPosition);
            _scaleSpring.Snap(rt.localScale.x);
            float z = rt.localEulerAngles.z;
            if (z > 180f) z -= 360f;
            _tiltSpring.Snap(z);
            _springSynced = true;
            _hasTarget = true;
        }

        public void TickLayout(MotionSettings motion, float dt)
        {
            if (LayoutLocked || IsInSlot || !_hasTarget)
                return;
            // 拖到 Canvas 上时不要用手里的弹簧坐标去写，否则牌会被甩出屏幕。
            if (HomeTray != null && HomeTray.HandCardContainer != null
                && transform.parent != HomeTray.HandCardContainer)
                return;

            var settings = motion != null ? motion : MotionSettings.CreateRuntimeDefaults();
            if (!_springSynced)
                CaptureSpringFromTransform();

            _posSpring.Step(_targetPos, settings.stiffness, settings.damping, dt, settings.substeps);
            _scaleSpring.Step(_targetScale, settings.stiffness, settings.damping, dt, settings.substeps);
            _tiltSpring.Step(_targetTilt, settings.stiffness, settings.damping, dt, settings.substeps);

            var rt = (RectTransform)transform;
            rt.anchoredPosition = _posSpring.Value;
            float s = _scaleSpring.value;
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, _tiltSpring.value);
        }

        public void AttachToSlot(Transform slotAnchor)
        {
            IsInSlot = true;
            LayoutLocked = true;
            var rt = (RectTransform)transform;
            var size = rt.rect.size;
            if (size.x < 20f || size.y < 20f)
                size = new Vector2(140f, 200f);

            transform.SetParent(slotAnchor, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(140f, size.x), Mathf.Max(200f, size.y));
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            gameObject.SetActive(true);

            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
            }

            transform.SetAsLastSibling();
            _posSpring.Snap(Vector2.zero);
            _scaleSpring.Snap(1f);
            _tiltSpring.Snap(0f);
            _springSynced = true;
            CurrentSlot = slotAnchor != null ? slotAnchor.GetComponentInParent<EventCardSlot>() : null;
            HomeTray?.NotifyCardLeftHand(this);
        }

        public void LiftFromSlot()
        {
            var slot = CurrentSlot;
            CurrentSlot = null;
            IsInSlot = false;
            LayoutLocked = true;
            if (slot != null && slot.Occupant == this)
                slot.Clear();
        }

        public void ReturnToHand()
        {
            IsInSlot = false;
            CurrentSlot = null;
            HomeTray?.ReturnCard(this);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
