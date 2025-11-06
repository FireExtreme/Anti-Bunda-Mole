using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace Anti_Bunda_Mole.Methods
{
    public class Animations
    {
        // Método genérico para Button ou ImageButton
        public async Task AnimateButton(View button)
        {
            if (button == null) return;

            await button.ScaleTo(0.85, 80, Easing.CubicIn);
            await button.ScaleTo(1, 80, Easing.CubicOut);
        }

        public void AddHoverEffect(View button)
        {
            if (button == null) return;

#if WINDOWS || MACCATALYST
            var hoverGesture = new PointerGestureRecognizer();
            hoverGesture.PointerEntered += async (s, e) =>
            {
                button.Opacity = 0.7;
                await button.ScaleTo(1.1, 100, Easing.CubicOut);
            };
            hoverGesture.PointerExited += async (s, e) =>
            {
                button.Opacity = 1;
                await button.ScaleTo(1.0, 100, Easing.CubicIn);
            };
            button.GestureRecognizers.Add(hoverGesture);
#endif
        }
    }
}
