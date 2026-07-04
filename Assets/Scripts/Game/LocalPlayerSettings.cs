// Guarda a escolha de nickname/cor feita na tela de perfil (b�nus) antes de entrar
// na sala. N�o � networked - � s� o dado local que esse cliente vai mandar via RPC
// quando pedir um personagem (ver PlayerSession.RPC_RequestCharacter).
public static class LocalPlayerSettings
{
    public static string Nickname = "Jogador";
    public static int ColorIndex = 0;
}
