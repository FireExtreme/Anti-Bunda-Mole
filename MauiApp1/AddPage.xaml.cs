using Anti_Bunda_Mole.Methods;
namespace Anti_Bunda_Mole;

public partial class AddPage : ContentPage
{
    private Animations _an;
    private AuxiliarFuncs _aux_f;
    private TaskManager _taskManager;

    public AddPage()
    {
        InitializeComponent();
        _an = new Animations();
        _aux_f = new AuxiliarFuncs();
        _taskManager = new TaskManager();

        _an.AddHoverEffect(btn_back);
        _an.AddHoverEffect(btn_save);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await _an.AnimateButton((ImageButton)sender);
        bool confirm = await _aux_f.ConfirmarAsync(this, "Confirme", "Tem certeza que quer sair sem salvar ?");
        if (confirm)
            await Navigation.PopAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        await _an.AnimateButton((ImageButton)sender);

        var task = new TaskItem
        {
            Title = TaskTitleEntry.Text,
            Description = TaskDescriptionEditor.Text,
            Type = rbTarefaRecorrente.IsChecked ? "Recorrente" : "Unica"
        };

        await _taskManager.SaveTaskAsync(task);
        await DisplayAlert("Sucesso", "Tarefa salva com sucesso!", "OK");
        await Navigation.PopAsync();
    }
}
