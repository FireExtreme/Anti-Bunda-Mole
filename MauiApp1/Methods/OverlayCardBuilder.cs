#if WINDOWS
using Microsoft.Maui.Controls;
using SQLite;
using System;
using System.IO;
using System.Linq;
using Anti_Bunda_Mole.Methods;

namespace Anti_Bunda_Mole.Methods
{
    public class OverlayCardBuilder
    {
        public ScrollView BuildCards()
        {
            var panel = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Spacing = 15,
                Padding = 20
            };

            try
            {
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "AntiBundaMole",
                    "tasks.db3"
                );

                var db = new SQLiteConnection(dbPath);
                db.CreateTable<TaskModel>();

                var pendingTasks = db.Table<TaskModel>().Where(t => !t.IsCompleted).ToList();

                if (!pendingTasks.Any())
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

            openMainButton.Clicked += (s, e) =>
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                System.Diagnostics.Process.Start(exePath);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            };

            panel.Children.Add(openMainButton);

            return new ScrollView { Content = panel };
        }
    }
}
#endif
