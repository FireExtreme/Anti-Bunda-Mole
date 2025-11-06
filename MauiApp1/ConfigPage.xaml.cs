using Anti_Bunda_Mole.Classes;
using Anti_Bunda_Mole.Methods;

namespace Anti_Bunda_Mole;

public partial class ConfigPage : ContentPage
{
    private Animations _an;
    private AuxiliarFuncs _aux_f;

    // Guarda os TimePickers de cada período
    private Dictionary<string, List<(TimePicker inicio, TimePicker fim)>> periodosPorDia;

    // RadioButtons personalizados
    private List<Frame> posicoesFrames;
    private Frame frameSelecionado;

    public ConfigPage()
    {
        InitializeComponent();
        _an = new Animations();
        _aux_f = new AuxiliarFuncs();
        _an.AddHoverEffect(btn_back);
        _an.AddHoverEffect(btn_save);

        periodosPorDia = new Dictionary<string, List<(TimePicker, TimePicker)>>();

        CriarFormularioDinamico();
        InicializarRadioButtons();

        _ = CarregarConfiguracoesAsync();
    }

    #region Formulário Dinâmico
    private void CriarFormularioDinamico()
    {
        var diasSemana = new string[] { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };
        var container = new VerticalStackLayout { Spacing = 15, Padding = 10 };

        foreach (var dia in diasSemana)
        {
            var diaFrame = new Frame
            {
                CornerRadius = 15,
                BackgroundColor = Color.FromArgb("#33000000"),
                Padding = 10
            };

            var diaStack = new VerticalStackLayout { Spacing = 5 };

            // Label do dia
            var diaLabel = new Label
            {
                Text = dia,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            };
            diaStack.Children.Add(diaLabel);

            // Checkbox para ativar o dia
            var chkDia = new CheckBox { Color = Colors.Gold };
            var horizontalCheck = new HorizontalStackLayout { Spacing = 5 };
            horizontalCheck.Children.Add(chkDia);
            horizontalCheck.Children.Add(new Label { Text = "Ativar", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center });
            diaStack.Children.Add(horizontalCheck);

            // Períodos
            var periodos = new List<(TimePicker, TimePicker)>();

            for (int i = 0; i < 2; i++)
            {
                var periodoStack = new HorizontalStackLayout { Spacing = 10 };

                var tpInicio = new TimePicker { Format = "HH:mm", Time = new TimeSpan(8, 0, 0) };
                var tpFim = new TimePicker { Format = "HH:mm", Time = new TimeSpan(12, 0, 0) };

                periodoStack.Children.Add(new Label { Text = "Início:", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center });
                periodoStack.Children.Add(tpInicio);
                periodoStack.Children.Add(new Label { Text = "Fim:", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center });
                periodoStack.Children.Add(tpFim);

                periodos.Add((tpInicio, tpFim));
                diaStack.Children.Add(periodoStack);
            }

            periodosPorDia[dia] = periodos;
            diaFrame.Content = diaStack;
            container.Children.Add(diaFrame);
        }

        stack_days.Content = container;
    }
    #endregion

    #region RadioButtons personalizados
    private void InicializarRadioButtons()
    {
        posicoesFrames = new List<Frame>
        {
            person_cb_upper_l,
            person_cb_upper_r,
            person_cb_bottom_l,
            person_cb_bottom_r
        };

        foreach (var frame in posicoesFrames)
        {
            // Hover
            _an.AddHoverEffect(frame);

            // Clique
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => SelecionarPosicao(frame);
            frame.GestureRecognizers.Add(tapGesture);
        }
    }

    private void SelecionarPosicao(Frame frame)
    {
        // Limpa seleção anterior
        if (frameSelecionado != null)
        {
            frameSelecionado.BackgroundColor = Colors.Transparent;
            frameSelecionado.BorderColor = Colors.Transparent;
            frameSelecionado.HasShadow = false;
        }

        // Seleciona novo
        frameSelecionado = frame;
        frameSelecionado.BackgroundColor = Color.FromArgb("#33FFE000");
        frameSelecionado.BorderColor = Colors.Gold;
        frameSelecionado.HasShadow = true;
    }

    private string ObterPosicaoSelecionada()
    {
        return frameSelecionado?.StyleId ?? "";
    }
    #endregion

    #region Helpers
    private CheckBox GetCheckBoxDoDia(string dia)
    {
        var containerStack = stack_days.Content as VerticalStackLayout;
        var diaFrame = containerStack.Children[Array.IndexOf(periodosPorDia.Keys.ToArray(), dia)] as Frame;
        var diaStack = diaFrame.Content as VerticalStackLayout;
        var chkDia = ((HorizontalStackLayout)diaStack.Children[1]).Children[0] as CheckBox;
        return chkDia;
    }
    #endregion

    #region Eventos Botões
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await _an.AnimateButton((ImageButton)sender);
        await _aux_f.ConfirmarAsync(this, "Confirme", "Tem certeza que quer sair sem salvar ?");
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        await _an.AnimateButton((ImageButton)sender);

        var config = ConfigManager.Instance.Config;

        config.IntervaloAviso = int.TryParse(entry_intervalo.Text, out int intervalo) ? intervalo : 0;
        config.PosicaoTarefas = ObterPosicaoSelecionada();

        foreach (var dia in periodosPorDia.Keys)
        {
            var periodos = periodosPorDia[dia];
            var periodoList = periodos.Select(p => new Periodo
            {
                Inicio = p.inicio.Time.ToString(@"hh\:mm"),
                Fim = p.fim.Time.ToString(@"hh\:mm")
            }).ToList();

            var chkDia = GetCheckBoxDoDia(dia);

            config.Dias[dia] = new DiaConfig
            {
                Ativo = chkDia.IsChecked,
                Periodos = periodoList
            };
        }

        await ConfigManager.Instance.SaveAsync();
        await DisplayAlert("Sucesso", "Configurações Salvas", "OK");
        await Navigation.PopAsync();
    }
    #endregion

    #region Carregar Configurações
    private async Task CarregarConfiguracoesAsync()
    {
        await ConfigManager.Instance.LoadAsync();
        var config = ConfigManager.Instance.Config;

        entry_intervalo.Text = config.IntervaloAviso.ToString();

        if (!string.IsNullOrEmpty(config.PosicaoTarefas))
        {
            var frame = posicoesFrames.FirstOrDefault(f => f.StyleId == config.PosicaoTarefas);
            if (frame != null) SelecionarPosicao(frame);
        }

        foreach (var dia in periodosPorDia.Keys)
        {
            if (!config.Dias.ContainsKey(dia)) continue;

            var diaConfig = config.Dias[dia];
            GetCheckBoxDoDia(dia).IsChecked = diaConfig.Ativo;

            var periodos = periodosPorDia[dia];
            for (int i = 0; i < periodos.Count && i < diaConfig.Periodos.Count; i++)
            {
                periodos[i].inicio.Time = TimeSpan.Parse(diaConfig.Periodos[i].Inicio);
                periodos[i].fim.Time = TimeSpan.Parse(diaConfig.Periodos[i].Fim);
            }
        }
    }
    #endregion
}
