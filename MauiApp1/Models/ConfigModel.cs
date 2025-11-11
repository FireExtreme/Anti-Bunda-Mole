namespace Anti_Bunda_Mole.Models;

public class Periodo
{
    public string Inicio { get; set; }  // HH:mm
    public string Fim { get; set; }     // HH:mm
}

public class DiaConfig
{
    public bool Ativo { get; set; }
    public List<Periodo> Periodos { get; set; } = new();
}

public class Configuracoes
{
    public int IntervaloAviso { get; set; } // em minutos
    public Dictionary<int, DiaConfig> Dias { get; set; } = new(); // chave agora é int
    public string PosicaoTarefas { get; set; } = "";

    public string CaminhoRingtone { get; set; } = "ringtone.wav"; // valor padrão
}
