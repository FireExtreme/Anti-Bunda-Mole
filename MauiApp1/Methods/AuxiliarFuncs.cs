using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Anti_Bunda_Mole.Methods
{
   public class AuxiliarFuncs
    {
        public async Task<bool> ConfirmarAsync(Page page, string titulo, string mensagem)
        {
            bool resposta = await page.DisplayAlert(titulo, mensagem, "Sim", "Não");


            if (resposta)
            {
                await page.Navigation.PopAsync();
            }
            return resposta;
            
        }
    }
}
