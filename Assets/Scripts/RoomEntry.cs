using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomInfoText;
    [SerializeField] private Button joinButton;

    private string _sessionName;

    public void Setup(string sessionName, int currentPlayers, int maxPlayers)
    {
        _sessionName = sessionName;

        // Mostra o nome da sala e quantos jogadores tem
        roomInfoText.text = $"{sessionName} ({currentPlayers}/{maxPlayers})";

        // Conecta o bot�o � fun��o de entrar
        joinButton.onClick.AddListener(OnJoinButtonClicked);

        // Desativa o bot�o se a sala estiver cheia
        joinButton.interactable = currentPlayers < maxPlayers;
    }

    private void OnJoinButtonClicked()
    {
        FusionLobbyManager.Instance.JoinRoom(_sessionName);
    }
}