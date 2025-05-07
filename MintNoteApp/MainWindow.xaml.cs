using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mint
{
    public partial class MainWindow : Window
    {
        private readonly string notesDirectory;

        public MainWindow(string username)
        {
            InitializeComponent();
            //user-specific Notes directory
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MintApp");
            notesDirectory = Path.Combine(appDataPath, username, "Notes");

            LoadNotesList_Main();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenNoteEditorButton_Click(object sender, RoutedEventArgs e)
        {
            // Pass notesDirectory to NoteEditor
            NoteEditorView noteEditorView = new NoteEditorView(notesDirectory); 
            noteEditorView.Show();
        }

        private void LoadNotesList_Main()
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(notesDirectory))
                {
                    Directory.CreateDirectory(notesDirectory);
                }

                // Clear previous buttons
                while (Sidebar_Main.Children.Count > 1)
                {
                    Sidebar_Main.Children.RemoveAt(1);
                }

                // Get text files (*.txt) instead of *.rtf
                string[] files = Directory.GetFiles(notesDirectory, "*.txt");
                if (files.Length == 0)
                {
                    NoNotesText_Main.Visibility = Visibility.Visible;
                }
                else
                {
                    NoNotesText_Main.Visibility = Visibility.Hidden;
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        Button noteButton = new Button
                        {
                            Content = fileName,
                            Margin = new Thickness(5),
                            Foreground = System.Windows.Media.Brushes.White,
                            Background = System.Windows.Media.Brushes.Transparent,
                            BorderBrush = System.Windows.Media.Brushes.Gray,
                            BorderThickness = new Thickness(1),
                            Tag = file
                        };
                        noteButton.Click += NoteButton_Click_Main;
                        Sidebar_Main.Children.Add(noteButton);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading notes: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NoteButton_Click_Main(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                // Open the selected note
                NoteEditorView noteEditorView = new NoteEditorView(notesDirectory); // Pass notesDirectory
                noteEditorView.OpenExistingNote(filePath);
                noteEditorView.Show();
            }
        }
    }
}
