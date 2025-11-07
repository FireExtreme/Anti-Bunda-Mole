using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anti_Bunda_Mole.Models;

public class Periodo
{
    public string Inicio { get; set; }  // HH:mm
    public string Fim { get; set; }     // HH:mm
}

public class DiaConfig
{
    public bool Ativo { get; set; }
    public List<Periodo> Periodos { get; set; }
}

public class Configuracoes
{
    public int IntervaloAviso { get; set; } // em minutos
    public Dictionary<string, DiaConfig> Dias { get; set; }
    public string PosicaoTarefas { get; set; } = "";
}
