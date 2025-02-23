﻿using Panuon.WPF.UI.Configurations;
using Panuon.WPF.UI.Internal.Implements;
using Panuon.WPF.UI.Internal.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Panuon.WPF.UI.Internal.Controls
{
    internal class NoticeBoxWindow 
        : Window
    {
        #region Fields
        private NoticeBoxPosition _position;

        private AnimationStackPanel _astkItems;

        private NoticeHandlerImpl _noticeHandler;
        #endregion

        #region Ctor
        public NoticeBoxWindow(NoticeBoxPosition position,
            AnimationEasing animationEase,
            TimeSpan animationDuration)
        {
            _position = position;

            SizeToContent = SizeToContent.Width;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            _astkItems = new AnimationStackPanel()
            {
                AnimationEasing = animationEase,
                AnimationDuration = animationDuration,
                HorizontalAlignment = HorizontalAlignment.Right,
                AnimationDirection = (position == NoticeBoxPosition.TopRight || position == NoticeBoxPosition.BottomRight)
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight,
                ArrangeDirection = (_position == NoticeBoxPosition.BottomLeft || _position == NoticeBoxPosition.BottomRight)
                    ? ArrangeDirection.Reverse
                    : ArrangeDirection.Normal,
                Spacing = 15,
            };
            Content = _astkItems;
        }
        #endregion

        #region Overrides

        #region OnApplyTemplate
        public override void OnApplyTemplate()
        {
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Topmost = true;
        }
        #endregion

        #region OnSourceInitialized
        protected override void OnSourceInitialized(EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            var extendStyle = Win32Util.GetWindowLong(hwnd, -20);
            var newStyle = extendStyle | 0x00000080 | 0x08000000;

            Win32Util.SetWindowLong(hwnd, -20, newStyle);
            Win32Util.SetWindowPos(hwnd, -1, 0, 0, 0, 0, 0x0010 | 0x0002); //TOPMOST SWP_NOACTIVATE NO_MOVE  

            Height = SystemParameters.MaximizedPrimaryScreenHeight - 14;

            base.OnSourceInitialized(e);
        }
        #endregion

        #region OnRenderSizeChanged
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            Top = 0;
            Left = (_position == NoticeBoxPosition.TopRight || _position == NoticeBoxPosition.BottomRight)
                ? SystemParameters.MaximizedPrimaryScreenWidth - RenderSize.Width - 15
                : 0;

            base.OnRenderSizeChanged(sizeInfo);
        }
        #endregion

        #endregion

        #region Methods
        public INoticeHandler AddItem(string message, 
            string caption, 
            MessageBoxIcon icon,
            ImageSource imageIcon,
            int? duration,
            TimeSpan defaultDuration,
            bool canClose,
            string noticeBoxItemStyle)
        {
            return (INoticeHandler)Dispatcher.Invoke(new Func<INoticeHandler>(() =>
            {
                var noticeBoxItem = new NoticeBoxItem(defaultDuration, duration)
                {
                    Style = XamlUtil.FromXaml<Style>(noticeBoxItemStyle),
                    Caption = caption,
                    Message = message,
                    Icon = icon,
                    ImageIcon = imageIcon,
                    CanClose = canClose,
                };
                noticeBoxItem.Closed += NoticeBoxItem_Closed;
                noticeBoxItem.Click += NoticeBoxItem_Click;
                _noticeHandler = new NoticeHandlerImpl(noticeBoxItem);
                _astkItems.Children.Add(noticeBoxItem);
                return _noticeHandler;
            }));
        }
        #endregion

        #region Event Handlers
        private void NoticeBoxItem_Click(object sender, RoutedEventArgs e)
        {
            _noticeHandler.TriggerClicked(sender as NoticeBoxItem);
        }

        private void NoticeBoxItem_Closed(object sender, EventArgs e)
        {
            var noticeBoxItem = sender as NoticeBoxItem;
            Dispatcher.Invoke(new Action(() =>
            {
                if (_astkItems.Children.Contains(noticeBoxItem))
                {
                    _astkItems.Children.Remove(noticeBoxItem);
                }
            }));
            _noticeHandler.TriggerClosed(noticeBoxItem);
        }
        #endregion

        #region Functions
        #endregion
    }
}