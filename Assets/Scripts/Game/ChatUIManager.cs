using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI de chat da GameScene: chat global (todo mundo v�) e chat privado (s� o destinat�rio v�).
// As RPCs de envio est�o no PlayerCharacter do jogador local (ver PlayerCharacter.LocalCharacter).
public class ChatUIManager : MonoBehaviour
{
    public static ChatUIManager Instance { get; private set; }

    [Header("Chat global")]
    [SerializeField] private TMP_InputField globalMessageInput;
    [SerializeField] private Button globalSendButton;
    [SerializeField] private TextMeshProUGUI globalChatLog; // uma linha por mensagem, visto por todos

    [Header("Chat privado")]
    [SerializeField] private TMP_Dropdown targetPlayerDropdown; // lista de personagens atuais (menos o pr�prio)
    [SerializeField] private Button refreshTargetsButton; // repopula o dropdown acima
    [SerializeField] private TMP_InputField privateMessageInput;
    [SerializeField] private Button privateSendButton;
    [SerializeField] private TextMeshProUGUI privateChatLog; // separado do global, s� aparece pra quem recebe

    // Guarda o PlayerRef correspondente a cada op��o do dropdown (na mesma ordem).
    private readonly List<PlayerRef> _targetPlayers = new List<PlayerRef>();

    private void Awake()
    {
        Instance = this;

        globalSendButton.onClick.AddListener(SendGlobalMessage);
        privateSendButton.onClick.AddListener(SendPrivateMessage);
        refreshTargetsButton.onClick.AddListener(RefreshTargetDropdown);

        RefreshTargetDropdown();
    }

    private void SendGlobalMessage()
    {
        if (PlayerCharacter.LocalCharacter == null || string.IsNullOrWhiteSpace(globalMessageInput.text)) return;

        PlayerCharacter.LocalCharacter.RPC_SendGlobalMessage(LocalPlayerSettings.Nickname, globalMessageInput.text);
        globalMessageInput.text = "";
    }

    private void SendPrivateMessage()
    {
        if (PlayerCharacter.LocalCharacter == null || string.IsNullOrWhiteSpace(privateMessageInput.text)) return;
        if (targetPlayerDropdown.options.Count == 0 || targetPlayerDropdown.value >= _targetPlayers.Count) return;

        PlayerRef target = _targetPlayers[targetPlayerDropdown.value];
        PlayerCharacter.LocalCharacter.RPC_SendPrivateMessage(target, LocalPlayerSettings.Nickname, privateMessageInput.text);
        privateMessageInput.text = "";
    }

    // Repopula a lista de jogadores dispon�veis pra mensagem privada. Feito sob demanda
    // (bot�o "Atualizar"), n�o todo frame - suficiente pro escopo dessa tarefa.
    private void RefreshTargetDropdown()
    {
        _targetPlayers.Clear();
        var options = new List<string>();

        foreach (var character in FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
        {
            if (character == PlayerCharacter.LocalCharacter) continue;
            _targetPlayers.Add(character.Object.InputAuthority);
            options.Add(character.Nickname.ToString());
        }

        targetPlayerDropdown.ClearOptions();
        targetPlayerDropdown.AddOptions(options);
    }

    // Chamado pela RPC_SendGlobalMessage de qualquer PlayerCharacter (roda em todo mundo).
    public void AddGlobalMessage(string senderName, string message)
    {
        if (globalChatLog != null)
            globalChatLog.text += $"\n{senderName}: {message}";
    }

    // Chamado pela RPC_SendPrivateMessage, mas s� roda na m�quina do destinat�rio (ver [RpcTarget]).
    public void AddPrivateMessage(string senderName, string message)
    {
        if (privateChatLog != null)
            privateChatLog.text += $"\n[privado] {senderName}: {message}";
    }
}
