using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Geometry = Microsoft.Maui.Controls.Shapes.Geometry;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// An <see cref="Element" /> that occupies an area on the screen, has a visual appearance, and can obtain touch input.
	/// </summary>
	/// <remarks>
	/// The base class for most Microsoft.Maui.Controls on-screen elements. Provides most properties, events, and methods for presenting an item on screen.
	/// </remarks>
	public partial class VisualElement : NavigableElement, IAnimatable, IVisualElementController, IResourcesProvider, IStyleElement, IFlowDirectionController, IPropertyPropagationController, IVisualController, IWindowController
	{
		/// <summary>Bindable property for <see cref="NavigableElement.Navigation"/>.</summary>
		public new static readonly BindableProperty NavigationProperty = NavigableElement.NavigationProperty;

		/// <summary>Bindable property for <see cref="NavigableElement.Style"/>.</summary>
		public new static readonly BindableProperty StyleProperty = NavigableElement.StyleProperty;

		/// <summary>Bindable property for <see cref="InputTransparent"/>.</summary>
		public static readonly BindableProperty InputTransparentProperty = BindableProperty.Create("InputTransparent", typeof(bool), typeof(VisualElement), default(bool));

		/// <summary>Bindable property for <see cref="IsEnabled"/>.</summary>
		public static readonly BindableProperty IsEnabledProperty = BindableProperty.Create("IsEnabled", typeof(bool),
			typeof(VisualElement), true, propertyChanged: OnIsEnabledPropertyChanged);

		static readonly BindablePropertyKey XPropertyKey = BindableProperty.CreateReadOnly("X", typeof(double), typeof(VisualElement), default(double));

		/// <summary>Bindable property for <see cref="X"/>.</summary>
		public static readonly BindableProperty XProperty = XPropertyKey.BindableProperty;

		static readonly BindablePropertyKey YPropertyKey = BindableProperty.CreateReadOnly("Y", typeof(double), typeof(VisualElement), default(double));

		/// <summary>Bindable property for <see cref="Y"/>.</summary>
		public static readonly BindableProperty YProperty = YPropertyKey.BindableProperty;

		/// <summary>Bindable property for <see cref="AnchorX"/>.</summary>
		public static readonly BindableProperty AnchorXProperty = BindableProperty.Create("AnchorX", typeof(double), typeof(VisualElement), .5d);

		/// <summary>Bindable property for <see cref="AnchorY"/>.</summary>
		public static readonly BindableProperty AnchorYProperty = BindableProperty.Create("AnchorY", typeof(double), typeof(VisualElement), .5d);

		/// <summary>Bindable property for <see cref="TranslationX"/>.</summary>
		public static readonly BindableProperty TranslationXProperty = BindableProperty.Create("TranslationX", typeof(double), typeof(VisualElement), 0d);

		/// <summary>Bindable property for <see cref="TranslationY"/>.</summary>
		public static readonly BindableProperty TranslationYProperty = BindableProperty.Create("TranslationY", typeof(double), typeof(VisualElement), 0d);

		static readonly BindablePropertyKey WidthPropertyKey = BindableProperty.CreateReadOnly("Width", typeof(double), typeof(VisualElement), -1d,
			coerceValue: (bindable, value) => double.IsNaN((double)value) ? 0d : value);

		/// <summary>Bindable property for <see cref="Width"/>.</summary>
		public static readonly BindableProperty WidthProperty = WidthPropertyKey.BindableProperty;

		static readonly BindablePropertyKey HeightPropertyKey = BindableProperty.CreateReadOnly("Height", typeof(double), typeof(VisualElement), -1d,
			coerceValue: (bindable, value) => double.IsNaN((double)value) ? 0d : value);

		/// <summary>Bindable property for <see cref="Height"/>.</summary>
		public static readonly BindableProperty HeightProperty = HeightPropertyKey.BindableProperty;

		/// <summary>Bindable property for <see cref="Rotation"/>.</summary>
		public static readonly BindableProperty RotationProperty = BindableProperty.Create("Rotation", typeof(double), typeof(VisualElement), default(double));

		/// <summary>Bindable property for <see cref="RotationX"/>.</summary>
		public static readonly BindableProperty RotationXProperty = BindableProperty.Create("RotationX", typeof(double), typeof(VisualElement), default(double));

		/// <summary>Bindable property for <see cref="RotationY"/>.</summary>
		public static readonly BindableProperty RotationYProperty = BindableProperty.Create("RotationY", typeof(double), typeof(VisualElement), default(double));

		/// <summary>Bindable property for <see cref="Scale"/>.</summary>
		public static readonly BindableProperty ScaleProperty = BindableProperty.Create(nameof(Scale), typeof(double), typeof(VisualElement), 1d);

		/// <summary>Bindable property for <see cref="ScaleX"/>.</summary>
		public static readonly BindableProperty ScaleXProperty = BindableProperty.Create(nameof(ScaleX), typeof(double), typeof(VisualElement), 1d);

		/// <summary>Bindable property for <see cref="ScaleY"/>.</summary>
		public static readonly BindableProperty ScaleYProperty = BindableProperty.Create(nameof(ScaleY), typeof(double), typeof(VisualElement), 1d);

		internal static readonly BindableProperty TransformProperty = BindableProperty.Create("Transform", typeof(string), typeof(VisualElement), null, propertyChanged: OnTransformChanged);

		/// <summary>Bindable property for <see cref="Clip"/>.</summary>
		public static readonly BindableProperty ClipProperty = BindableProperty.Create(nameof(Clip), typeof(Geometry), typeof(VisualElement), null,
			propertyChanging: (bindable, oldvalue, newvalue) =>
			{
				if (oldvalue != null)
					(bindable as VisualElement)?.StopNotifyingClipChanges();
			},
			propertyChanged: (bindable, oldvalue, newvalue) =>
			{
				if (newvalue != null)
					(bindable as VisualElement)?.NotifyClipChanges();
			});

		void NotifyClipChanges()
		{
			if (Clip != null)
			{
				Clip.PropertyChanged += OnClipChanged;

				if (Clip is GeometryGroup geometryGroup)
					geometryGroup.InvalidateGeometryRequested += InvalidateGeometryRequested;
			}
		}

		void StopNotifyingClipChanges()
		{
			if (Clip != null)
			{
				Clip.PropertyChanged -= OnClipChanged;

				if (Clip is GeometryGroup geometryGroup)
					geometryGroup.InvalidateGeometryRequested -= InvalidateGeometryRequested;
			}
		}

		void OnClipChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(Clip));
		}

		void InvalidateGeometryRequested(object sender, EventArgs e)
		{
			OnPropertyChanged(nameof(Clip));
		}

		/// <summary>Bindable property for <see cref="Visual"/>.</summary>
		public static readonly BindableProperty VisualProperty =
			BindableProperty.Create(nameof(Visual), typeof(IVisual), typeof(VisualElement), VisualMarker.MatchParent,
									validateValue: (b, v) => v != null, propertyChanged: OnVisualChanged);

		static IVisual _defaultVisual = VisualMarker.Default;
		IVisual _effectiveVisual = _defaultVisual;

		/// <summary>
		/// Gets or sets a <see cref="IVisual"/> implementation that overrides the visual appearance of an element. This is a bindable property.
		/// </summary>
		[TypeConverter(typeof(VisualTypeConverter))]
		public IVisual Visual
		{
			get { return (IVisual)GetValue(VisualProperty); }
			set { SetValue(VisualProperty, value); }
		}

		internal static void SetDefaultVisual(IVisual visual) => _defaultVisual = visual;

		/// <inheritdoc/>
		IVisual IVisualController.EffectiveVisual
		{
			get { return _effectiveVisual; }
			set
			{
				if (value == _effectiveVisual)
					return;

				_effectiveVisual = value;
				OnPropertyChanged(VisualProperty.PropertyName);
			}
		}

		static void OnTransformChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if ((string)newValue == "none")
			{
				bindable.ClearValue(TranslationXProperty);
				bindable.ClearValue(TranslationYProperty);
				bindable.ClearValue(RotationProperty);
				bindable.ClearValue(RotationXProperty);
				bindable.ClearValue(RotationYProperty);
				bindable.ClearValue(ScaleProperty);
				bindable.ClearValue(ScaleXProperty);
				bindable.ClearValue(ScaleYProperty);
				return;
			}
			var transforms = ((string)newValue).Split(' ');
			foreach (var transform in transforms)
			{
				if (string.IsNullOrEmpty(transform) || transform.IndexOf("(", StringComparison.Ordinal) < 0 || transform.IndexOf(")", StringComparison.Ordinal) < 0)
					throw new FormatException("Format for transform is 'none | transform(value) [transform(value) ]*'");
				var transformName = transform.Substring(0, transform.IndexOf("(", StringComparison.Ordinal));
				var value = transform.Substring(transform.IndexOf("(", StringComparison.Ordinal) + 1, transform.IndexOf(")", StringComparison.Ordinal) - transform.IndexOf("(", StringComparison.Ordinal) - 1);
				double translationX, translationY, scaleX, scaleY, rotateX, rotateY, rotate;
				if (transformName.StartsWith("translateX", StringComparison.OrdinalIgnoreCase) && double.TryParse(value, out translationX))
					bindable.SetValue(TranslationXProperty, translationX);
				else if (transformName.StartsWith("translateY", StringComparison.OrdinalIgnoreCase) && double.TryParse(value, out translationY))
					bindable.SetValue(TranslationYProperty, translationY);
				else if (transformName.StartsWith("translate", StringComparison.OrdinalIgnoreCase))
				{
					var translate = value.Split(',');
					if (double.TryParse(translate[0], out translationX) && double.TryParse(translate[1], out translationY))
					{
						bindable.SetValue(TranslationXProperty, translationX);
						bindable.SetValue(TranslationYProperty, translationY);
					}
				}
				else if (transformName.StartsWith("scaleX", StringComparison.OrdinalIgnoreCase) && double.TryParse(value, out scaleX))
					bindable.SetValue(ScaleXProperty, scaleX);
				else if (transformName.StartsWith("scaleY", StringComparison.OrdinalIgnoreCase) && double.TryParse(value, out scaleY))
					bindable.SetValue(ScaleYProperty, scaleY);
				else if (transformName.StartsWith("scale", StringComparison.OrdinalIgnoreCase))
				{
					var scale = value.Split(',');
					if (double.TryParse(scale[0], out scaleX) && double.TryParse(scale[1], out scaleY))
					{
						bindable.SetValue(ScaleXProperty, scaleX);
						bindable.SetValue(ScaleYProperty, scaleY);
					}
				}
				else if (transformName.StartsWith("rotateX", StringComparison.OrdinalIgnoreCase) && double.TryParse(value, out rotateX))
					bindable.SetValue(RotationXProperty, rotateX);
				else if (transformName.StartsWith("rotateY", StringComparison.OrdinalIgnoreCase) && double.TryParse(value, out rotateY))
					bindable.SetValue(RotationYProperty, rotateY);
				else if (transformName.StartsWith("rotate", StringComparison.OrdinalIgnoreCase) && double.TryParse(value, out rotate))
					bindable.SetValue(RotationProperty, rotate);
				else
					throw new FormatException("Invalid transform name");
			}
		}

		internal static readonly BindableProperty TransformOriginProperty =
			BindableProperty.Create("TransformOrigin", typeof(Point), typeof(VisualElement), new Point(.5d, .5d),
									propertyChanged: (b, o, n) => { (((VisualElement)b).AnchorX, ((VisualElement)b).AnchorY) = (Point)n; });

		/// <summary>Bindable property for <see cref="IsVisible"/>.</summary>
		public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create("IsVisible", typeof(bool), typeof(VisualElement), true,
			propertyChanged: (bindable, oldvalue, newvalue) => ((VisualElement)bindable).OnIsVisibleChanged((bool)oldvalue, (bool)newvalue));

		/// <summary>Bindable property for <see cref="Opacity"/>.</summary>
		public static readonly BindableProperty OpacityProperty = BindableProperty.Create("Opacity", typeof(double), typeof(VisualElement), 1d, coerceValue: (bindable, value) => ((double)value).Clamp(0, 1));

		/// <summary>Bindable property for <see cref="BackgroundColor"/>.</summary>
		public static readonly BindableProperty BackgroundColorProperty = BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(VisualElement), null);

		/// <summary>Bindable property for <see cref="Background"/>.</summary>
		public static readonly BindableProperty BackgroundProperty = BindableProperty.Create(nameof(Background), typeof(Brush), typeof(VisualElement), Brush.Default,
			propertyChanging: (bindable, oldvalue, newvalue) =>
			{
				if (oldvalue != null)
					(bindable as VisualElement)?.StopNotifyingBackgroundChanges();
			},
			propertyChanged: (bindable, oldvalue, newvalue) =>
			{
				if (newvalue != null)
					(bindable as VisualElement)?.NotifyBackgroundChanges();
			});

		void NotifyBackgroundChanges()
		{
			if (Background is ImmutableBrush)
				return;

			if (Background != null)
			{
				Background.Parent = this;
				Background.PropertyChanged += OnBackgroundChanged;

				if (Background is GradientBrush gradientBrush)
					gradientBrush.InvalidateGradientBrushRequested += InvalidateGradientBrushRequested;
			}
		}

		void StopNotifyingBackgroundChanges()
		{
			if (Background is ImmutableBrush)
				return;

			if (Background != null)
			{
				Background.Parent = null;
				Background.PropertyChanged -= OnBackgroundChanged;

				if (Background is GradientBrush gradientBrush)
					gradientBrush.InvalidateGradientBrushRequested -= InvalidateGradientBrushRequested;
			}
		}

		void OnBackgroundChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(Background));
		}

		void InvalidateGradientBrushRequested(object sender, EventArgs e)
		{
			OnPropertyChanged(nameof(Background));
		}

		internal static readonly BindablePropertyKey BehaviorsPropertyKey = BindableProperty.CreateReadOnly("Behaviors", typeof(IList<Behavior>), typeof(VisualElement), default(IList<Behavior>),
			defaultValueCreator: bindable =>
			{
				var collection = new AttachedCollection<Behavior>();
				collection.AttachTo(bindable);
				return collection;
			});

		/// <summary>Bindable property for <see cref="Behaviors"/>.</summary>
		public static readonly BindableProperty BehaviorsProperty = BehaviorsPropertyKey.BindableProperty;

		internal static readonly BindablePropertyKey TriggersPropertyKey = BindableProperty.CreateReadOnly("Triggers", typeof(IList<TriggerBase>), typeof(VisualElement), default(IList<TriggerBase>),
			defaultValueCreator: bindable =>
			{
				var collection = new AttachedCollection<TriggerBase>();
				collection.AttachTo(bindable);
				return collection;
			});

		/// <summary>Bindable property for <see cref="Triggers"/>.</summary>
		public static readonly BindableProperty TriggersProperty = TriggersPropertyKey.BindableProperty;


		/// <summary>Bindable property for <see cref="WidthRequest"/>.</summary>

		public static readonly BindableProperty WidthRequestProperty = BindableProperty.Create(nameof(WidthRequest), typeof(double), typeof(VisualElement), -1d, propertyChanged: OnRequestChanged);

		/// <summary>Bindable property for <see cref="HeightRequest"/>.</summary>
		public static readonly BindableProperty HeightRequestProperty = BindableProperty.Create(nameof(HeightRequest), typeof(double), typeof(VisualElement), -1d, propertyChanged: OnRequestChanged);

		/// <summary>Bindable property for <see cref="MinimumWidthRequest"/>.</summary>
		public static readonly BindableProperty MinimumWidthRequestProperty = BindableProperty.Create(nameof(MinimumWidthRequest), typeof(double), typeof(VisualElement), -1d, propertyChanged: OnRequestChanged);

		/// <summary>Bindable property for <see cref="MinimumHeightRequest"/>.</summary>
		public static readonly BindableProperty MinimumHeightRequestProperty = BindableProperty.Create(nameof(MinimumHeightRequest), typeof(double), typeof(VisualElement), -1d, propertyChanged: OnRequestChanged);

		/// <summary>Bindable property for <see cref="MaximumWidthRequest"/>.</summary>
		public static readonly BindableProperty MaximumWidthRequestProperty = BindableProperty.Create(nameof(MaximumWidthRequest), typeof(double), typeof(VisualElement), double.PositiveInfinity, propertyChanged: OnRequestChanged);

		/// <summary>Bindable property for <see cref="MaximumHeightRequest"/>.</summary>
		public static readonly BindableProperty MaximumHeightRequestProperty = BindableProperty.Create(nameof(MaximumHeightRequest), typeof(double), typeof(VisualElement), double.PositiveInfinity, propertyChanged: OnRequestChanged);

		/// <summary>Bindable property for <see cref="IsFocused"/>.</summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static readonly BindablePropertyKey IsFocusedPropertyKey = BindableProperty.CreateReadOnly("IsFocused",
			typeof(bool), typeof(VisualElement), default(bool), propertyChanged: OnIsFocusedPropertyChanged);

		/// <summary>Bindable property for <see cref="IsFocused"/>.</summary>
		public static readonly BindableProperty IsFocusedProperty = IsFocusedPropertyKey.BindableProperty;

		/// <summary>Bindable property for <see cref="FlowDirection"/>.</summary>
		public static readonly BindableProperty FlowDirectionProperty = BindableProperty.Create(nameof(FlowDirection), typeof(FlowDirection), typeof(VisualElement), FlowDirection.MatchParent, propertyChanging: FlowDirectionChanging, propertyChanged: FlowDirectionChanged);

		IFlowDirectionController FlowController => this;

		/// <summary>
		/// Gets or sets the layout flow direction. This is a bindable property.
		/// </summary>
		[TypeConverter(typeof(FlowDirectionConverter))]
		public FlowDirection FlowDirection
		{
			get { return (FlowDirection)GetValue(FlowDirectionProperty); }
			set { SetValue(FlowDirectionProperty, value); }
		}

		EffectiveFlowDirection _effectiveFlowDirection = default(EffectiveFlowDirection);
		EffectiveFlowDirection IFlowDirectionController.EffectiveFlowDirection
		{
			get => _effectiveFlowDirection;
			set => SetEffectiveFlowDirection(value, true);
		}

		void SetEffectiveFlowDirection(EffectiveFlowDirection value, bool fireFlowDirectionPropertyChanged)
		{
			if (value == _effectiveFlowDirection)
				return;

			_effectiveFlowDirection = value;
			InvalidateMeasureInternal(InvalidationTrigger.Undefined);

			if (fireFlowDirectionPropertyChanged)
				OnPropertyChanged(FlowDirectionProperty.PropertyName);

		}

		/// <inheritdoc/>
		EffectiveFlowDirection IVisualElementController.EffectiveFlowDirection => FlowController.EffectiveFlowDirection;

		static readonly BindablePropertyKey WindowPropertyKey = BindableProperty.CreateReadOnly(
			nameof(Window), typeof(Window), typeof(VisualElement), null, propertyChanged: OnWindowChanged);

		/// <summary>Bindable property for <see cref="Window"/>.</summary>
		public static readonly BindableProperty WindowProperty = WindowPropertyKey.BindableProperty;

		/// <summary>
		/// Gets the <see cref="Window"/> that is associated with an element. This is a read-only bindable property.
		/// </summary>
		public Window Window => (Window)GetValue(WindowProperty);

		/// <inheritdoc/>
		Window IWindowController.Window
		{
			get => (Window)GetValue(WindowProperty);
			set => SetValue(WindowPropertyKey, value);
		}

		readonly Dictionary<Size, SizeRequest> _measureCache = new Dictionary<Size, SizeRequest>();

		int _batched;
		LayoutConstraint _computedConstraint;

		bool _isInPlatformLayout;

		bool _isPlatformStateConsistent = true;

		bool _isPlatformEnabled;

		double _mockHeight = -1;

		double _mockWidth = -1;

		double _mockX = -1;

		double _mockY = -1;

		LayoutConstraint _selfConstraint;

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualElement"/> class.
		/// </summary>
		protected internal VisualElement()
		{
		}

		/// <summary>
		/// Gets or sets the X component of the center point for any transform operation, relative to the bounds of the element. This is a bindable property.
		/// </summary>
		/// <remarks>The default value is 0.5.</remarks>
		public double AnchorX
		{
			get { return (double)GetValue(AnchorXProperty); }
			set { SetValue(AnchorXProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Y component of the center point for any transform operation, relative to the bounds of the element. This is a bindable property.
		/// </summary>
		/// <remarks>The default value is 0.5.</remarks>
		public double AnchorY
		{
			get { return (double)GetValue(AnchorYProperty); }
			set { SetValue(AnchorYProperty, value); }
		}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> which will fill the background of an element. This is a bindable property.
		/// </summary>
		/// <remarks>For background gradients and such, use <see cref="Background"/>.</remarks>
		public Color BackgroundColor
		{
			get { return (Color)GetValue(BackgroundColorProperty); }
			set { SetValue(BackgroundColorProperty, value); }
		}

		/// <summary>
		/// Gets or sets the <see cref="Brush"/> which will be used to fill the background of an element. This is a bindable property.
		/// </summary>
		[TypeConverter(typeof(BrushTypeConverter))]
		public Brush Background
		{
			get { return (Brush)GetValue(BackgroundProperty); }
			set { SetValue(BackgroundProperty, value); }
		}

		/// <summary>
		/// Gets the list of <see cref="Behavior"/> objects associated to this element. This is a read-only bindable property.
		/// </summary>
		public IList<Behavior> Behaviors
		{
			get { return (IList<Behavior>)GetValue(BehaviorsProperty); }
		}

		/// <summary>
		/// Gets the bounds of the element in device-independent units.
		/// </summary>
		/// <remarks><see cref="Bounds"/> is assigned during layout.</remarks>
		public Rect Bounds
		{
			get { return IsMocked() ? new Rect(_mockX, _mockY, _mockWidth, _mockHeight) : _frame; }
			private set
			{
				Frame = value;
			}
		}

		/// <summary>
		/// Gets the current rendered height of this element. This is a read-only bindable property.
		/// </summary>
		/// <remarks>The height of an element is set during layout.</remarks>
		public double Height
		{
			get { return _mockHeight == -1 ? (double)GetValue(HeightProperty) : _mockHeight; }
			private set { SetValue(HeightPropertyKey, value); }
		}

		/// <summary>
		/// Gets or sets the desired height override of this element. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is -1, which means the value is unset; the effective minimum height will be zero.</para>
		/// <para><see cref="HeightRequest"/> does not immediately change the <see cref="Bounds"/> of an element; setting the <see cref="HeightRequest"/> will change the resulting height of the element during the next layout pass.</para>
		/// </remarks>
		public double HeightRequest
		{
			get { return (double)GetValue(HeightRequestProperty); }
			set { SetValue(HeightRequestProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this element responds to hit testing during user interaction. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is <see langword="false"/>.</para>
		/// <para>Setting <see cref="InputTransparent"/> to <see langword="true"/> makes the element invisible to touch and pointer input. The input is passed to the first non-input-transparent element that is visually behind the input transparent element. </para>
		/// </remarks>
		public bool InputTransparent
		{
			get { return (bool)GetValue(InputTransparentProperty); }
			set { SetValue(InputTransparentProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this element is enabled in the user interface. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is <see langword="true"/>.</para>
		/// <para>Elements that are not enabled will not receive focus or respond to input events.</para>
		/// </remarks>
		public bool IsEnabled
		{
			get { return (bool)GetValue(IsEnabledProperty); }
			set { SetValue(IsEnabledProperty, value); }
		}

		/// <summary>
		/// Gets a value indicating whether this element is focused currently. This is a bindable property.
		/// </summary>
		/// <remarks>Applications may have multiple focuses depending on the implementation of the underlying platform. Menus and modals in particular may leave multiple items with focus.</remarks>
		public bool IsFocused => (bool)GetValue(IsFocusedProperty);

		/// <summary>
		/// Gets or sets a value that determines whether this element will be visible on screen and take up space in layouts. This is a bindable property.
		/// </summary>
		/// <remarks>When an element has <see cref="IsVisible"/> set to <see langword="false"/> it will no longer take up space in layouts or be eligible to receive any kind of input event.</remarks>
		[TypeConverter(typeof(VisibilityConverter))]
		public bool IsVisible
		{
			get { return (bool)GetValue(IsVisibleProperty); }
			set { SetValue(IsVisibleProperty, value); }
		}

		/// <summary>
		/// Gets or sets the minimum height the element will request during layout. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is -1, which means the value is unset and a height will be determined automatically.</para>
		/// <para><see cref="MinimumHeightRequest"/> is used to ensure that the element has at least the specified height during layout.</para>
		/// </remarks>
		public double MinimumHeightRequest
		{
			get { return (double)GetValue(MinimumHeightRequestProperty); }
			set { SetValue(MinimumHeightRequestProperty, value); }
		}

		/// <summary>
		/// Gets or sets the minimum width the element will request during layout. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is -1, which means the value is unset; the effective minimum width will be zero.</para>
		/// <para><see cref="MinimumWidthRequest"/> is used to ensure that the element has at least the specified width during layout.</para>
		/// </remarks>
		public double MinimumWidthRequest
		{
			get { return (double)GetValue(MinimumWidthRequestProperty); }
			set { SetValue(MinimumWidthRequestProperty, value); }
		}

		/// <summary>
		/// Gets or sets the maximum height the element will request during layout. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is <see cref="double.PositiveInfinity"/>.</para>
		/// <para><see cref="MaximumHeightRequest"/> is used to ensure that the element has no more than the specified height during layout.</para>
		/// </remarks>
		public double MaximumHeightRequest
		{
			get { return (double)GetValue(MaximumHeightRequestProperty); }
			set { SetValue(MaximumHeightRequestProperty, value); }
		}

		/// <summary>
		/// Gets or sets the maximum width the element will request during layout. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is <see cref="double.PositiveInfinity"/>.</para>
		/// <para><see cref="MaximumWidthRequest"/> is used to ensure the element has no more than the specified width during layout.</para>
		/// </remarks>
		public double MaximumWidthRequest
		{
			get { return (double)GetValue(MaximumWidthRequestProperty); }
			set { SetValue(MaximumWidthRequestProperty, value); }
		}

		/// <summary>
		/// Gets or sets the opacity value applied to the element when it is rendered. The range of this value is 0 to 1; values outside this range will be set to the nearest valid value. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is 1.0.</para>
		/// <para>
		/// The opacity value has no effect unless <see cref="IsVisible"/> is <see langword="true"/>. The effective opacity of an element is the value of <see cref="Opacity"/> multiplied by the opacity of the element's <c>Parent</c>. If a parent has 0.5 opacity, and a child has 0.5 opacity, the child will render with an effective 0.25 opacity.
		/// </para>
		/// </remarks>
		public double Opacity
		{
			get { return (double)GetValue(OpacityProperty); }
			set { SetValue(OpacityProperty, value); }
		}

		/// <summary>
		/// Gets or sets the rotation (in degrees) about the Z-axis (affine rotation) when the element is rendered. This is a bindable property.
		/// </summary>
		/// <remarks>Rotation is applied relative to <see cref="AnchorX" /> and <see cref="AnchorY" />.</remarks>
		public double Rotation
		{
			get { return (double)GetValue(RotationProperty); }
			set { SetValue(RotationProperty, value); }
		}

		/// <summary>
		/// Gets or sets the rotation (in degrees) about the X-axis (perspective rotation) when the element is rendered. This is a bindable property.
		/// </summary>
		/// <remarks>Rotation is applied relative to <see cref="AnchorX" /> and <see cref="AnchorY" />.</remarks>
		public double RotationX
		{
			get { return (double)GetValue(RotationXProperty); }
			set { SetValue(RotationXProperty, value); }
		}

		/// <summary>
		/// Gets or sets the rotation (in degrees) about the Y-axis (perspective rotation) when the element is rendered. This is a bindable property.
		/// </summary>
		/// <remarks>Rotation is applied relative to <see cref="AnchorX" /> and <see cref="AnchorY" />.</remarks>
		public double RotationY
		{
			get { return (double)GetValue(RotationYProperty); }
			set { SetValue(RotationYProperty, value); }
		}

		/// <summary>
		/// Gets or sets the scale factor applied to the element. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>Default value is 1.0.</para>
		/// <para><see cref="Scale"/> is applied relative to <see cref="AnchorX" /> and <see cref="AnchorY" />.</para>
		/// </remarks>
		public double Scale
		{
			get => (double)GetValue(ScaleProperty);
			set => SetValue(ScaleProperty, value);
		}

		/// <summary>
		/// Gets or sets a scale value to apply to the X direction. This is a bindable property.
		/// </summary>
		/// <remarks>The default value is 1.0.</remarks>
		public double ScaleX
		{
			get => (double)GetValue(ScaleXProperty);
			set => SetValue(ScaleXProperty, value);
		}

		/// <summary>
		/// Gets or sets a scale value to apply to the Y direction. This is a bindable property.
		/// </summary>
		/// <remarks>The default value is 1.0.</remarks>
		public double ScaleY
		{
			get => (double)GetValue(ScaleYProperty);
			set => SetValue(ScaleYProperty, value);
		}

		/// <summary>
		/// Gets or sets the X translation delta of the element. This is a bindable property.
		/// </summary>
		/// <remarks>Translation is applied post layout. It is particularly good for applying animations. Translating an element outside the bounds of its parent container may prevent inputs from working.</remarks>
		public double TranslationX
		{
			get { return (double)GetValue(TranslationXProperty); }
			set { SetValue(TranslationXProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Y translation delta of the element. This is a bindable property.
		/// </summary>
		/// <remarks>Translation is applied post layout. It is particularly good for applying animations. Translating an element outside the bounds of its parent container may prevent inputs from working.</remarks>
		public double TranslationY
		{
			get { return (double)GetValue(TranslationYProperty); }
			set { SetValue(TranslationYProperty, value); }
		}

		/// <summary>
		/// Gets the list of <see cref="TriggerBase"/> objects associated to this element. This is a read-only bindable property.
		/// </summary>
		public IList<TriggerBase> Triggers => (IList<TriggerBase>)GetValue(TriggersProperty);

		/// <summary>
		/// Gets the current width of this element. This is a read-only bindable property.
		/// </summary>
		/// <remarks>The width value of an element is set during the layout cycle.</remarks>
		public double Width
		{
			get { return _mockWidth == -1 ? (double)GetValue(WidthProperty) : _mockWidth; }
			private set { SetValue(WidthPropertyKey, value); }
		}

		/// <summary>
		/// Gets or sets the desired width override of this element. This is a bindable property.
		/// </summary>
		/// <remarks>
		/// <para>The default value is -1, which means the value is unset and a width will be determined automatically.</para>
		/// <para><see cref="WidthRequest"/> does not immediately change the <see cref="Bounds"/> of an element; setting the <see cref="WidthRequest"/> will change the resulting width of the element during the next layout pass.</para>
		/// </remarks>
		public double WidthRequest
		{
			get { return (double)GetValue(WidthRequestProperty); }
			set { SetValue(WidthRequestProperty, value); }
		}

		/// <summary>
		/// Gets the current X position of this element. This is a read-only bindable property.
		/// </summary>
		/// <remarks>The position of an element is set during layout.</remarks>
		public double X
		{
			get { return _mockX == -1 ? (double)GetValue(XProperty) : _mockX; }
			private set { SetValue(XPropertyKey, value); }
		}

		/// <summary>
		/// Gets the current Y position of this element. This is a read-only bindable property.
		/// </summary>
		/// <remarks>The position of an element is set during layout.</remarks>
		public double Y
		{
			get { return _mockY == -1 ? (double)GetValue(YProperty) : _mockY; }
			private set { SetValue(YPropertyKey, value); }
		}

		/// <summary>
		/// Specifies the clipping region for an element. This is a bindable property.
		/// </summary>
		/// <remarks>When an element is rendered, only the portion of the element that falls inside the clipping <see cref="Geometry"/> is displayed, while any content that extends outside the clipping region is clipped (that is, not displayed).</remarks>
		[TypeConverter(typeof(PathGeometryConverter))]
		public Geometry Clip
		{
			get { return (Geometry)GetValue(ClipProperty); }
			set { SetValue(ClipProperty, value); }
		}

		/// <summary>
		/// Gets a value that indicates there are batched changes done for this element.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool Batched => _batched > 0;

		internal LayoutConstraint ComputedConstraint
		{
			get { return _computedConstraint; }
			set
			{
				if (_computedConstraint == value)
					return;

				LayoutConstraint oldConstraint = Constraint;
				_computedConstraint = value;
				LayoutConstraint newConstraint = Constraint;
				if (oldConstraint != newConstraint)
					OnConstraintChanged(oldConstraint, newConstraint);
			}
		}

		internal LayoutConstraint Constraint => ComputedConstraint | SelfConstraint;

		/// <summary>
		/// Gets a value that indicates that layout for this element is disabled.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool DisableLayout { get; set; }

		/// <summary>
		/// Gets or sets a value that indicates that this element is currently going through the platform layout cycle.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool IsInPlatformLayout
		{
			get
			{
				if (_isInPlatformLayout)
					return true;

				Element parent = RealParent;
				if (parent != null)
				{
					var visualElement = parent as VisualElement;
					if (visualElement != null && visualElement.IsInPlatformLayout)
						return true;
				}

				return false;
			}
			set { _isInPlatformLayout = value; }
		}

		/// <summary>
		/// Gets or sets a value that indicates that this element is currently consistent with the platform equivalent element state.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool IsPlatformStateConsistent
		{
			get { return _isPlatformStateConsistent; }
			set
			{
				if (_isPlatformStateConsistent == value)
					return;
				_isPlatformStateConsistent = value;
				if (value && IsPlatformEnabled)
					InvalidateMeasureInternal(InvalidationTrigger.RendererReady);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal event EventHandler PlatformEnabledChanged;

		/// <summary>
		/// Gets or sets a value that indicates whether this elements's platform equivalent element is enabled.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool IsPlatformEnabled
		{
			get { return _isPlatformEnabled; }
			set
			{
				if (value == _isPlatformEnabled)
					return;

				_isPlatformEnabled = value;
				if (value && IsPlatformStateConsistent)
					InvalidateMeasureInternal(InvalidationTrigger.RendererReady);

				OnIsPlatformEnabledChanged();
				PlatformEnabledChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		internal LayoutConstraint SelfConstraint
		{
			get { return _selfConstraint; }
			set
			{
				if (_selfConstraint == value)
					return;

				LayoutConstraint oldConstraint = Constraint;
				_selfConstraint = value;
				LayoutConstraint newConstraint = Constraint;
				if (oldConstraint != newConstraint)
				{
					OnConstraintChanged(oldConstraint, newConstraint);
				}
			}
		}

		/// <summary>
		/// Signals the start of a batch of changes to the elements properties. This can benefit performance if a bigger number of property values are changed.
		/// </summary>
		/// <remarks>
		/// <para>Application authors will generally not need to batch updates manually as the animation framework will do this for them.</para>
		/// <para>When the operation is done, <see cref="BatchCommit"/> should be called.</para>
		/// </remarks>
		public void BatchBegin() => _batched++;

		/// <summary>
		/// Signals the end of a batch of commands to the element and that those commands should now be committed.
		/// </summary>
		/// <remarks>This method only ensures that updates sent during the batch have been committed. It does not ensure that they were not committed before calling this.</remarks>
		public void BatchCommit()
		{
			_batched = Math.Max(0, _batched - 1);
			if (!Batched)
			{
				BatchCommitted?.Invoke(this, new EventArg<VisualElement>(this));
			}
		}

		ResourceDictionary _resources;
		bool IResourcesProvider.IsResourcesCreated => _resources != null;

		/// <summary>
		/// Gets or sets the local resource dictionary.
		/// </summary>
		/// <remarks>
		/// <para>In XAML, resource dictionaries are filled with key/value pairs that are specified in XAML and consequently created at runtime. The keys in the resource dictionary are specified with the <c>x:Key</c> attribute of the XAML tag for the type to create. An object of that type is created, and is initialized with the property and field values that are specified either by additional attributes or by nested tags, both of which, when present, are simply string representations of the property or field names. The object is then inserted into the <see cref="ResourceDictionary" /> for the enclosing type at runtime.</para>
		/// <para>Resource dictionaries and their associated XML provide the application developer with a convenient method to reuse code inside the XAML compile-time and runtime engines.</para>
		/// <para>For more information, see: <see href="https://learn.microsoft.com/dotnet/maui/fundamentals/resource-dictionaries">Resource Dictionaries (Microsoft Learn)</see>.</para>
		/// </remarks>
		public ResourceDictionary Resources
		{
			get
			{
				if (_resources != null)
					return _resources;
				_resources = new ResourceDictionary();
				((IResourceDictionary)_resources).ValuesChanged += OnResourcesChanged;
				return _resources;
			}
			set
			{
				if (_resources == value)
					return;
				OnPropertyChanging();
				if (_resources != null)
					((IResourceDictionary)_resources).ValuesChanged -= OnResourcesChanged;
				_resources = value;
				OnResourcesChanged(value);
				if (_resources != null)
					((IResourceDictionary)_resources).ValuesChanged += OnResourcesChanged;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Signals that the platform equivalent element for this element's size has changed and a new layout cycle might be needed.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void PlatformSizeChanged() => InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);

		/// <summary>
		/// Occurs when the order of this element's children changes.
		/// </summary>
		public event EventHandler ChildrenReordered;

		/// <summary>
		/// Attempts to set focus to this element.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the keyboard focus was set to this element; <see langword = "false" /> if the call to this method did not force a focus change.
		/// </returns>
		/// <remarks>Element must be able to receive focus for this to work. Calling <see cref="Focus"/> on offscreen or unrealized elements has undefined behavior.</remarks>
		public bool Focus()
		{
			if (IsFocused)
			{
#if ANDROID
				// TODO: Refactor using mappers for .NET 8
				if (this is ITextInput && Handler is IPlatformViewHandler platformViewHandler)
					KeyboardManager.ShowKeyboard(platformViewHandler.PlatformView);
#endif
				return true;
			}

			if (FocusChangeRequested == null)
			{
				FocusRequest focusRequest = new FocusRequest(false);

				Handler?.Invoke(nameof(IView.Focus), focusRequest);

				return focusRequest.IsFocused;
			}

			var arg = new FocusRequestArgs { Focus = true };
			FocusChangeRequested(this, arg);
			return arg.Result;
		}

		/// <summary>
		/// Occurs when this element is focused.
		/// </summary>
		public event EventHandler<FocusEventArgs> Focused;

		SizeRequest GetSizeRequest(double widthConstraint, double heightConstraint)
		{
			var constraintSize = new Size(widthConstraint, heightConstraint);
			if (_measureCache.TryGetValue(constraintSize, out SizeRequest cachedResult))
				return cachedResult;

			double widthRequest = WidthRequest;
			double heightRequest = HeightRequest;
			if (widthRequest >= 0)
				widthConstraint = Math.Min(widthConstraint, widthRequest);
			if (heightRequest >= 0)
				heightConstraint = Math.Min(heightConstraint, heightRequest);

			SizeRequest result = OnMeasure(widthConstraint, heightConstraint);
			bool hasMinimum = result.Minimum != result.Request;
			Size request = result.Request;
			Size minimum = result.Minimum;

			if (heightRequest != -1 && !double.IsNaN(heightRequest))
			{
				request.Height = heightRequest;
				if (!hasMinimum)
					minimum.Height = heightRequest;
			}

			if (widthRequest != -1 && !double.IsNaN(widthRequest))
			{
				request.Width = widthRequest;
				if (!hasMinimum)
					minimum.Width = widthRequest;
			}

			double minimumHeightRequest = MinimumHeightRequest;
			double minimumWidthRequest = MinimumWidthRequest;

			if (minimumHeightRequest != -1)
				minimum.Height = minimumHeightRequest;
			if (minimumWidthRequest != -1)
				minimum.Width = minimumWidthRequest;

			minimum.Height = Math.Min(request.Height, minimum.Height);
			minimum.Width = Math.Min(request.Width, minimum.Width);

			var r = new SizeRequest(request, minimum);

			if (r.Request.Width > 0 && r.Request.Height > 0)
				_measureCache[constraintSize] = r;

			return r;
		}

		/// <summary>
		/// Returns the minimum size that an element needs in order to be displayed on the device.
		/// </summary>
		/// <param name="widthConstraint">The suggested maximum width constraint for the element to render.</param>
		/// <param name="heightConstraint">The suggested maximum height constraint for the element to render.</param>
		/// <param name="flags">A value that controls whether margins are included in the returned size.</param>
		/// <returns>The minimum size that an element needs in order to be displayed on the device.</returns>
		/// <remarks>If the minimum size that the element needs in order to be displayed on the device is larger than can be accommodated by <paramref name="widthConstraint" /> and <paramref name="heightConstraint" />, the return value may represent a rectangle that is larger in either one or both of those parameters.</remarks>
		public virtual SizeRequest Measure(double widthConstraint, double heightConstraint, MeasureFlags flags = MeasureFlags.None)
		{
			bool includeMargins = (flags & MeasureFlags.IncludeMargins) != 0;
			Thickness margin = default(Thickness);
			if (includeMargins)
			{
				if (this is View view)
					margin = view.Margin;
				widthConstraint = Math.Max(0, widthConstraint - margin.HorizontalThickness);
				heightConstraint = Math.Max(0, heightConstraint - margin.VerticalThickness);
			}

			SizeRequest result = GetSizeRequest(widthConstraint, heightConstraint);

			if (includeMargins && !margin.IsEmpty)
			{
				result.Minimum = new Size(result.Minimum.Width + margin.HorizontalThickness, result.Minimum.Height + margin.VerticalThickness);
				result.Request = new Size(result.Request.Width + margin.HorizontalThickness, result.Request.Height + margin.VerticalThickness);
			}

			DesiredSize = result.Request;
			return result;
		}

		/// <summary>
		/// Occurs when the current measure of an element has been invalidated.
		/// </summary>
		public event EventHandler MeasureInvalidated;

		/// <summary>
		/// Occurs when the size of an element changed.
		/// </summary>
		public event EventHandler SizeChanged;

		/// <summary>
		/// Unsets keyboard focus on this element.
		/// </summary>
		/// <remarks>Element must already have focus for this to have any effect.</remarks>
		public void Unfocus()
		{
			if (!IsFocused)
				return;

			Handler?.Invoke(nameof(IView.Unfocus));
			FocusChangeRequested?.Invoke(this, new FocusRequestArgs());
		}

		/// <summary>
		/// Occurs when this element is unfocused.
		/// </summary>
		/// <remarks>This event is not triggered when the element does not currently have focus.</remarks>
		public event EventHandler<FocusEventArgs> Unfocused;

		/// <summary>
		/// Marks the current measure of an element as invalidated.
		/// </summary>
		protected virtual void InvalidateMeasure() => InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);

		protected override void OnBindingContextChanged()
		{
			PropagateBindingContextToStateTriggers();
			PropagateBindingContextToBrush();
			PropagateBindingContextToShadow();

			base.OnBindingContextChanged();
		}

		protected override void OnChildAdded(Element child)
		{
			base.OnChildAdded(child);

			var view = child as View;

			if (view != null)
				ComputeConstraintForView(view);
		}

		protected override void OnChildRemoved(Element child, int oldLogicalIndex)
		{
			base.OnChildRemoved(child, oldLogicalIndex);

			if (child is View view)
				view.ComputedConstraint = LayoutConstraint.None;
		}

		/// <summary>
		/// Raises the <see cref="ChildrenReordered"/> event.
		/// </summary>
		protected void OnChildrenReordered()
			=> ChildrenReordered?.Invoke(this, EventArgs.Empty);

		IPlatformSizeService _platformSizeService;

		/// <summary>
		/// Method that is called when a layout measurement happens.
		/// </summary>
		/// <param name="widthConstraint">The width constraint to request.</param>
		/// <param name="heightConstraint">The height constraint to request.</param>
		/// <returns>The requested size that the element requires in order to be displayed on the device.</returns>
		protected virtual SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			if (!IsPlatformEnabled)
				return new SizeRequest(new Size(-1, -1));

			if (Handler != null)
				return new SizeRequest(Handler.GetDesiredSize(widthConstraint, heightConstraint));

			_platformSizeService ??= DependencyService.Get<IPlatformSizeService>();
			return _platformSizeService.GetPlatformSize(this, widthConstraint, heightConstraint);
		}

		/// <summary>
		/// Method that is called when the size of the element is set during a layout cycle. Implement this method to add class handling for this event.
		/// </summary>
		/// <param name="width">The new width of the element.</param>
		/// <param name="height">The new height of the element.</param>
		protected virtual void OnSizeAllocated(double width, double height)
		{
		}

		/// <summary>
		/// Method that is called during a layout cycle to signal the start of a sub-tree layout.
		/// </summary>
		/// <param name="width">The newly allocated width.</param>
		/// <param name="height">The newly allocated height.</param>
		/// <remarks>Calling <see cref="SizeAllocated(double, double)"/> will start a new layout cycle on the children of the element. Excessive calls to this method may cause performance problems.</remarks>
		protected void SizeAllocated(double width, double height) => OnSizeAllocated(width, height);

		/// <summary>
		/// Occurs when a batch of property changes have been committed by calling <see cref="BatchCommit"/>.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public event EventHandler<EventArg<VisualElement>> BatchCommitted;

		internal void ComputeConstrainsForChildren()
		{
			for (var i = 0; i < LogicalChildrenInternal.Count; i++)
			{
				if (LogicalChildrenInternal[i] is View child)
					ComputeConstraintForView(child);
			}
		}

		internal virtual void ComputeConstraintForView(View view) => view.ComputedConstraint = LayoutConstraint.None;

		[EditorBrowsable(EditorBrowsableState.Never)]
		public event EventHandler<FocusRequestArgs> FocusChangeRequested;

		/// <summary>
		/// Invalidates the measure of an element.
		/// </summary>
		/// <remarks>For internal use only. This API can be changed or removed without notice at any time.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void InvalidateMeasureNonVirtual(InvalidationTrigger trigger)
		{
			InvalidateMeasureInternal(trigger);
		}

		internal virtual void InvalidateMeasureInternal(InvalidationTrigger trigger)
		{
			_measureCache.Clear();

			// TODO ezhart Once we get InvalidateArrange sorted, HorizontalOptionsChanged and 
			// VerticalOptionsChanged will need to call ParentView.InvalidateArrange() instead

			switch (trigger)
			{
				case InvalidationTrigger.MarginChanged:
				case InvalidationTrigger.HorizontalOptionsChanged:
				case InvalidationTrigger.VerticalOptionsChanged:
					ParentView?.InvalidateMeasure();
					break;
				default:
					(this as IView)?.InvalidateMeasure();
					break;
			}

			MeasureInvalidated?.Invoke(this, new InvalidationEventArgs(trigger));
		}

		/// <inheritdoc/>
		void IVisualElementController.InvalidateMeasure(InvalidationTrigger trigger) => InvalidateMeasureInternal(trigger);

		internal void InvalidateStateTriggers(bool attach)
		{
			if (!this.HasVisualStateGroups())
				return;

			var groups = (IList<VisualStateGroup>)GetValue(VisualStateManager.VisualStateGroupsProperty);

			if (groups.Count == 0)
				return;

			foreach (var group in groups)
				foreach (var state in group.States)
					foreach (var stateTrigger in state.StateTriggers)
					{
						if (attach)
							stateTrigger.SendAttached();
						else
							stateTrigger.SendDetached();
					}
		}

		internal void MockBounds(Rect bounds)
		{
#if NETSTANDARD2_0 || NET6_0_OR_GREATER
			(_mockX, _mockY, _mockWidth, _mockHeight) = bounds;
#else
			_mockX = bounds.X;
			_mockY = bounds.Y;
			_mockWidth = bounds.Width;
			_mockHeight = bounds.Height;
#endif
		}

		bool IsMocked()
		{
			return _mockX != -1 || _mockY != -1 || _mockWidth != -1 || _mockHeight != -1;
		}

		internal virtual void OnConstraintChanged(LayoutConstraint oldConstraint, LayoutConstraint newConstraint) => ComputeConstrainsForChildren();

		internal virtual void OnIsPlatformEnabledChanged()
		{
		}

		internal virtual void OnIsVisibleChanged(bool oldValue, bool newValue)
		{
			if (this is IView fe)
			{
				fe.Handler?.UpdateValue(nameof(IView.Visibility));
			}

			InvalidateMeasureInternal(InvalidationTrigger.Undefined);
		}

		internal override void OnParentResourcesChanged(IEnumerable<KeyValuePair<string, object>> values)
		{
			if (values == null)
				return;

			if (!((IResourcesProvider)this).IsResourcesCreated || Resources.Count == 0)
			{
				base.OnParentResourcesChanged(values);
				return;
			}

			var innerKeys = new HashSet<string>();
			var changedResources = new List<KeyValuePair<string, object>>();
			foreach (KeyValuePair<string, object> c in Resources)
				innerKeys.Add(c.Key);
			foreach (KeyValuePair<string, object> value in values)
			{
				if (innerKeys.Add(value.Key))
					changedResources.Add(value);
				else if (value.Key.StartsWith(Style.StyleClassPrefix, StringComparison.Ordinal))
				{
					var mergedClassStyles = new List<Style>(Resources[value.Key] as List<Style>);
					mergedClassStyles.AddRange(value.Value as List<Style>);
					changedResources.Add(new KeyValuePair<string, object>(value.Key, mergedClassStyles));
				}
			}
			if (changedResources.Count != 0)
				OnResourcesChanged(changedResources);
		}

		internal void UnmockBounds() => _mockX = _mockY = _mockWidth = _mockHeight = -1;

		void PropagateBindingContextToStateTriggers()
		{
			var groups = (IList<VisualStateGroup>)GetValue(VisualStateManager.VisualStateGroupsProperty);

			if (groups.Count == 0)
				return;

			foreach (var group in groups)
				foreach (var state in group.States)
					foreach (var stateTrigger in state.StateTriggers)
						SetInheritedBindingContext(stateTrigger, BindingContext);
		}

		void OnFocused() => Focused?.Invoke(this, new FocusEventArgs(this, true));

		internal void ChangeVisualStateInternal() => ChangeVisualState();

		bool _isPointerOver;

		internal bool IsPointerOver
		{
			get { return _isPointerOver; }
		}

		private protected void SetPointerOver(bool value, bool callChangeVisualState = true)
		{
			if (_isPointerOver == value)
				return;

			_isPointerOver = value;
			if (callChangeVisualState)
				ChangeVisualState();
		}

		/// <summary>
		/// Changes the current visual state based on this elements current property values.
		/// </summary>
		protected internal virtual void ChangeVisualState()
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, VisualStateManager.CommonStates.Disabled);
			}
			else if (IsPointerOver)
			{
				VisualStateManager.GoToState(this, VisualStateManager.CommonStates.PointerOver);
			}
			else
			{
				VisualStateManager.GoToState(this, VisualStateManager.CommonStates.Normal);
			}

			if (IsEnabled)
			{
				// Focus needs to be handled independently; otherwise, if no actual Focus state is supplied
				// in the control's visual states, the state can end up stuck in PointerOver after the pointer
				// exits and the control still has focus.
				VisualStateManager.GoToState(this,
					IsFocused ? VisualStateManager.CommonStates.Focused : VisualStateManager.CommonStates.Unfocused);
			}
		}

		static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var self = bindable as IVisualController;
			var newVisual = (IVisual)newValue;

			if (newVisual.IsMatchParent())
				self.EffectiveVisual = Microsoft.Maui.Controls.VisualMarker.Default;
			else
				self.EffectiveVisual = (IVisual)newValue;

			(self as IPropertyPropagationController)?.PropagatePropertyChanged(VisualElement.VisualProperty.PropertyName);
		}

		static void FlowDirectionChanging(BindableObject bindable, object oldValue, object newValue)
		{
			var self = bindable as IFlowDirectionController;

			if (self.EffectiveFlowDirection.IsExplicit() && oldValue == newValue)
				return;

			var newFlowDirection = ((FlowDirection)newValue).ToEffectiveFlowDirection(isExplicit: true);

			if (self is VisualElement ve)
				ve.SetEffectiveFlowDirection(newFlowDirection, false);
			else
				self.EffectiveFlowDirection = newFlowDirection;
		}

		static void FlowDirectionChanged(BindableObject bindable, object oldValue, object newValue)
		{
			(bindable as IPropertyPropagationController)?.PropagatePropertyChanged(VisualElement.FlowDirectionProperty.PropertyName);
		}


		static void OnIsEnabledPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var element = (VisualElement)bindable;

			if (element == null)
				return;

			element.ChangeVisualState();
		}

		static void OnIsFocusedPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
		{
			var element = (VisualElement)bindable;

			if (element == null)
			{
				return;
			}

			var isFocused = (bool)newvalue;
			if (isFocused)
			{
				element.OnFocused();
			}
			else
			{
				element.OnUnfocus();
			}

			element.ChangeVisualState();
		}

		static void OnRequestChanged(BindableObject bindable, object oldvalue, object newvalue)
		{
			var constraint = LayoutConstraint.None;
			var element = (VisualElement)bindable;
			if (element.WidthRequest >= 0 && element.MinimumWidthRequest >= 0)
			{
				constraint |= LayoutConstraint.HorizontallyFixed;
			}
			if (element.HeightRequest >= 0 && element.MinimumHeightRequest >= 0)
			{
				constraint |= LayoutConstraint.VerticallyFixed;
			}

			element.SelfConstraint = constraint;

			if (element is IView fe)
			{
				fe.Handler?.UpdateValue(nameof(IView.Width));
				fe.Handler?.UpdateValue(nameof(IView.Height));
				fe.Handler?.UpdateValue(nameof(IView.MinimumHeight));
				fe.Handler?.UpdateValue(nameof(IView.MinimumWidth));
				fe.Handler?.UpdateValue(nameof(IView.MaximumHeight));
				fe.Handler?.UpdateValue(nameof(IView.MaximumWidth));
			}

			((VisualElement)bindable).InvalidateMeasureInternal(InvalidationTrigger.SizeRequestChanged);
		}

		void OnUnfocus() => Unfocused?.Invoke(this, new FocusEventArgs(this, false));

		bool IFlowDirectionController.ApplyEffectiveFlowDirectionToChildContainer => true;

		void IPropertyPropagationController.PropagatePropertyChanged(string propertyName)
		{
			PropertyPropagationExtensions.PropagatePropertyChanged(propertyName, this, ((IVisualTreeElement)this).GetVisualChildren());
		}

		void UpdateBoundsComponents(Rect bounds)
		{
			_frame = bounds;

			BatchBegin();

			X = bounds.X;
			Y = bounds.Y;
			Width = bounds.Width;
			Height = bounds.Height;

			SizeAllocated(Width, Height);
			SizeChanged?.Invoke(this, EventArgs.Empty);

			BatchCommit();
		}

		public class FocusRequestArgs : EventArgs
		{
			public bool Focus { get; set; }

			public bool Result { get; set; }
		}

		public class VisibilityConverter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
				=> sourceType == typeof(string);

			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
				=> true;

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				var strValue = value?.ToString()?.Trim();

				if (!string.IsNullOrEmpty(strValue))
				{
					if (strValue.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase))
						return true;
					if (strValue.Equals("visible", StringComparison.OrdinalIgnoreCase))
						return true;
					if (strValue.Equals(bool.FalseString, StringComparison.OrdinalIgnoreCase))
						return false;
					if (strValue.Equals("hidden", StringComparison.OrdinalIgnoreCase))
						return false;
					if (strValue.Equals("collapse", StringComparison.OrdinalIgnoreCase))
						return false;
				}
				throw new InvalidOperationException(string.Format("Cannot convert \"{0}\" into {1}.", strValue, typeof(bool)));
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if (value is not bool visibility)
					throw new NotSupportedException();
				return visibility.ToString();
			}
		}
	}
}
