using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using Microsoft.Data.Sqlite;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CheeerUpJokes
{
    public partial class MainWindow : Window
    {
        private string apiUrl = "https://official-joke-api.appspot.com/random_joke";
        private Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection("Data Source=jokes.db");
            connection.Open();
            var tableCmd = connection.CreateCommand();
            tableCmd.CommandText =
                @"CREATE TABLE IF NOT EXISTS Jokes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Setup TEXT,
                    Punchline TEXT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                )";
            tableCmd.ExecuteNonQuery();
        }

        private async void GetJokeButton_Click(object sender, RoutedEventArgs e)
        {
            using HttpClient client = new HttpClient();
            string response = await client.GetStringAsync(apiUrl);

            var doc = JsonDocument.Parse(response);
            string setup = doc.RootElement.GetProperty("setup").GetString();
            string punchline = doc.RootElement.GetProperty("punchline").GetString();

            SetupTextBlock.Text = setup;
            PunchlineTextBlock.Text = punchline;
            EmojiTextBlock.Text = "😄";

            using var connection = new SqliteConnection("Data Source=jokes.db");
            connection.Open();
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Jokes (Setup, Punchline) VALUES (@setup, @punchline)";
            insertCmd.Parameters.AddWithValue("@setup", setup);
            insertCmd.Parameters.AddWithValue("@punchline", punchline);
            insertCmd.ExecuteNonQuery();

            AnimateText(SetupTextBlock);
            AnimateText(PunchlineTextBlock);
            AnimateEmoji(EmojiTextBlock);
            ChangeJokeBorderColor();

            AddJokeToHistory(setup, punchline);
        }

        private void AnimateText(System.Windows.Controls.TextBlock textBlock)
        {
            var brush = new SolidColorBrush(Colors.Black);
            textBlock.Foreground = brush;
            ColorAnimation colorAnim = new ColorAnimation
            {
                From = Colors.Black,
                To = Colors.OrangeRed,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                AutoReverse = true
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
        }

        private void AnimateEmoji(System.Windows.Controls.TextBlock emoji)
        {
            DoubleAnimation jumpAnim = new DoubleAnimation
            {
                From = 0,
                To = -20,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            TranslateTransform trans = new TranslateTransform();
            emoji.RenderTransform = trans;
            trans.BeginAnimation(TranslateTransform.YProperty, jumpAnim);
        }

        private void ChangeJokeBorderColor()
        {
            byte r = (byte)rnd.Next(200, 256);
            byte g = (byte)rnd.Next(200, 256);
            byte b = (byte)rnd.Next(200, 256);
            JokeBorder.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        private void AddJokeToHistory(string setup, string punchline)
        {
            var jokeBorder = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 240, 179)),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var stack = new System.Windows.Controls.StackPanel();

            var setupText = new System.Windows.Controls.TextBlock
            {
                Text = setup,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            var punchlineText = new System.Windows.Controls.TextBlock
            {
                Text = "😄 " + punchline,
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            stack.Children.Add(setupText);
            stack.Children.Add(punchlineText);
            jokeBorder.Child = stack;

            HistoryStackPanel.Children.Insert(0, jokeBorder);
            HistoryScrollViewer.ScrollToTop();

            DoubleAnimation fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
            jokeBorder.BeginAnimation(System.Windows.Controls.Border.OpacityProperty, fadeAnim);
        }
    }
}
