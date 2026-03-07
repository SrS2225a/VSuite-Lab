using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Metadata;
using Avalonia.Styling;

namespace VSuiteLab.Resources
{
    [PseudoClasses(":inactive", ":active")]
    public class LoadingIndicator : TemplatedControl
    {
        public static readonly StyledProperty<bool> IsActiveProperty =
            AvaloniaProperty.Register<LoadingIndicator, bool>(nameof(IsActive), true);

        public static readonly StyledProperty<double> SpeedRatioProperty =
            AvaloniaProperty.Register<LoadingIndicator, double>(nameof(SpeedRatio), 1d);

        protected override Type StyleKeyOverride => typeof(LoadingIndicator);

        public bool IsActive
        {
            get => GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public double SpeedRatio
        {
            get => GetValue(SpeedRatioProperty);
            set => SetValue(SpeedRatioProperty, value);
        }
        
        public LoadingIndicator()
        {

            if (Application.Current?.Resources.TryGetResource("DefaultLoadingIndicator", null, out var resource) ?? false)
            {
                Theme = resource as ControlTheme;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            UpdateVisualStates();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsActiveProperty)
                UpdateVisualStates();
        }

        private void UpdateVisualStates()
        {
            PseudoClasses.Set(":active", IsActive);
            PseudoClasses.Set(":inactive", !IsActive);
        }
    }
}