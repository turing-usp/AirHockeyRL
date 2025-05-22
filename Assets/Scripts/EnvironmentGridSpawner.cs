/*******************************************************
 *  EnvironmentGridSpawner.cs   ―   centred grid (no camera)
 *  Attach to the root “Multiple”.
 *******************************************************/
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;   // PrefabUtility for edit-time instancing
#endif

[ExecuteAlways]
public class EnvironmentGridSpawner : MonoBehaviour
{
    /* ───────── Grid parameters ───────── */
    [Header("Environment prefab (asset)")]
    public GameObject environmentPrefab;        // drag Env.prefab here

    [Header("Grid size")]
    [Min(1)] public int rows    = 3;
    [Min(1)] public int columns = 3;

    [Header("Spacing (metres)")]
    public float stepX = 10f;                   // left ↔ right
    public float stepZ = 15f;                   // front ↔ back

    const string CLONE_PREFIX = "EnvClone_";

    /* ───────── Life-cycle hooks ───────── */
    void OnEnable()          => BuildGrid();

#if UNITY_EDITOR
    void OnValidate()        // auto-rebuild when inspector values change
    {
        if (environmentPrefab) BuildGrid();
    }
#endif

    /* ───────── Build / rebuild routine ───────── */
    void BuildGrid()
    {
        if (!environmentPrefab)
        {
            Debug.LogWarning($"[{name}] Please assign an Environment prefab.");
            return;
        }

        /* 1 ─ delete previous clones */
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.name.StartsWith(CLONE_PREFIX))
            {
#if UNITY_EDITOR
                if (Application.isPlaying) Destroy(c.gameObject);
                else                       DestroyImmediate(c.gameObject);
#else
                Destroy(c.gameObject);
#endif
            }
        }

        /* 2 ─ figure out where the ORIGINAL sits in the grid */
        int centreRow = rows    / 2;
        int centreCol = columns / 2;
        bool evenRow  = rows    % 2 == 0;
        bool evenCol  = columns % 2 == 0;

        Vector3 basePos = environmentPrefab.transform.localPosition;

        /* 3 ─ spawn clones (skip the original’s cell) */
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (!evenRow && !evenCol && r == centreRow && c == centreCol)
                    continue;                           // leave template as is

                float offX = (c - centreCol + (evenCol ? 0.5f : 0f)) * stepX;
                float offZ = (r - centreRow + (evenRow ? 0.5f : 0f)) * stepZ;

                GameObject clone;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    clone = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab, transform);
                else
                    clone = Instantiate(environmentPrefab, transform);
#else
                clone = Instantiate(environmentPrefab, transform);
#endif
                clone.transform.localPosition = basePos + new Vector3(offX, 0f, offZ);
                clone.name = $"{CLONE_PREFIX}{r}_{c}";
            }
        }
    }
}
