using Fusion;
using UnityEngine;

// Objeto "proxy" de cada jogador dentro da GameScene.
//
// Por que isso existe: o projeto usa GameMode.Shared (definido no FusionLobbyManager,
// Tarefa 1). Em Shared Mode n�o existe um �nico "servidor" com StateAuthority sobre a
// sala inteira - cada objeto tem sua pr�pria StateAuthority, normalmente o peer que o
// spawnou. Pra sele��o de personagem funcionar sem condi��o de corrida (dois jogadores
// pegando o mesmo slot ao mesmo tempo), a decis�o final PRECISA acontecer numa �nica
// m�quina. A solu��o: o Master Client (Runner.IsSharedModeMasterClient) spawna um
// PlayerSession pra cada jogador, com StateAuthority = Master mas InputAuthority = o
// pr�prio jogador dono. Assim, RPC_RequestCharacter (RpcSources.InputAuthority ->
// RpcTargets.StateAuthority) sempre executa na m�quina do Master, que � quem realmente
// spawna o personagem - exatamente como pedido no enunciado, s� que adaptado pro
// Shared Mode em vez de um Host/Server dedicado.
public class PlayerSession : NetworkBehaviour
{
    // Refer�ncia pro PlayerSession do jogador LOCAL, pra UI (CharacterSelectUI) n�o precisar
    // ficar procurando com FindObjectsOfType toda vez que o jogador clica num personagem.
    public static PlayerSession Local { get; private set; }

    public override void Spawned()
    {
        // Cada jogador s� precisa disso pra existir e guardar sua InputAuthority; n�o tem
        // apar�ncia nem l�gica pr�pria.
        gameObject.name = $"PlayerSession_{Object.InputAuthority.PlayerId}";

        if (Object.HasInputAuthority)
            Local = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this) Local = null;
    }

    // S� quem tem InputAuthority sobre ESSE objeto (o pr�prio dono) pode chamar,
    // e s� executa na m�quina que tem StateAuthority sobre ele (o Master Client).
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacter(int slotIndex, NetworkString<_16> nickname, int colorIndex)
    {
        // Object.InputAuthority = o jogador dono desse PlayerSession. N�o confiamos em
        // nenhum PlayerRef mandado pelo cliente - pegamos direto da rede, ent�o n�o d�
        // pra um jogador pedir personagem "em nome" de outro.
        CharacterSelectionManager.Instance?.TryClaimSlot(slotIndex, Object.InputAuthority, nickname, colorIndex);
    }
}
