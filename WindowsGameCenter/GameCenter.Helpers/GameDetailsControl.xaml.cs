using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using GameCenter.Helpers;
using System.Windows.Input;
using System;

namespace GameCenter.Helpers
{
    public sealed partial class GameDetailsControl : UserControl
    {
        public static readonly DependencyProperty GameProperty =
            DependencyProperty.Register("Game", typeof(Game), typeof(GameDetailsControl), new PropertyMetadata(null, OnGameChanged));

        public Game Game
        {
            get { return (Game)GetValue(GameProperty); }
            set { SetValue(GameProperty, value); }
        }

        public GameDetailsControl()
        {
            this.InitializeComponent();
        }

        private static void OnGameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as GameDetailsControl;
            control.UpdateGameDetails();
        }

        private void UpdateGameDetails()
        {
            if (Game != null)
            {
                GameImage.Source = Game.ImageSource;
                GameTitle.Text = Game.Title;
                GameLauncher.Text = Game.Launcher;
                GameDescription.Text = Game.Description;

                // Update screenshots
                ScreenshotsPanel.Children.Clear();
                foreach (var screenshot in Game.ScreenshotSources)
                {
                    var image = new Image
                    {
                        Source = screenshot,
                        Width = 200,
                        Height = 113,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
                    };
                    ScreenshotsPanel.Children.Add(image);
                }

                // Update DLC list
                DLCListView.ItemsSource = Game.AvailableDLC;
            }
        }

        private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ExpandStoryboard.Begin();
        }

        private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            CollapseStoryboard.Begin();
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (Game != null)
            {
                await Windows.System.Launcher.LaunchUriAsync(new System.Uri(Game.LaunchUri));
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = this.Parent as FrameworkElement;
            if (parent != null)
            {
                var deleteCommand = parent.DataContext as ICommand;
                if (deleteCommand != null && deleteCommand.CanExecute(Game))
                {
                    deleteCommand.Execute(Game);
                }
            }
        }

        private void PurchaseDLC_Click(object sender, RoutedEventArgs e)
        {
            var dlc = (sender as FrameworkElement)?.DataContext as DLC;
            if (dlc != null)
            {
                // Implement DLC purchase logic
            }
        }
    }
}