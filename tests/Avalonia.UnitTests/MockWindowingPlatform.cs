using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Moq;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockWindowingPlatform : IWindowingPlatform
    {
        private static readonly Size s_screenSize = new Size(1280, 1024);
        private readonly Func<IWindowImpl> _windowImpl;
        private readonly Func<IWindowBaseImpl, IPopupImpl> _popupImpl;

        public MockWindowingPlatform(
            Func<IWindowImpl> windowImpl = null,
            Func<IWindowBaseImpl, IPopupImpl> popupImpl = null )
        {
            _windowImpl = windowImpl;
            _popupImpl = popupImpl;
        }

        public static Mock<IWindowImpl> CreateWindowMock()
        {
            var win = Mock.Of<IWindowImpl>(x => x.Scaling == 1);
            var mock = Mock.Get(win);
            mock.Setup(x => x.Show()).Callback(() =>
            {
                mock.Object.Activated?.Invoke();
            });
            mock.Setup(x => x.CreatePopup()).Returns(() =>
            {
                windowImpl.Object.Closed?.Invoke();
            });

            windowImpl.Setup(x => x.Move(It.IsAny<PixelPoint>())).Callback<PixelPoint>(x =>
            {
                position = x;
                windowImpl.Object.PositionChanged?.Invoke(x);
            });

            windowImpl.Setup(x => x.Resize(It.IsAny<Size>())).Callback<Size>(x =>
            {
                clientSize = x.Constrain(s_screenSize);
                windowImpl.Object.Resized?.Invoke(clientSize);
            });

            windowImpl.Setup(x => x.Show()).Callback(() =>
            {
                windowImpl.Object.Activated?.Invoke();
            });

            return windowImpl;
        }

        public static Mock<IPopupImpl> CreatePopupMock(IWindowBaseImpl parent)
        {
            var popupImpl = new Mock<IPopupImpl>();

            var positionerHelper = new ManagedPopupPositionerPopupImplHelper(parent, (pos, size, scale) =>
            {
                popupImpl.Object.PositionChanged?.Invoke(pos);
                popupImpl.Object.Resized?.Invoke(size);
            });
            
            var positioner = new ManagedPopupPositioner(positionerHelper);

            popupImpl.Setup(x => x.Scaling).Returns(1);
            popupImpl.Setup(x => x.PopupPositioner).Returns(positioner);
            
            SetupToplevel(popupImpl);
            
            return popupImpl;
        }

        public static Mock<IScreenImpl> CreateScreenMock()
        {
            var screenImpl = new Mock<IScreenImpl>();
            var bounds = new PixelRect(0, 0, (int)s_screenSize.Width, (int)s_screenSize.Height);
            var screen = new Screen(96, bounds, bounds, true);
            screenImpl.Setup(x => x.AllScreens).Returns(new[] { screen });
            screenImpl.Setup(x => x.ScreenCount).Returns(1);
            return screenImpl;
        }

        public IWindowImpl CreateWindow()
        {
            if (_windowImpl is object)
            {
                return _windowImpl();
            }
            else
            {
                var mock = CreateWindowMock();

                if (_popupImpl is object)
                {
                    mock.Setup(x => x.CreatePopup()).Returns(() => _popupImpl(mock.Object));
                }

                return mock.Object;
            }
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        private static void SetupToplevel<T>(Mock<T> mock) where T : class, ITopLevelImpl
        {
            mock.SetupGet(x => x.MouseDevice).Returns(new MouseDevice());
        }
    }
}
