using System;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Mint
{
    public partial class NoteEditorView : Window
    {
        private string currentFilePath = string.Empty;
        private readonly string notesDirectory;

        private System.Timers.Timer userTypingTimer;
        private bool isUserDeletingContent = false;

        public NoteEditorView(string notesDirectory)
        {
            InitializeComponent();
            this.notesDirectory = notesDirectory; // Use passed-in user-specific directory

            // Ensure the notes directory exists
            if (!Directory.Exists(notesDirectory))
            {
                Directory.CreateDirectory(notesDirectory);
            }

            LoadNotesList();
            mainTextArea.TextChanged += MainTextArea_TextChanged;

            userTypingTimer = new System.Timers.Timer(800);
            userTypingTimer.Elapsed += UserTypingTimer_Elapsed;
            userTypingTimer.AutoReset = false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (mainTextArea.Document.Blocks.Count > 0)
            {
                SaveDocument();
            }
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void SaveDocument()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs();
            }
            else
            {
                SaveFile(currentFilePath);
            }
        }

        private void SaveFileAs()
        {
            // Ensure the notes directory exists
            if (!Directory.Exists(notesDirectory))
            {
                Directory.CreateDirectory(notesDirectory);
            }

            string fileName = documentTitleTextBox.Text.Trim();
            if (string.IsNullOrEmpty(fileName))
                fileName = "Untitled";

            currentFilePath = Path.Combine(notesDirectory, fileName + ".txt");
            SaveFile(currentFilePath);
        }

        private void SaveFile(string filePath)
        {
            try
            {
                TextRange textRange = new TextRange(mainTextArea.Document.ContentStart, mainTextArea.Document.ContentEnd);
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    textRange.Save(fileStream, DataFormats.Rtf);
                }

                documentTitleTextBox.Text = Path.GetFileNameWithoutExtension(filePath);
                NoNotesText.Visibility = Visibility.Hidden;
                LoadNotesList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadNotesList()
        {
            try
            {
                // Ensure the notes directory exists
                if (!Directory.Exists(notesDirectory))
                {
                    Directory.CreateDirectory(notesDirectory);
                }

                while (Sidebar.Children.Count > 2)
                {
                    Sidebar.Children.RemoveAt(2);
                }

                string[] files = Directory.GetFiles(notesDirectory, "*.txt");
                if (files.Length == 0)
                {
                    NoNotesText.Visibility = Visibility.Visible;
                }
                else
                {
                    NoNotesText.Visibility = Visibility.Hidden;
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        Button noteButton = new Button
                        {
                            Content = fileName,
                            Style = (Style)FindResource("SoftEdgeButtonStyle"),
                            Margin = new Thickness(5),
                            Tag = file
                        };
                        noteButton.Click += NoteButton_Click;
                        Sidebar.Children.Add(noteButton);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading notes: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                OpenExistingNote(filePath);
            }
        }

        private void MainTextArea_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextRange textRange = new TextRange(mainTextArea.Document.ContentStart, mainTextArea.Document.ContentEnd);
            string textContent = textRange.Text.Trim();

            if (string.IsNullOrEmpty(textContent))
            {
                isUserDeletingContent = true;
                userTypingTimer.Stop();
                userTypingTimer.Start();
            }
            else
            {
                isUserDeletingContent = false;
                SaveDocument();
            }
        }

        private void UserTypingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isUserDeletingContent)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "You have deleted all content of this note. Do you want to delete this document permanently?",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (File.Exists(currentFilePath))
                        {
                            File.Delete(currentFilePath);
                        }

                        documentTitleTextBox.Text = "";
                        mainTextArea.Document.Blocks.Clear();
                        currentFilePath = string.Empty;
                        LoadNotesList();
                    }
                    else
                    {
                        // Undo deletion
                        mainTextArea.Undo();
                    }
                }
            });
        }

        // Button functionalities
        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            mainTextArea.Visibility = Visibility.Visible;

            // Reset to default background and text colors
            mainTextArea.Background = Brushes.Transparent;
            mainTextArea.Foreground = Brushes.White;
            mainTextArea.FontFamily = new FontFamily("Arial");
            mainTextArea.FontSize = 12;

            // Clear any syntax highlighting that was applied in code mode
            TextRange textRange = new TextRange(mainTextArea.Document.ContentStart, mainTextArea.Document.ContentEnd);
            textRange.ClearAllProperties();
        }


        private void CodeButton_Click(object sender, RoutedEventArgs e)
        {
            HighlightSyntax();

            // Set background for code mode
            mainTextArea.Background = Brushes.Black;
            mainTextArea.Foreground = Brushes.LightGray;
            mainTextArea.FontFamily = new FontFamily("Consolas");
            mainTextArea.FontSize = 14;
        }

        private void HighlightSyntax()
        {
            // Define keywords, strings, comments, and numbers with word boundaries
            string[] keywords = { @"\bint\b", @"\bstring\b", @"\bif\b", @"\belse\b", @"\bfor\b", @"\bwhile\b", @"\breturn\b", @"\bvoid\b", @"\bpublic\b", @"\bprivate\b", @"\bclass\b" };
            string[] strings = { "\"[^\"]*\"" }; // Regex for strings
            string[] comments = { "//[^\n]*", "/\\*.*?\\*/" }; // Regex for comments
            string[] numbers = { @"\b\d+(\.\d+)?\b" }; // Regex for numbers

            // Get the text range of the document
            TextRange textRange = new TextRange(mainTextArea.Document.ContentStart, mainTextArea.Document.ContentEnd);

            // Clear all previous formatting
            textRange.ClearAllProperties();

            // Call the helper function to highlight each pattern
            HighlightWithRegex(keywords, Brushes.Blue);
            HighlightWithRegex(strings, Brushes.LightGreen);
            HighlightWithRegex(comments, Brushes.Gray);
            HighlightWithRegex(numbers, Brushes.Cyan);

            // Helper function to apply color to matching regex patterns
            void HighlightWithRegex(string[] patterns, Brush color)
            {
                foreach (string pattern in patterns)
                {
                    TextPointer pointer = mainTextArea.Document.ContentStart;
                    while (pointer != null && pointer.CompareTo(mainTextArea.Document.ContentEnd) < 0)
                    {
                        if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                        {
                            string textRun = pointer.GetTextInRun(LogicalDirection.Forward);

                            // Use Regex to find matches
                            var matches = System.Text.RegularExpressions.Regex.Matches(textRun, pattern);
                            foreach (System.Text.RegularExpressions.Match match in matches)  // Access each Match object
                            {
                                // Match the text exactly between word boundaries
                                TextPointer start = pointer.GetPositionAtOffset(match.Index);
                                TextPointer end = start.GetPositionAtOffset(match.Length);
                                TextRange wordRange = new TextRange(start, end);
                                wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, color);
                            }
                        }
                        pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                    }
                }
            }
        }




        private void BoldButton_Click(object sender, RoutedEventArgs e)
{
    TextSelection selection = mainTextArea.Selection;
    if (selection != null)
    {
        object currentWeight = selection.GetPropertyValue(TextElement.FontWeightProperty);
        if (currentWeight != DependencyProperty.UnsetValue && currentWeight.Equals(FontWeights.Bold))
        {
            selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
        }
        else
        {
            selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
        }
    }
}


        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            string selectedSize = (comboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (int.TryParse(selectedSize, out int fontSize))
            {
                mainTextArea.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, (double)fontSize);
            }
        }

        private void AlignLeftButton_Click(object sender, RoutedEventArgs e)
        {
            mainTextArea.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Left);
        }

        private void AlignCenterButton_Click(object sender, RoutedEventArgs e)
        {
            mainTextArea.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Center);
        }

        private void AlignRightButton_Click(object sender, RoutedEventArgs e)
        {
            mainTextArea.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, TextAlignment.Right);
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                Image image = new Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(imagePath)),
                    Height = 100,
                    Width = 100
                };

                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(image);
                mainTextArea.Document.Blocks.Add(paragraph);
            }
        }

        //opening existing notes
        public void OpenExistingNote(string filePath)
        {
            try
            {
                mainTextArea.TextChanged -= MainTextArea_TextChanged;

                TextRange textRange = new TextRange(mainTextArea.Document.ContentStart, mainTextArea.Document.ContentEnd);
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    textRange.Load(fileStream, DataFormats.Rtf);
                }

                documentTitleTextBox.Text = Path.GetFileNameWithoutExtension(filePath);
                currentFilePath = filePath;

                mainTextArea.TextChanged += MainTextArea_TextChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening note: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
