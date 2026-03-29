using OurGame.Systems;
using UnityEngine;

[DisallowMultipleComponent]
public class ZainoController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string parametroAnim = "statoApertura";

    [Header("State Mapping")]
    [SerializeField] private int closedState = 0;
    [SerializeField] private int centralPocketState = 1;
    [SerializeField] private int bottomPocketState = 2;
    [SerializeField] private int upperPocketState = 1;
    [SerializeField] private int leftPocketState = 1;
    [SerializeField] private int rightPocketState = 1;

    private BackpackInventorySystem inventorySystem;

    void Reset()
    {
        AutoAssignReferences();
    }

    void OnValidate()
    {
        AutoAssignReferences();
    }

    void Awake()
    {
        AutoAssignReferences();
    }

    void OnEnable()
    {
        TryBindInventorySystem();
    }

    void Start()
    {
        TryBindInventorySystem();
    }

    void Update()
    {
        if (inventorySystem == null)
            TryBindInventorySystem();
    }

    private void AutoAssignReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void TryBindInventorySystem()
    {
        BackpackInventorySystem nextSystem = null;
        BackpackInventorySystem.TryGetInstance(out nextSystem);

        if (nextSystem == inventorySystem)
            return;

        Unsubscribe();
        inventorySystem = nextSystem;

        if (inventorySystem == null)
            return;

        inventorySystem.OnPocketOpened += HandlePocketOpened;
        inventorySystem.OnPocketClosed += HandlePocketClosed;
    }

    private void HandlePocketOpened(string pocketName)
    {
        SetAnimatorState(GetPocketState(pocketName));
    }

    private void HandlePocketClosed()
    {
        SetAnimatorState(closedState);
    }

    private int GetPocketState(string pocketName)
    {
        return pocketName switch
        {
            PocketNames.CentralPocket => centralPocketState,
            PocketNames.BottomPocket => bottomPocketState,
            PocketNames.UpperPocket => upperPocketState,
            PocketNames.LeftPocket => leftPocketState,
            PocketNames.RightPocket => rightPocketState,
            _ => closedState
        };
    }

    private void SetAnimatorState(int state)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parametroAnim))
            return;

        animator.SetInteger(parametroAnim, state);
    }

    private void Unsubscribe()
    {
        if (inventorySystem == null)
            return;

        inventorySystem.OnPocketOpened -= HandlePocketOpened;
        inventorySystem.OnPocketClosed -= HandlePocketClosed;
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }
}
