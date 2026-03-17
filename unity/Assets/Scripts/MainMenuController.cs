using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Este método será chamado pelo botão JOGAR
    public void PlayGame()
    {
        Debug.Log("Carregando o jogo...");
        // Carrega a cena pelo nome. O nome DEVE ser exatamente igual ao da sua cena de jogo.
        SceneManager.LoadScene("GameScene"); 
    }

    // Este método será chamado pelo botão SAIR
    public void QuitGame()
    {
        Debug.Log("Saindo do jogo..."); // Isso vai aparecer no Console
        Application.Quit(); // Isso só funciona na build final (.exe)
    }
}