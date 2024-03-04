using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	Window? _window;
	public MainPage()
	{
		InitializeComponent();
	}

	private void Button_Clicked(object sender, EventArgs e)
	{
		if (this._window != null)
		{
			Application.Current?.CloseWindow(this._window);
			this._window = null;
			return;
		}
		this._window = new MyWindow(new ContentPage());
		Application.Current?.OpenWindow(this._window);
	}
}

public class MyWindow : Window
{
	public MyWindow(Page page) : base(page)
	{

	}
}

public class MyWindowHandler : WindowHandler
{
	private Windows.Win32.Foundation.HWND _mainWindowHandle;
	private Windows.Win32.Foundation.HWND _childWindowHandle;

	protected override void ConnectHandler(Microsoft.UI.Xaml.Window platformView)
	{
		base.ConnectHandler(platformView);

		var mainPagePlatformView = Application.Current?.MainPage?.Window.Handler.PlatformView as MauiWinUIWindow;
		if (mainPagePlatformView != null)
		{
			this._mainWindowHandle = new Windows.Win32.Foundation.HWND((mainPagePlatformView).GetWindowHandle());
			this._childWindowHandle = new Windows.Win32.Foundation.HWND(this.PlatformView.GetWindowHandle());

			Windows.Win32.PInvoke.SetParent(this._childWindowHandle, this._mainWindowHandle);
		}
	}

	protected override void DisconnectHandler(Microsoft.UI.Xaml.Window platformView)
	{
		//var result = Windows.Win32.PInvoke.SetParent(this._childWindowHandle, Windows.Win32.Foundation.HWND.Null);
		base.DisconnectHandler(platformView);
	}
}