/*******************************************************
 * EnvironmentGridSpawner.cs
 *  - Keeps the scene template in place (never moved)
 *  - Fills the rest of the grid around that anchor
 *  - Optionally reads grid size from ML-Agents environment parameters
 *******************************************************/
using System;
using UnityEngine;
using Unity.MLAgents;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class EnvironmentGridSpawner : MonoBehaviour
{
    [Header("Environment Template (scene object or prefab)")]
    public GameObject environmentTemplate;

    [Header("Grid Size (odd numbers only)")]
    [Min(1)] public int rows = 3;
    [Min(1)] public int columns = 3;

    [Header("Grid Size via Environment Parameters")]
    [SerializeField] private bool useEnvironmentGridOverrides = true;
    [SerializeField] private string rowsParamName = "grid_rows";
    [SerializeField] private string columnsParamName = "grid_columns";

    [Header("Spacing (meters)")]
    public float stepX = 4.5f;
    public float stepZ = 2.5f;

    private const string CLONE_PREFIX = "EnvClone_";

    private void OnEnable()
    {
        EnforceOddGridSize();
        ApplyGridOverridesFromEnvironment();
        BuildGrid();
    }

    private void OnDisable()
    {
        ClearAllClones();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnforceOddGridSize();

        if (environmentTemplate && !Application.isPlaying &&
            string.IsNullOrEmpty(Undo.GetCurrentGroupName()))
        {
            EditorApplication.delayCall -= RebuildDelayed;
            EditorApplication.delayCall += RebuildDelayed;
        }
    }

    private void RebuildDelayed()
    {
        if (this && environmentTemplate) BuildGrid();
    }
#endif

    [ContextMenu("Rebuild Grid Now")]
    public void ForceRebuildGrid()
    {
        EnforceOddGridSize();
        ApplyGridOverridesFromEnvironment();
        BuildGrid();
    }

    private void ApplyGridOverridesFromEnvironment()
    {
        if (!Application.isPlaying || !useEnvironmentGridOverrides) return;

        try
        {
            EnvironmentParameters env = Academy.Instance.EnvironmentParameters;
            int newRows = Mathf.RoundToInt(env.GetWithDefault(rowsParamName, rows));
            int newCols = Mathf.RoundToInt(env.GetWithDefault(columnsParamName, columns));
            rows = ClampOddAtLeastOne(newRows);
            columns = ClampOddAtLeastOne(newCols);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[{name}] Could not read environment grid overrides: {ex.Message}");
        }
    }

    private void EnforceOddGridSize()
    {
        rows = ClampOddAtLeastOne(rows);
        columns = ClampOddAtLeastOne(columns);
    }

    private static int ClampOddAtLeastOne(int value)
    {
        int v = Mathf.Max(1, value);
        if ((v % 2) == 0) v += 1;
        return v;
    }

    private void ClearAllClones()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            Transform t = transform.GetChild(i);
            if (t && t.name.StartsWith(CLONE_PREFIX))
            {
#if UNITY_EDITOR
                if (Application.isPlaying) Destroy(t.gameObject);
                else Undo.DestroyObjectImmediate(t.gameObject);
#else
                Destroy(t.gameObject);
#endif
            }
        }
    }

    private void BuildGrid()
    {
        if (!environmentTemplate)
        {
            Debug.LogWarning($"[{name}] Environment template not set; aborting.");
            return;
        }

        ClearAllClones();

        bool templateIsSceneObj = environmentTemplate.scene.IsValid();
        Vector3 anchorLocalPos = templateIsSceneObj
            ? environmentTemplate.transform.localPosition
            : Vector3.zero;

        int centreRow = rows / 2;
        int centreCol = columns / 2;
        bool evenRow = (rows % 2) == 0;
        bool evenCol = (columns % 2) == 0;

        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < columns; ++c)
            {
                if (templateIsSceneObj && r == centreRow && c == centreCol)
                    continue;

                float offX = (c - centreCol + (evenCol ? 0.5f : 0f)) * stepX;
                float offZ = (r - centreRow + (evenRow ? 0.5f : 0f)) * stepZ;

                Vector3 localPos = anchorLocalPos + new Vector3(offX, 0f, offZ);
                Vector3 worldPos = transform.TransformPoint(localPos);

                GameObject clone = Instantiate(
                    environmentTemplate,
                    worldPos,
                    environmentTemplate.transform.rotation,
                    transform
                );
                clone.name = $"{CLONE_PREFIX}{r}_{c}";

                // Remove this script if it is present on cloned prefabs.
                if (clone.TryGetComponent(out EnvironmentGridSpawner rogue))
                {
#if UNITY_EDITOR
                    if (Application.isPlaying) Destroy(rogue);
                    else Undo.DestroyObjectImmediate(rogue);
#else
                    Destroy(rogue);
#endif
                }
            }
        }

        if (templateIsSceneObj && environmentTemplate.transform.parent != transform)
            environmentTemplate.transform.SetParent(transform, true);
    }

#if UNITY_EDITOR
    static EnvironmentGridSpawner()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingPlayMode) return;

        foreach (EnvironmentGridSpawner s in FindObjectsOfType<EnvironmentGridSpawner>())
            s.ClearAllClones();
    }
#endif
}
