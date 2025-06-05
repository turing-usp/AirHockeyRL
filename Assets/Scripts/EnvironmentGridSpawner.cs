/*******************************************************
 * EnvironmentGridSpawner.cs
 *  – Keeps the *scene* template in place (never moved)
 *  – Fills the rest of the grid around that anchor
 *  – Deletes only clones when you press ■ Stop
 *******************************************************/
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class EnvironmentGridSpawner : MonoBehaviour
{
    /* ─────────────  Inspector  ───────────── */

    [Header("Environment Template (scene object or prefab)")]
    public GameObject environmentTemplate;

    [Header("Grid Size")]
    [Min(1)] public int rows    = 3;
    [Min(1)] public int columns = 3;

    [Header("Spacing (metres)")]
    public float stepX = 4.5f;
    public float stepZ = 2.5f;

    /* ─────────────  Internals  ───────────── */

    private const string CLONE_PREFIX = "EnvClone_";

    /* ─────────────  Life-cycle  ───────────── */

    void OnEnable()  => BuildGrid();
    void OnDisable() => ClearAllClones();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (environmentTemplate && !Application.isPlaying &&
            string.IsNullOrEmpty(Undo.GetCurrentGroupName()))
        {
            EditorApplication.delayCall -= RebuildDelayed;
            EditorApplication.delayCall += RebuildDelayed;
        }
    }
    void RebuildDelayed()
    {
        if (this && environmentTemplate) BuildGrid();
    }
#endif

    [ContextMenu("Rebuild Grid Now")]
    public void ForceRebuildGrid() => BuildGrid();

    /* ─────────────  Helpers  ───────────── */

    void ClearAllClones()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            Transform t = transform.GetChild(i);
            if (t && t.name.StartsWith(CLONE_PREFIX))
            {
#if UNITY_EDITOR
                if (Application.isPlaying) Destroy(t.gameObject);
                else                        Undo.DestroyObjectImmediate(t.gameObject);
#else
                Destroy(t.gameObject);
#endif
            }
        }
    }

    void BuildGrid()
    {
        if (!environmentTemplate)
        {
            Debug.LogWarning($"[{name}] Environment template not set — aborting.");
            return;
        }

        ClearAllClones();                                   // fresh slate

        /* 0 ─ Anchor: where is the *original* template?   *
         *     If it's a scene object it stays put;        *
         *     if it's a prefab we'll treat (0,0,0) as the *
         *     anchor so the whole grid is cloned.         */
        bool   templateIsSceneObj = environmentTemplate.scene.IsValid();
        Vector3 anchorLocalPos    = templateIsSceneObj
                                    ? environmentTemplate.transform.localPosition
                                    : Vector3.zero;        // prefab case
        float  baseY              = anchorLocalPos.y;      // keep table height

        /* 1 ─ Geometry helpers (same for odd/even grids) */
        int  centreRow = rows    / 2;                      // ⌊n/2⌋
        int  centreCol = columns / 2;
        bool evenRow   = (rows    % 2) == 0;
        bool evenCol   = (columns % 2) == 0;

        /* 2 ─ Spawn every cell except the anchor itself  */
        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < columns; ++c)
            {
                /* skip the cell occupied by the scene template */
                if (templateIsSceneObj && r == centreRow && c == centreCol)
                    continue;

                float offX = (c - centreCol + (evenCol ? 0.5f : 0f)) * stepX;
                float offZ = (r - centreRow + (evenRow ? 0.5f : 0f)) * stepZ;

                Vector3 localPos = anchorLocalPos + new Vector3(offX, 0f, offZ);
                Vector3 worldPos = transform.TransformPoint(localPos);

                GameObject clone = Instantiate(environmentTemplate,
                                               worldPos,
                                               environmentTemplate.transform.rotation,
                                               transform);
                clone.name = $"{CLONE_PREFIX}{r}_{c}";

                /* Remove this script if it sneaks in with the prefab */
                if (clone.TryGetComponent(out EnvironmentGridSpawner rogue))
                {
#if UNITY_EDITOR
                    if (Application.isPlaying) Destroy(rogue);
                    else                        Undo.DestroyObjectImmediate(rogue);
#else
                    Destroy(rogue);
#endif
                }
            }
        }

        /* 3 ─ Ensure the original template is parented to the spawner      *
         *     (purely for organisational purposes and consistent movement) */
        if (templateIsSceneObj && environmentTemplate.transform.parent != transform)
            environmentTemplate.transform.SetParent(transform, true);
    }

    /* ─────────────  Auto-clean on ■ Stop  ───────────── */
#if UNITY_EDITOR
    static EnvironmentGridSpawner()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingPlayMode) return;

        foreach (EnvironmentGridSpawner s in FindObjectsOfType<EnvironmentGridSpawner>())
            s.ClearAllClones();                          // original arena stays
    }
#endif
}
