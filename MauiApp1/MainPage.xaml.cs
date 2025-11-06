using Anti_Bunda_Mole.Methods;
using Microsoft.Maui.Controls.Shapes;
using System.Linq;

namespace Anti_Bunda_Mole
{
    public partial class MainPage : ContentPage
    {
        private Animations _an;
        private TaskManager _taskManager;
        private TaskItem _taskmd;
        private TaskEvents _tkev;

        public MainPage()
        {
            InitializeComponent();
            _an = new Animations();
            _taskManager = new TaskManager();
            _taskmd = new TaskItem();

            NavigationPage.SetHasNavigationBar(this, false);

            _an.AddHoverEffect(btn_add);
            _an.AddHoverEffect(btn_config);
            _an.AddHoverEffect(btn_delete);
            // Carrega tarefas na memória e atualiza a UI
            LoadTasksToUI();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadTasksToUI();
        }

        private async void LoadTasksToUI()
        {
            await _taskManager.LoadTasksIntoMemoryAsync();

            PendingTasksContainer.Children.Clear();

            var pendingLabel = new Label
            {
                Text = "Tarefas Pendentes",
                FontSize = 44,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromRgba(255, 224, 0, 0.99),
                Margin = new Thickness(0, 15, 0, 5)
            };
            PendingTasksContainer.Children.Add(pendingLabel);

            var pendingTasks = _taskManager.TasksInMemory
                                    .Where(t => !t.IsCompleted)
                                    .OrderByDescending(t => t.CreatedAt);

            foreach (var task in pendingTasks)
            {
                PendingTasksContainer.Children.Add(CreateTaskView(task, false));
            }

            var completedLabel = new Label
            {
                Text = "Tarefas Concluídas",
                FontSize = 44,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromRgba(0, 222, 100, 0.99),
                Margin = new Thickness(0, 10, 0, 5)
            };
            PendingTasksContainer.Children.Add(completedLabel);

            var completedTasks = _taskManager.TasksInMemory
                                    .Where(t => t.IsCompleted)
                                    .OrderByDescending(t => t.CreatedAt);

            foreach (var task in completedTasks)
            {
                PendingTasksContainer.Children.Add(CreateTaskView(task, true));
            }


        }

        private View CreateTaskView(TaskItem task, bool isCompleted)
        {
            // Título
            var titleLabel = new Label
            {
                Text = task.Title,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                VerticalOptions = LayoutOptions.Center,
                TextColor = isCompleted ? Color.FromRgba(0, 222, 100, 0.99)
                                        : Color.FromRgba(255, 224, 0, 0.99)
            };

            // Descrição
            var descriptionLabel = new Label
            {
                Text = task.Description,
                FontSize = 14,
                TextColor = isCompleted ? Color.FromRgba(0, 222, 100, 0.99)
                                        : Color.FromRgba(255, 224, 0, 0.7)
            };

            // Ícone de check (apenas se não estiver concluída)
            var checkButton = new ImageButton
            {
                Source = "check.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 32,
                HeightRequest = 32,
                HorizontalOptions = LayoutOptions.End,
                IsVisible = !isCompleted
            };

            // Evento de clique do botão
            checkButton.Clicked += async (s, e) =>
            {
                string input = await DisplayPromptAsync(
                    "Confirmação",
                    $"Digite o título da tarefa para confirmar a conclusão:\n\n{task.Title.ToUpperInvariant()}",
                    "Confirmar",
                    "Cancelar",
                    keyboard: Keyboard.Text
                );

                if (string.IsNullOrWhiteSpace(input))
                    return;

                if (input.ToUpperInvariant().Trim() == task.Title.ToUpperInvariant().Trim())
                {
                    task.IsCompleted = true;
                    await _taskManager.SaveTaskAsync(task);

                    checkButton.IsVisible = false; // Esconde o botão após confirmar
                    LoadTasksToUI(); // Atualiza a UI
                }
                else
                {
                    await DisplayAlert("Erro", "O título digitado não confere. Tente novamente.", "OK");
                }
            };

            // Layout horizontal para o título e o botão lado a lado
            var titleRow = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Auto }
        },
                ColumnSpacing = 5
            };
            titleRow.Add(titleLabel, 0, 0);
            titleRow.Add(checkButton, 1, 0);

            // Empilha o título+botão e a descrição
            var stack = new VerticalStackLayout
            {
                Padding = 10,
                Spacing = 5
            };
            stack.Add(titleRow);
            stack.Add(descriptionLabel);

            // Borda principal do card
            var border = new Border
            {
                StrokeThickness = 5,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                Stroke = isCompleted ? Color.FromRgba(0, 222, 100, 0.99)
                                     : Color.FromRgba(255, 224, 0, 0.99),
                Background = Color.FromRgba("#33000000"),
                Content = stack,
                ClassId = isCompleted ? "completed" : "pending",
                BindingContext = task
            };

            return border;
        }

        private async void OnCreateTaskClicked(object sender, EventArgs e)
        {
            await _an.AnimateButton((ImageButton)sender);
            await Navigation.PushAsync(new AddPage());
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await _an.AnimateButton((ImageButton)sender);
            await Navigation.PushAsync(new ConfigPage());
        }

        private async void OnDeleteCompletedClicked(object sender, EventArgs e)
        {
            await _an.AnimateButton((ImageButton)sender);

            bool confirm = await DisplayAlert("Confirmar", "Deseja remover todas as tarefas concluídas?", "Sim", "Não");
            if (!confirm)
                return;

            try
            {
                // Seleciona todas as tarefas concluídas do container
                var completedTasks = PendingTasksContainer.Children
                    .OfType<Border>()
                    .Where(f => f.ClassId == "completed")
                    .ToList();

                foreach (var border in completedTasks)
                {
                    if (border.BindingContext is TaskItem task)
                    {
                        await _taskManager.DeleteTaskAsync(task);
                    }
                }

                // Recarrega a lista
                LoadTasksToUI();

                await DisplayAlert("Sucesso", "Tarefas concluídas removidas.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Falha ao excluir tarefas: {ex.Message}", "OK");
            }
        }

        private async void OnTestOverlayClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OverlayTestPage());
        }
    }
}
