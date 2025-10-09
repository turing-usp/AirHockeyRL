using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyAgentSelector : MonoBehaviour
{
    [Header("Dropdown UI")]
    [SerializeField] TMP_Dropdown dropdown;

    [Header("Agentes Inimigos")]
    [SerializeField] GameObject easyAgent;
    [SerializeField] GameObject mediumAgent;
    [SerializeField] GameObject hardAgent;

    List<GameObject> allAgents;

    void Start()
    {
        // Prepara a lista
        allAgents = new List<GameObject> { easyAgent, mediumAgent, hardAgent };

        // Desativa todos inicialmente
        foreach (var agent in allAgents)
            agent.SetActive(false);

        // Preenche o dropdown
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "PPO", "A2C", "DQN" });

        // Conecta evento
        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        // Ativa o padrão
        ActivateAgent(0);
    }

    void OnDropdownChanged(int index)
    {
        ActivateAgent(index);
    }

    void ActivateAgent(int index)
    {
        for (int i = 0; i < allAgents.Count; i++)
        {
            allAgents[i].SetActive(i == index);
        }

        Debug.Log($"[EnemyAgentSelector] Agente ativado: {dropdown.options[index].text}");
    }
}
