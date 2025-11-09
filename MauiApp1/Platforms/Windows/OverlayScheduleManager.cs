#if WINDOWS
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using Anti_Bunda_Mole.Methods;
using Anti_Bunda_Mole.Models;
using SQLite;

namespace Anti_Bunda_Mole.Platforms.Windows
{
    public class OverlayScheduleManager
    {
        private Timer? _scheduleTimer;
        private bool _overlayActiveBySchedule = false;
        private bool _soundActive = false; // controla se o som já está rodando

        public void Start()
        {
            if (_scheduleTimer != null) return;

            _scheduleTimer = new Timer(_ =>
            {
                try
                {
                    ConfigManager.Instance.LoadIfNeeded();
                    var config = ConfigManager.Instance.Config;

                    var now = DateTime.Now;
                    int currentDay = (int)now.DayOfWeek; // 0 = Sunday, 6 = Saturday

                    bool withinSchedule = false;

                    // Checa se hoje é dia ativo e horário dentro do período configurado
                    if (config.Dias.ContainsKey(currentDay))
                    {
                        var dayConfig = config.Dias[currentDay];
                        if (dayConfig.Ativo)
                        {
                            withinSchedule = dayConfig.Periodos.Any(p =>
                            {
                                if (TimeSpan.TryParse(p.Inicio, out var start) &&
                                    TimeSpan.TryParse(p.Fim, out var end))
                                {
                                    return now.TimeOfDay >= start && now.TimeOfDay < end;
                                }
                                return false;
                            });
                        }
                    }

                    // Checa se existem tarefas pendentes no DB
                    bool hasPendingTasks = false;
                    if (withinSchedule)
                    {
                        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        var dbPath = Path.Combine(documentsPath, "AntiBundaMole", "tasks.db3");

                        if (File.Exists(dbPath))
                        {
                            using var db = new SQLiteConnection(dbPath);
                            hasPendingTasks = db.Table<TaskModel>().Any(t => !t.IsCompleted);
                        }
                    }

                    // Invoca na thread principal
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (withinSchedule && hasPendingTasks)
                        {
                            // Mostra overlay se houver tarefas pendentes
                            var builder = new OverlayCardBuilder();
                            TaskOverlayController.ShowTasks(builder.BuildCards);
                            _overlayActiveBySchedule = true;

                            // Inicia som apenas se ainda não estiver ativo
                            if (!_soundActive)
                            {
                                SoundScheduler.StartFromConfig();
                                _soundActive = true;
                            }
                        }
                        else
                        {
                            // Fecha overlay se estava ativo
                            CloseOverlayIfActive();

                            // Para som apenas se estava rodando
                            if (_soundActive)
                            {
                                SoundScheduler.Stop();
                                _soundActive = false;
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro no ScheduleChecker: {ex}");
                }
            }, null, 0, 1000); // checa a cada segundo
        }

        public void Stop()
        {
            _scheduleTimer?.Dispose();
            _scheduleTimer = null;

            CloseOverlayIfActive();

            if (_soundActive)
            {
                SoundScheduler.Stop();
                _soundActive = false;
            }
        }

        private void CloseOverlayIfActive()
        {
            if (!_overlayActiveBySchedule) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TaskOverlayController.CloseOverlay();
                _overlayActiveBySchedule = false;
            });
        }
    }
}
#endif
