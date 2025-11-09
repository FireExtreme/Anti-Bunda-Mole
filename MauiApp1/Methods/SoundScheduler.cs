using System;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using Anti_Bunda_Mole.Models;
using Anti_Bunda_Mole.Methods;
using NAudio.Wave;

namespace Anti_Bunda_Mole.Methods;

public static class SoundScheduler
{
    private static System.Timers.Timer? _timer;
    private static IWavePlayer? _waveOut;
    private static AudioFileReader? _audioFile;

    /// <summary>
    /// Inicia ou reinicia o toque automático de acordo com o IntervaloAviso das configurações.
    /// </summary>
    public static void StartFromConfig()
    {
        var config = ConfigManager.Instance.Config;
        int intervaloMinutos = config.IntervaloAviso;

        if (intervaloMinutos <= 0)
            return; // intervalo inválido

        Start(intervaloMinutos);
    }

    /// <summary>
    /// Inicia o timer com o intervalo em minutos.
    /// </summary>
    public static void Start(int intervaloMinutos)
    {
        Stop(); // Para qualquer execução anterior

        double intervaloMs = intervaloMinutos * 60 * 1000;

        _timer = new System.Timers.Timer(intervaloMs);
        _timer.Elapsed += async (s, e) => await TocarRingtoneAsync();
        _timer.AutoReset = true;
        _timer.Start();

        // Toca imediatamente
        _ = TocarRingtoneAsync();
    }

    /// <summary>
    /// Para o toque e libera recursos.
    /// </summary>
    public static void Stop()
    {
        try
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;

            _audioFile?.Dispose();
            _audioFile = null;
        }
        catch { }
    }

    /// <summary>
    /// Toca o ringtone configurado.
    /// </summary>
    public static async Task TocarRingtoneAsync()
    {
        try
        {
            var path = ConfigManager.Instance.Config.CaminhoRingtone;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            // Para e libera qualquer reprodução anterior
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFile?.Dispose();

            // Cria novo leitor e player
            _audioFile = new AudioFileReader(path);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioFile);
            _waveOut.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao tocar ringtone: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}
