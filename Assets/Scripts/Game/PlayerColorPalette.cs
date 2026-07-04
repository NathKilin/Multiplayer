using UnityEngine;

// Paleta fixa de cores pro b�nus (nickname + cor). Usada tanto pela UI de escolha
// de cor (bot�es) quanto pelo PlayerCharacter pra pintar o personagem.
// S� guardamos o �NDICE pela rede (int), nunca a Color em si - assim n�o dependemos
// de sincroniza��o de Color pela rede e garantimos que todo mundo enxerga a mesma cor.
public static class PlayerColorPalette
{
    public static readonly Color[] Colors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta,
    };

    public static Color GetColor(int index)
    {
        if (index < 0 || index >= Colors.Length) return Color.white;
        return Colors[index];
    }
}
