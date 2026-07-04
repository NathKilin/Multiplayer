using Fusion;
using UnityEngine;

// Struct de input compartilhado entre todos os personagens.
// O Fusion serializa isso e entrega pra quem estiver simulando o objeto
// (ver FusionLobbyManager.OnInput e PlayerCharacter.FixedUpdateNetwork).
public struct GameplayInput : INetworkInput
{
    public Vector2 Move;
    public bool Fire;
}
