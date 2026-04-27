using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.InferenceEngine;

/// <summary>
/// AI paddle driven by <b>paddle3.onnx</b> via ML-Agents inference.
///
/// COLOUR  : Purple  (0.7, 0.3, 0.9)
/// MODE    : Only activates when GameModeState.SelectedMode == GameMode.AI
/// MODEL   : Loaded automatically from  Resources/Models/paddle3
///
/// Required components on the same GameObject
/// ------------------------------------------
///   • SpriteRenderer
///   • Collider2D + Rigidbody2D
///   • ArkanoidAgent   – supplies observations and applies actions
///   • BehaviorParameters
///   • DecisionRequester
///
/// How to set up in the Inspector
/// --------------------------------
///   1. Add this script to a paddle GameObject (or a duplicate of the
///      player paddle prefab).
///   2. Make sure paddle3.onnx is located at:
///          Assets/Resources/Models/paddle3.onnx
///   3. BehaviorParameters and DecisionRequester are configured at
///      runtime — you do not need to touch them in the Inspector.
///   4. In GameManager, set this object as the AI paddle reference.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(ArkanoidAgent))]
[RequireComponent(typeof(BehaviorParameters))]
[RequireComponent(typeof(DecisionRequester))]
public class AIPaddle : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Constants
    // ------------------------------------------------------------------ //

    /// <summary>Path inside a Resources folder (no extension).</summary>
    private const string ModelResourcePath = "Models/finalpaddle";

    /// <summary>Must match the BehaviorName the model was trained with.</summary>
    private const string BehaviorName = "ArkanoidPaddle";

    // ------------------------------------------------------------------ //
    //  Inspector fields
    // ------------------------------------------------------------------ //

    [Header("Model")]
    [Tooltip("Optional override — leave empty to auto-load paddle3.onnx " +
             "from Resources/Models/.")]
    public ModelAsset modelOverride;

    [Header("Movement")]
    [Tooltip("Move speed passed to ArkanoidAgent (world units / second).")]
    public float moveSpeed    = 11f;
    public float edgePadding  = 0.2f;

    // ------------------------------------------------------------------ //
    //  Private references (resolved once in Awake / Start)
    // ------------------------------------------------------------------ //

    private ArkanoidAgent    agent;
    private BehaviorParameters bp;
    private DecisionRequester  dr;

    // ------------------------------------------------------------------ //
    //  Unity lifecycle
    // ------------------------------------------------------------------ //

    void Awake()
    {
        agent = GetComponent<ArkanoidAgent>();
        bp    = GetComponent<BehaviorParameters>();
        dr    = GetComponent<DecisionRequester>();

        // Apply purple colour immediately (visible in editor too)
        GetComponent<SpriteRenderer>().color = new Color(0.7f, 0.3f, 0.9f);

        // Disable everything until Start() decides whether AI mode is active
        SetAIEnabled(false);
    }

    void Start()
    {
        bool isAiMode = GameModeState.SelectedMode == GameMode.AI;

        if (!isAiMode)
        {
            // Not in AI mode — keep this paddle fully disabled
            Debug.Log("[AIPaddle] Not in AI mode — AI paddle is disabled.");
            gameObject.SetActive(false);   // hide it entirely
            return;
        }

        // ----- AI mode: configure and activate -----

        // Disable any leftover manual-movement scripts
        var playerMovement = GetComponent<PaddleMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        var ballFollower = GetComponent<AIPaddleController>();
        if (ballFollower == null) ballFollower = gameObject.AddComponent<AIPaddleController>();

        var netSync = GetComponent<LocalPaddleNetworkSync>();
        if (netSync != null) netSync.enabled = false;

        var remote = GetComponent<RemotePaddleController>();
        if (remote != null) remote.enabled = false;

        bp.Model = null;
        bp.BehaviorType = BehaviorType.Default;
        bp.enabled = false;
        dr.enabled = false;
        agent.enabled = false;

        GameObject ballObject = GameObject.FindGameObjectWithTag("Ball");
        ballFollower.ballTarget = ballObject != null ? ballObject.transform : null;
        ballFollower.speed = moveSpeed;
        ballFollower.edgePadding = edgePadding;
        ballFollower.reactionDeadZone = 0.1f;
        ballFollower.enabled = true;

        Debug.Log("[AIPaddle] AI paddle is ready using presentation-safe fallback movement.");
    }

    ModelAsset LoadModelFromResources()
    {
        ModelAsset modelAsset = Resources.Load<ModelAsset>(ModelResourcePath);
        if (modelAsset != null)
            return modelAsset;

        ModelAsset[] models = Resources.LoadAll<ModelAsset>("Models");
        foreach (ModelAsset candidate in models)
        {
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    void SetAIEnabled(bool state)
    {
        if (agent != null) agent.enabled = state;
        if (bp    != null) bp.enabled    = state;
        if (dr    != null) dr.enabled    = state;
    }
}
