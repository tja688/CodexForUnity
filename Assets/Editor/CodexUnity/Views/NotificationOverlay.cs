using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CodexUnity.Views
{
    /// <summary>
    /// 全局通知浮层 - 淡入淡出的侧边提醒
    /// </summary>
    public class NotificationOverlay : VisualElement
    {
        private readonly VisualElement _container;
        private readonly List<NotificationToast> _activeToasts = new();
        private bool _isRegistered;

        public NotificationOverlay()
        {
            // 固定在右侧
            style.position = Position.Absolute;
            style.right = 12;
            style.top = 12;
            style.width = 280;
            style.flexDirection = FlexDirection.Column;
            pickingMode = PickingMode.Ignore; // 不阻挡下层点击

            _container = new VisualElement();
            _container.style.flexDirection = FlexDirection.Column;
            Add(_container);

            // 订阅全局通知
            InstanceManager.Instance.OnNotification += ShowNotification;
        }

        public void ShowNotification(ToastNotification notification)
        {
            var toast = new NotificationToast(notification);
            toast.OnDismiss += () => RemoveToast(toast);
            toast.style.marginTop = _activeToasts.Count > 0 ? 8 : 0;  // 模拟 gap

            _container.Add(toast);
            _activeToasts.Add(toast);

            // 启动淡入动画
            toast.schedule.Execute(() => toast.FadeIn()).ExecuteLater(50);

            // 设置自动消失
            var lifetimeMs = (long)(notification.lifetime * 1000);
            toast.schedule.Execute(() => toast.FadeOut()).ExecuteLater(lifetimeMs);

            EnsureUpdateLoop();
        }

        private void RemoveToast(NotificationToast toast)
        {
            if (_activeToasts.Contains(toast))
            {
                _activeToasts.Remove(toast);
                _container.Remove(toast);
            }
        }

        private void EnsureUpdateLoop()
        {
            if (_isRegistered) return;
            EditorApplication.update += OnUpdate;
            _isRegistered = true;
        }

        private void OnUpdate()
        {
            // 清理已完成淡出的 toast
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                if (_activeToasts[i].IsExpired)
                {
                    var toast = _activeToasts[i];
                    _activeToasts.RemoveAt(i);
                    _container.Remove(toast);
                }
            }

            if (_activeToasts.Count == 0 && _isRegistered)
            {
                EditorApplication.update -= OnUpdate;
                _isRegistered = false;
            }
        }

        public void Cleanup()
        {
            InstanceManager.Instance.OnNotification -= ShowNotification;
            if (_isRegistered)
            {
                EditorApplication.update -= OnUpdate;
                _isRegistered = false;
            }
        }
    }

    /// <summary>
    /// 单个通知 Toast
    /// </summary>
    public class NotificationToast : VisualElement
    {
        private readonly ToastNotification _notification;
        private bool _isFadingOut;
        private double _fadeOutStartTime;
        private const float FadeInDuration = 0.3f;
        private const float FadeOutDuration = 0.3f;

        public bool IsExpired { get; private set; }
        public event Action OnDismiss;

        public NotificationToast(ToastNotification notification)
        {
            _notification = notification;

            // 初始状态：透明，右移
            style.opacity = 0;
            style.translate = new Translate(20, 0);
            style.transitionProperty = new List<StylePropertyName>
            {
                new("opacity"),
                new("translate")
            };
            style.transitionDuration = new List<TimeValue>
            {
                new(FadeInDuration, TimeUnit.Second),
                new(FadeInDuration, TimeUnit.Second)
            };
            style.transitionTimingFunction = new List<EasingFunction>
            {
                new(EasingMode.EaseOut),
                new(EasingMode.EaseOut)
            };

            // 卡片样式
            style.backgroundColor = GetBackgroundColor(notification.type);
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.paddingTop = 10;
            style.paddingBottom = 10;
            style.paddingLeft = 12;
            style.paddingRight = 12;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftColor = GetBorderColor(notification.type);
            style.borderRightColor = GetBorderColor(notification.type);
            style.borderTopColor = GetBorderColor(notification.type);
            style.borderBottomColor = GetBorderColor(notification.type);

            // 添加阴影效果（通过边框模拟）
            pickingMode = PickingMode.Position;

            // 内容
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;

            var iconLabel = new Label(GetIcon(notification.type));
            iconLabel.style.fontSize = 14;
            iconLabel.style.marginRight = 6;
            header.Add(iconLabel);

            var instanceLabel = new Label(notification.instanceName ?? "System");
            instanceLabel.style.fontSize = 11;
            instanceLabel.style.color = GetTextColor(notification.type);
            instanceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            instanceLabel.style.flexGrow = 1;
            header.Add(instanceLabel);

            var closeButton = new Button(() => FadeOut());
            closeButton.text = "×";
            closeButton.style.backgroundColor = Color.clear;
            closeButton.style.borderLeftWidth = 0;
            closeButton.style.borderRightWidth = 0;
            closeButton.style.borderTopWidth = 0;
            closeButton.style.borderBottomWidth = 0;
            closeButton.style.color = GetTextColor(notification.type);
            closeButton.style.fontSize = 14;
            closeButton.style.width = 20;
            closeButton.style.height = 20;
            closeButton.style.paddingTop = 0;
            closeButton.style.paddingBottom = 0;
            closeButton.style.paddingLeft = 0;
            closeButton.style.paddingRight = 0;
            header.Add(closeButton);

            Add(header);

            var messageLabel = new Label(notification.message);
            messageLabel.style.fontSize = 12;
            messageLabel.style.color = GetTextColor(notification.type);
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.marginTop = 4;
            Add(messageLabel);
        }

        public void FadeIn()
        {
            style.opacity = 1;
            style.translate = new Translate(0, 0);
        }

        public void FadeOut()
        {
            if (_isFadingOut) return;
            _isFadingOut = true;
            _fadeOutStartTime = EditorApplication.timeSinceStartup;

            style.transitionDuration = new List<TimeValue>
            {
                new(FadeOutDuration, TimeUnit.Second),
                new(FadeOutDuration, TimeUnit.Second)
            };
            style.opacity = 0;
            style.translate = new Translate(20, 0);

            schedule.Execute(() =>
            {
                IsExpired = true;
                OnDismiss?.Invoke();
            }).ExecuteLater((long)(FadeOutDuration * 1000) + 50);
        }

        private static Color GetBackgroundColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new Color(0.15f, 0.87f, 0.51f, 0.15f),
                NotificationType.Warning => new Color(0.99f, 0.79f, 0.34f, 0.15f),
                NotificationType.Error => new Color(1f, 0.42f, 0.42f, 0.15f),
                _ => new Color(0.31f, 0.80f, 0.77f, 0.15f)
            };
        }

        private static Color GetBorderColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new Color(0.15f, 0.87f, 0.51f, 0.4f),
                NotificationType.Warning => new Color(0.99f, 0.79f, 0.34f, 0.4f),
                NotificationType.Error => new Color(1f, 0.42f, 0.42f, 0.4f),
                _ => new Color(0.31f, 0.80f, 0.77f, 0.4f)
            };
        }

        private static Color GetTextColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new Color(0.15f, 0.87f, 0.51f),
                NotificationType.Warning => new Color(0.99f, 0.79f, 0.34f),
                NotificationType.Error => new Color(1f, 0.42f, 0.42f),
                _ => new Color(0.31f, 0.80f, 0.77f)
            };
        }

        private static string GetIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "✓",
                NotificationType.Warning => "⚠",
                NotificationType.Error => "✕",
                _ => "ℹ"
            };
        }
    }
}
