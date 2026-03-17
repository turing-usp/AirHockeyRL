using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Sentis;                 // <- só para o tipo ModelAsset

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] TMP_Dropdown modelDropdown;
    [SerializeField] Button       playButton;

    // Guardamos também a lista real de assets:
    ModelAsset[] _modelAssets;

    void Start()
    {
        // 1. Carregar TODOS os ModelAsset em Resources/Models
        _modelAssets = Resources.LoadAll<ModelAsset>("Models");

        // 2. Popular o dropdown com os nomes visíveis
        modelDropdown.ClearOptions();
        foreach (var ma in _modelAssets)
            modelDropdown.options.Add(new TMP_Dropdown.OptionData(ma.name));

        modelDropdown.RefreshShownValue();

        // 3. Callback do botão
        playButton.onClick.AddListener(() =>
        {
            if (_modelAssets.Length == 0) return;

            // Salva referência para a próxima cena
            GameConfig.SelectedModelAsset = _modelAssets[modelDropdown.value];

            // Carrega GameScene
            SceneManager.LoadScene("GameScene");
        });
    }
}
