using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI de escolha de personagem na GameScene: 10 bot�es, um por slot (0 a 9).
// N�o faz polling - s� atualiza quando CharacterSelectionManager avisa via
// OnChangedRender (Refresh) ou quando o pr�prio pedido � negado (OnRequestDenied).
public class CharacterSelectUI : MonoBehaviour
{
    public static CharacterSelectUI Instance { get; private set; }

    [SerializeField] private Button[] characterButtons = new Button[10]; // ordem = �ndice do slot (0-9)
    [SerializeField] private TextMeshProUGUI feedbackText;

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int slotIndex = i; // copia local pra n�o "vazar" a vari�vel de loop pro closure
            if (characterButtons[i] != null)
                characterButtons[i].onClick.AddListener(() => RequestCharacter(slotIndex));
        }
    }

    private void RequestCharacter(int slotIndex)
    {
        if (PlayerSession.Local == null)
        {
            Debug.LogWarning("PlayerSession local ainda n�o existe - aguarde a GameScene terminar de carregar.");
            return;
        }

        PlayerSession.Local.RPC_RequestCharacter(slotIndex, LocalPlayerSettings.Nickname, LocalPlayerSettings.ColorIndex);
    }

    // Chamado pelo CharacterSelectionManager (OnChangedRender) em TODOS os clientes sempre
    // que o array de slots muda - desabilita bot�es ocupados e reabilita os liberados.
    public void Refresh(NetworkArray<PlayerRef> slots)
    {
        for (int i = 0; i < characterButtons.Length && i < slots.Length; i++)
        {
            if (characterButtons[i] == null) continue;
            characterButtons[i].interactable = slots.Get(i) == PlayerRef.None;
        }
    }

    // RPC direcionado do CharacterSelectionManager: s� roda na m�quina de quem pediu o slot ocupado.
    public void OnRequestDenied(int slotIndex)
    {
        if (feedbackText != null)
            feedbackText.text = $"Personagem {slotIndex} j� foi escolhido por outro jogador. Escolha outro.";
    }
}
