﻿using System.Diagnostics;
using Microsoft.Maui.Appium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Windows;

namespace TestUtils.Appium.UITests
{
	public sealed class AppiumContext : IContext
	{
		const int Port = 4723;
		readonly AppiumOptions _appiumOptions;
		readonly List<UITestContext> _contexts; // Since tests don't know when they are done, we need to keep track of all the contexts we create so we can dispose them
		AppiumLocalService? _server;

		public AppiumContext()
		{
			_appiumOptions = new AppiumOptions();
			_contexts = new List<UITestContext>();
		}

		public void CreateAndStartServer(int port = Port)
		{
			var arguments = new OpenQA.Selenium.Appium.Service.Options.OptionCollector();
			arguments.AddArguments(new KeyValuePair<string, string>("--base-path", "/wd/hub"));

			var logFile = Environment.GetEnvironmentVariable("APPIUM_LOG_FILE") ?? "appium.log";

			var service = new AppiumServiceBuilder()
				.WithArguments(arguments)
				.UsingPort(port)
				.WithLogFile(new FileInfo(logFile))
				.Build();

			service.OutputDataReceived += (s, e) => Debug.WriteLine($"Appium {e.Data}");
			service.Start();
			_server = service;
		}

		public UITestContext CreateUITestContext(TestConfig testConfig)
		{
			if (_server == null)
			{
				throw new InvalidOperationException("Server is not initialized. Call CreateAndStartServer() first.");
			}

			if (testConfig == null)
				throw new ArgumentNullException(nameof(testConfig));

			SetGeneralAppiumOptions(testConfig, _appiumOptions);
			SetPlatformAppiumOptions(testConfig, _appiumOptions);

			while (!_server.IsRunning)
			{
				Task.Delay(1000).Wait();
			}

			var driverUri = new Uri($"http://localhost:{Port}/wd/hub");
			AppiumDriver driver = testConfig.TestDevice switch
			{
				TestDevice.Android => new AndroidDriver(driverUri, _appiumOptions),
				TestDevice.iOS => new IOSDriver(driverUri, _appiumOptions),
				TestDevice.Mac => new MacDriver(driverUri, _appiumOptions),
				TestDevice.Windows => new WindowsDriver(driverUri, _appiumOptions),
				_ => throw new InvalidOperationException("Unknown test device"),
			};

			var newContext = new UITestContext(new AppiumUITestApp(testConfig.AppId, driver), testConfig);
			_contexts.Add(newContext);

			return newContext;
		}

		static void SetPlatformAppiumOptions(TestConfig testConfig, AppiumOptions appiumOptions)
		{
			var appId = testConfig.BundleId ?? testConfig.AppId;
			appiumOptions.PlatformName = testConfig.PlatformName;
			appiumOptions.AutomationName = testConfig.AutomationName;

			if (!string.IsNullOrEmpty(testConfig.DeviceName))
				appiumOptions.DeviceName = testConfig.DeviceName;

			if (!string.IsNullOrEmpty(testConfig.PlatformVersion))
				appiumOptions.PlatformVersion = testConfig.PlatformVersion;

			if (!string.IsNullOrEmpty(testConfig.AppPath))
				appiumOptions.App = testConfig.AppPath;

			switch (testConfig.TestDevice)
			{
				case TestDevice.iOS:
					appiumOptions.AddAdditionalAppiumOption(MobileCapabilityType.Udid, testConfig.Udid);
					appiumOptions.AddAdditionalAppiumOption(IOSMobileCapabilityType.BundleId, appId);
					break;
				case TestDevice.Mac:
					appiumOptions.AddAdditionalAppiumOption(IOSMobileCapabilityType.BundleId, appId);
					appiumOptions.AddAdditionalAppiumOption("showServerLogs", true);
					break;
			}
		}

		static void SetGeneralAppiumOptions(TestConfig testConfig, AppiumOptions appiumOptions)
		{
			appiumOptions.AddAdditionalAppiumOption("reportDirectory", testConfig.ReportDirectory);
			appiumOptions.AddAdditionalAppiumOption("reportFormat", testConfig.ReportFormat);

			if (string.IsNullOrEmpty(testConfig.TestName))
				appiumOptions.AddAdditionalAppiumOption("testName", testConfig.TestName);

			if (testConfig.FullReset)
				appiumOptions.AddAdditionalAppiumOption(MobileCapabilityType.FullReset, "true");

			appiumOptions.AddAdditionalAppiumOption(MobileCapabilityType.NewCommandTimeout, 3000);
		}

		public void Dispose()
		{
			foreach (var context in _contexts)
			{
				context.Dispose();
			}

			_contexts.Clear();

			_server?.Dispose();
			_server = null;
		}
	}
}
