using Anti_Bunda_Mole.Methods;
using Anti_Bunda_Mole.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;
using Microsoft.Maui.Storage;

namespace Anti_Bunda_Mole;

public partial class ConfigPage : ContentPage
{
    private Animations _an;
    private AuxiliarFuncs _aux_f;

    private Dictionary<int, List<(TimePicker inicio, TimePicker fim)>> periodosPorDia;
    private List<Frame> posicoesFrames;
    private Frame frameSelecionado;

    public ConfigPage()
    {
        InitializeComponent();
        _an = new Animations();
        _aux_f = new AuxiliarFuncs();
        _an.AddHoverEffect(btn_back);
        _an.AddHoverEffect(btn_save);

        periodosPorDia = new Dictionary<int, List<(TimePicker, TimePicker)>>();

        CriarFormularioDinamico();
        InicializarRadioButtons();

        _ = CarregarConfiguracoesAsync();
    }

    #region Formulário Dinâmico
    private void CriarFormularioDinamico()
    {
        var diasSemana = new (int DiaNumero, string Nome)[]
        {
            (0, "Dom"), (1, "Seg"), (2, "Ter"),
            (3, "Qua"), (4, "Qui"), (5, "Sex"), (6, "Sáb")
        };

        var container = new VerticalStackLayout { Spacing = 15, Padding = 10 };

        foreach (var (diaNumero, nome) in diasSemana)
        {
            var diaFrame = new Frame
            {
                CornerRadius = 15,
                BackgroundColor = Color.FromArgb("#33000000"),
                Padding = 10
            };

            var diaStack = new VerticalStackLayout { Spacing = 5 };
            diaStack.Children.Add(new Label { Text = nome, TextColor = Colors.White, FontAttributes = FontAttributes.Bold });

            var chkDia = new CheckBox { Color = Colors.Gold };
            var horizontalCheck = new HorizontalStackLayout { Spacing = 5 };
            horizontalCheck.Children.Add(chkDia);
            horizontalCheck.Children.Add(new Label { Text = "Ativar", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center });
            diaStack.Children.Add(horizontalCheck);

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

            periodosPorDia[diaNumero] = periodos;
            diaFrame.Content = diaStack;
            container.Children.Add(diaFrame);
        }

        stack_days.Content = container;
    }
    #endregion

    #region RadioButtons personalizados
    private void InicializarRadioButtons()
    {
        posicoesFrames = new List<Frame> { person_cb_upper_l, person_cb_upper_r, person_cb_bottom_l, person_cb_bottom_r };

        foreach (var frame in posicoesFrames)
        {
            _an.AddHoverEffect(frame);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => SelecionarPosicao(frame);
            frame.GestureRecognizers.Add(tapGesture);
        }
    }

    private void SelecionarPosicao(Frame frame)
    {
        if (frameSelecionado != null)
        {
            frameSelecionado.BackgroundColor = Colors.Transparent;
            frameSelecionado.BorderColor = Colors.Transparent;
            frameSelecionado.HasShadow = false;
        }

        frameSelecionado = frame;
        frameSelecionado.BackgroundColor = Color.FromArgb("#33FFE000");
        frameSelecionado.BorderColor = Colors.Gold;
        frameSelecionado.HasShadow = true;
    }

    private string ObterPosicaoSelecionada() => frameSelecionado?.StyleId ?? "";
    #endregion

    #region Helpers
    private CheckBox GetCheckBoxDoDia(int diaNumero)
    {
        var containerStack = stack_days.Content as VerticalStackLayout;
        var diaFrame = containerStack.Children[diaNumero] as Frame;
        var diaStack = diaFrame.Content as VerticalStackLayout;
        return ((HorizontalStackLayout)diaStack.Children[1]).Children[0] as CheckBox;
    }
    #endregion

    #region Botões
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

        foreach (var diaNumero in periodosPorDia.Keys)
        {
            var periodos = periodosPorDia[diaNumero];
            var periodoList = periodos.Select(p => new Periodo
            {
                Inicio = p.inicio.Time.ToString(@"hh\:mm"),
                Fim = p.fim.Time.ToString(@"hh\:mm")
            }).ToList();

            var chkDia = GetCheckBoxDoDia(diaNumero);

            config.Dias[diaNumero] = new DiaConfig { Ativo = chkDia.IsChecked, Periodos = periodoList };
        }

        await ConfigManager.Instance.SaveAsync();
        await DisplayAlert("Sucesso", "Configurações Salvas", "OK");
        await Navigation.PopAsync();
    }
    #endregion

    #region Carregar Config
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

        foreach (var diaNumero in periodosPorDia.Keys)
        {
            if (!config.Dias.ContainsKey(diaNumero)) continue;

            var diaConfig = config.Dias[diaNumero];
            GetCheckBoxDoDia(diaNumero).IsChecked = diaConfig.Ativo;

            var periodos = periodosPorDia[diaNumero];
            for (int i = 0; i < periodos.Count && i < diaConfig.Periodos.Count; i++)
            {
                periodos[i].inicio.Time = TimeSpan.Parse(diaConfig.Periodos[i].Inicio);
                periodos[i].fim.Time = TimeSpan.Parse(diaConfig.Periodos[i].Fim);
            }
        }
    }
    #endregion

    #region Ringtone
    private async void OnSelectRingtoneClicked(object sender, EventArgs e)
    {
        try
        {
            var audioFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".wav", ".mp3", ".m4a" } },
                { DevicePlatform.Android, new[] { "audio/*" } },
                { DevicePlatform.iOS, new[] { "public.audio" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Escolha um arquivo de som",
                FileTypes = audioFileType
            });

            if (result == null) return;

            var targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AntiBundaMole");
            Directory.CreateDirectory(targetFolder);

            var fileName = Path.GetFileName(result.FullPath);
            var targetPath = Path.Combine(targetFolder, fileName);

            using var stream = await result.OpenReadAsync();
            using var fileStream = File.Create(targetPath);
            await stream.CopyToAsync(fileStream);

            if (!targetPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var wavPath = Path.ChangeExtension(targetPath, ".wav");
                await ConvertToWavAsync(targetPath, wavPath);
                File.Delete(targetPath);
                targetPath = wavPath;
            }

            ConfigManager.Instance.Config.CaminhoRingtone = targetPath;
            lbl_ringtone.Text = $"Arquivo atual: {Path.GetFileName(targetPath)}";

            await ConfigManager.Instance.SaveAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao selecionar o som: {ex.Message}", "OK");
        }
    }

    public static async Task ConvertToWavAsync(string inputPath, string outputPath)
    {
        await Task.Run(() =>
        {
            using var reader = new AudioFileReader(inputPath);
            WaveFileWriter.CreateWaveFile(outputPath, reader);
        });
    }
    #endregion
}
