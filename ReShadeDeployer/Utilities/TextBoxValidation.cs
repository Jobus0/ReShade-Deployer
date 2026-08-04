using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReShadeDeployer;

public static class TextBoxValidation
{
    public static readonly DependencyProperty HighlightEmptyProperty =
        DependencyProperty.RegisterAttached(
            "HighlightEmpty",
            typeof(bool),
            typeof(TextBoxValidation),
            new PropertyMetadata(false, OnHighlightEmptyChanged));

    public static readonly DependencyProperty CustomConditionProperty =
        DependencyProperty.RegisterAttached(
            "CustomCondition",
            typeof(Func<bool>),
            typeof(TextBoxValidation),
            new PropertyMetadata(null, OnCustomConditionChanged));

    public static void SetHighlightEmpty(UIElement element, bool value) => element.SetValue(HighlightEmptyProperty, value);
    public static bool GetHighlightEmpty(UIElement element) => (bool)element.GetValue(HighlightEmptyProperty);

    public static void SetCustomCondition(UIElement element, Func<bool>? value) => element.SetValue(CustomConditionProperty, value);
    public static Func<bool>? GetCustomCondition(UIElement element) => (Func<bool>?)element.GetValue(CustomConditionProperty);

    private static void OnCustomConditionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox tb)
        {
            UpdateBorders(tb);
        }
    }

    private static void OnHighlightEmptyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox tb)
        {
            tb.TextChanged -= OnStateChanged;
            tb.Loaded -= OnStateChanged;
            tb.IsEnabledChanged -= OnStateChanged;

            if ((bool)e.NewValue)
            {
                tb.TextChanged += OnStateChanged;
                tb.Loaded += OnStateChanged;
                tb.IsEnabledChanged += OnStateChanged;

                if (tb.IsLoaded)
                {
                    UpdateBorders(tb);
                }
            }
        }
    }

    private static void OnStateChanged(object sender, RoutedEventArgs e) => UpdateBorders((TextBox)sender);
    private static void OnStateChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateBorders((TextBox)sender);

    public static void UpdateBorders(TextBox tb)
    {
        if (!GetHighlightEmpty(tb)) return;

        var accentBorder = FindVisualChild<Border>(tb, "AccentBorder");
        var contentBorder = FindVisualChild<Border>(tb, "ContentBorder");

        if (accentBorder != null && contentBorder != null)
        {
            bool isEmpty = string.IsNullOrEmpty(tb.Text);
            
            // Check custom delegate if assigned (returns true when valid)
            var customCondition = GetCustomCondition(tb);
            bool failsCustomCondition = customCondition != null && !customCondition();

            // Needs to be enabled AND (empty OR failing custom check)
            bool shouldHighlight = tb.IsEnabled && (isEmpty || failsCustomCondition);

            if (shouldHighlight)
            {
                accentBorder.BorderBrush = Brushes.Red;
                contentBorder.BorderBrush = Brushes.Red;
            }
            else
            {
                accentBorder.ClearValue(Border.BorderBrushProperty);
                contentBorder.ClearValue(Border.BorderBrushProperty);
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name) return element;
            var result = FindVisualChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }
}