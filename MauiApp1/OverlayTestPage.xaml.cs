using Anti_Bunda_Mole.Methods;
using SQLite;
using Microsoft.Maui.Controls;
using System.IO;
using System.Linq;

#if WINDOWS
using Anti_Bunda_Mole.Platforms.Windows;
#endif

namespace Anti_Bunda_Mole
{
    public partial class OverlayTestPage : ContentPage
    {
        public OverlayTestPage()
        {
            InitializeComponent();
        }

        private void OnShowOverlayClicked(object sender, EventArgs e)
        {
#if WINDOWS
            // Usa o TaskOverlayController para exibir o overlay conforme o JSON
            TaskOverlayController.ShowTasks(BuildOverlayCards);
#endif
        }

        private void OnCloseOverlayClicked(object sender, EventArgs e)
        {
#if WINDOWS
            TaskOverlayController.Close();
#endif
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

#if WINDOWS
        private ScrollView BuildOverlayCards()
        {
            var panel = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Spacing = 15,
                Padding = 20
            };

            try
            {
                // Caminho do banco local
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "AntiBundaMole",
                    "tasks.db3"
                );

                var db = new SQLiteConnection(dbPath);
                db.CreateTable<TaskItem>();

                // Seleciona tarefas pendentes
                var pendingTasks = db.Table<TaskItem>().Where(t => !t.IsCompleted).ToList();

                if (pendingTasks.Count == 0)
                {
                    panel.Children.Add(new Label
                    {
                        Text = "Nenhuma tarefa pendente.",
                        FontSize = 20,
                        TextColor = Colors.LightGray,
                        HorizontalOptions = LayoutOptions.Center
                    });
                }
                else
                {
                    foreach (var task in pendingTasks)
                    {
                        var card = new Frame
                        {
                            CornerRadius = 12,
                            BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f),
                            BorderColor = Colors.DimGray,
                            HasShadow = true,
                            Padding = 15,
                            Content = new VerticalStackLayout
                            {
                                Spacing = 5,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = task.Title,
                                        FontSize = 22,
                                        FontAttributes = FontAttributes.Bold,
                                        TextColor = Colors.White
                                    },
                                    new Label
                                    {
                                        Text = task.Description,
                                        FontSize = 16,
                                        TextColor = Colors.LightGray
                                    }
                                }
                            }
                        };

                        panel.Children.Add(card);
                    }
                }
            }
            catch (Exception ex)
            {
                panel.Children.Add(new Label
                {
                    Text = $"Erro ao carregar tarefas: {ex.Message}",
                    TextColor = Colors.Red
                });
            }

            // Botão final
            var openMainButton = new Button
            {
                Text = "Abrir Painel Principal",
                BackgroundColor = Colors.MediumPurple,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                HeightRequest = 45,
                Margin = new Thickness(0, 20, 0, 0)
            };

            openMainButton.Clicked += async (s, e) =>
            {
                try
                {
                    if (Application.Current.MainPage is not MainPage)
                        Application.Current.MainPage = new MainPage();

                    await Application.Current.MainPage.Navigation.PopToRootAsync(animated: false);
                }
                catch
                {
                    Application.Current.MainPage = new MainPage();
                }
            };

            panel.Children.Add(openMainButton);

            // Envolvendo em ScrollView para permitir rolagem
            return new ScrollView
            {
                Content = panel
            };
        }
#endif
    }
}
