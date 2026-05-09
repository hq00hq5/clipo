using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Clipo
{
    public static class HighlightBehavior
    {
        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.RegisterAttached(
                "HighlightText",
                typeof(string),
                typeof(HighlightBehavior),
                new PropertyMetadata(string.Empty, OnHighlightTextChanged));

        public static string GetHighlightText(DependencyObject obj) => (string)obj.GetValue(HighlightTextProperty);
        public static void SetHighlightText(DependencyObject obj, string value) => obj.SetValue(HighlightTextProperty, value);

        public static readonly DependencyProperty FullTextProperty =
            DependencyProperty.RegisterAttached(
                "FullText",
                typeof(string),
                typeof(HighlightBehavior),
                new PropertyMetadata(string.Empty, OnHighlightTextChanged));

        public static string GetFullText(DependencyObject obj) => (string)obj.GetValue(FullTextProperty);
        public static void SetFullText(DependencyObject obj, string value) => obj.SetValue(FullTextProperty, value);

        private static void OnHighlightTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                var fullText = GetFullText(textBlock) ?? string.Empty;
                var highlightText = GetHighlightText(textBlock) ?? string.Empty;

                textBlock.Inlines.Clear();

                if (string.IsNullOrWhiteSpace(highlightText) || string.IsNullOrWhiteSpace(fullText))
                {
                    textBlock.Inlines.Add(new Run(fullText));
                    return;
                }

                int startIndex = 0;
                while (startIndex < fullText.Length)
                {
                    int index = fullText.IndexOf(highlightText, startIndex, System.StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                    {
                        textBlock.Inlines.Add(new Run(fullText.Substring(startIndex)));
                        break;
                    }

                    if (index > startIndex)
                    {
                        textBlock.Inlines.Add(new Run(fullText.Substring(startIndex, index - startIndex)));
                    }

                    textBlock.Inlines.Add(new Run(fullText.Substring(index, highlightText.Length))
                    {
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 255)) // Neon Blue
                    });

                    startIndex = index + highlightText.Length;
                }
            }
        }
    }
}
