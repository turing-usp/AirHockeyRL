// Assets/Scripts/Game/GameSetup.cs
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Sentis;            // Sentis API

public class GameSetup : MonoBehaviour
{
    [SerializeField] Agent orangeAgent;   // arraste o pusher laranja

    void Start()
    {
        var chosen = GameConfig.SelectedModelAsset;
        if (chosen == null)
        {
            Debug.LogWarning("Nenhum modelo selecionado; mantendo o que já está no agente.");
            return;
        }

        var bp = orangeAgent.GetComponent<BehaviorParameters>();
        orangeAgent.SetModel(bp.BehaviorName, chosen);   // overload Sentis (ModelAsset)
    }
}
