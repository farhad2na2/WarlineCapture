using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class M01PlayableVisualPrototypeController : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Transform playerSquad;
    [SerializeField] private Transform enemyPatrol;
    [SerializeField] private Transform selectionRing;
    [SerializeField] private Transform moveMarker;
    [SerializeField] private Transform attackMarker;
    [SerializeField] private Text selectedText;
    [SerializeField] private Text commandText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text enemyHealthText;
    [SerializeField] private Image enemyHealthFill;
    [SerializeField] private Text toastText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Rect playableBounds = new(-1.35f, -0.68f, 2.7f, 1.36f);
    [SerializeField] private float squadMoveSpeed = 0.24f;
    [SerializeField] private float attackRange = 0.78f;
    [SerializeField] private float attackCooldownSeconds = 0.75f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private int enemyMaxHealth = 100;

    private enum CommandMode
    {
        Direct,
        Move,
        Attack
    }

    private bool selected;
    private bool moving;
    private bool attacking;
    private bool complete;
    private Vector3 moveTarget;
    private CommandMode commandMode;
    private int enemyHealth;
    private float nextAttackAt;
    private bool pendingFirstAttackHit;

    private void Awake()
    {
        enemyHealth = Mathf.Max(1, enemyMaxHealth);
        commandMode = CommandMode.Direct;
        SetVisible(selectionRing, false);
        SetVisible(moveMarker, false);
        SetVisible(attackMarker, false);
        if (resultPanel != null)
            resultPanel.SetActive(false);
        RefreshHud();
    }

    private void Update()
    {
        if (complete)
            return;

        HandleInput();
        TickMovement();
        TickAttack();
        RefreshHud();
    }

    public void SetMoveMode()
    {
        if (!selected)
        {
            ShowToast("Select the squad first.");
            return;
        }

        commandMode = CommandMode.Move;
        ShowToast("Move mode: tap walkable ground.");
        RefreshHud();
    }

    public void SetAttackMode()
    {
        if (!selected)
        {
            ShowToast("Select the squad first.");
            return;
        }

        commandMode = CommandMode.Attack;
        ShowToast("Attack mode: tap enemy patrol.");
        RefreshHud();
    }

    public void StopOrder()
    {
        moving = false;
        attacking = false;
        commandMode = CommandMode.Direct;
        SetVisible(moveMarker, false);
        SetVisible(attackMarker, false);
        ShowToast("Order stopped.");
        RefreshHud();
    }

    private void HandleInput()
    {
        if (gameplayCamera == null || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 world = gameplayCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        world.z = 0f;

        if (IsNear(world, playerSquad.position, 0.18f))
        {
            SelectSquad();
            return;
        }

        if (!selected)
        {
            ShowToast("Select a squad first.");
            return;
        }

        bool tappedEnemy = IsNear(world, enemyPatrol.position, 0.20f);
        if ((commandMode == CommandMode.Attack || commandMode == CommandMode.Direct) && tappedEnemy)
        {
            IssueAttack();
            return;
        }

        if (commandMode == CommandMode.Attack && !tappedEnemy)
        {
            ShowToast("Choose an enemy target.");
            return;
        }

        if (!playableBounds.Contains(new Vector2(world.x, world.y)))
        {
            ShowToast("Target outside mission area.");
            return;
        }

        IssueMove(world);
    }

    private void SelectSquad()
    {
        selected = true;
        SetVisible(selectionRing, true);
        if (selectionRing != null)
            selectionRing.position = playerSquad.position + new Vector3(0f, -0.025f, -0.02f);
        ShowToast("Rifle squad selected.");
    }

    private void IssueMove(Vector3 target)
    {
        moving = true;
        attacking = false;
        commandMode = CommandMode.Direct;
        moveTarget = target;
        if (moveMarker != null)
            moveMarker.position = target + new Vector3(0f, 0f, -0.02f);
        SetVisible(moveMarker, true);
        SetVisible(attackMarker, false);
        ShowToast("Move order accepted.");
    }

    private void IssueAttack()
    {
        moving = false;
        attacking = true;
        pendingFirstAttackHit = true;
        commandMode = CommandMode.Direct;
        SetVisible(moveMarker, false);
        if (attackMarker != null)
            attackMarker.position = enemyPatrol.position + new Vector3(0f, 0.02f, -0.02f);
        SetVisible(attackMarker, true);
        nextAttackAt = Time.time + 0.15f;
        ShowToast("Attack order accepted. Closing to range.");
    }

    private void TickMovement()
    {
        if (!moving || playerSquad == null)
            return;

        playerSquad.position = Vector3.MoveTowards(playerSquad.position, moveTarget, squadMoveSpeed * Time.deltaTime);
        if (selectionRing != null && selected)
            selectionRing.position = playerSquad.position + new Vector3(0f, -0.025f, -0.02f);

        if (Vector3.Distance(playerSquad.position, moveTarget) <= 0.015f)
        {
            moving = false;
            SetVisible(moveMarker, false);
        }
    }

    private void TickAttack()
    {
        if (!attacking || playerSquad == null || enemyPatrol == null)
            return;

        float distance = Vector3.Distance(playerSquad.position, enemyPatrol.position);
        if (distance > attackRange)
        {
            Vector3 approach = Vector3.MoveTowards(playerSquad.position, enemyPatrol.position, Mathf.Max(0f, distance - attackRange + 0.02f));
            playerSquad.position = Vector3.MoveTowards(playerSquad.position, approach, squadMoveSpeed * Time.deltaTime);
            if (selectionRing != null && selected)
                selectionRing.position = playerSquad.position + new Vector3(0f, -0.025f, -0.02f);
            if (pendingFirstAttackHit && Time.time >= nextAttackAt)
            {
                ApplyAttackDamage("First shots fired");
                pendingFirstAttackHit = false;
            }
            return;
        }

        if (Time.time < nextAttackAt)
            return;

        ApplyAttackDamage("Hit confirmed");
    }

    private void ApplyAttackDamage(string prefix)
    {
        nextAttackAt = Time.time + attackCooldownSeconds;
        enemyHealth = Mathf.Max(0, enemyHealth - attackDamage);
        ShowToast($"{prefix}. Enemy patrol {enemyHealth}/{enemyMaxHealth}.");
        if (enemyPatrol != null)
            enemyPatrol.localScale *= 0.96f;
        if (enemyHealth <= 0)
        {
            complete = true;
            attacking = false;
            enemyPatrol.gameObject.SetActive(false);
            SetVisible(attackMarker, false);
            ShowToast("Objective complete.");
            if (resultPanel != null)
                resultPanel.SetActive(true);
        }
    }

    private void RefreshHud()
    {
        if (selectedText != null)
            selectedText.text = selected ? "SELECTED: Rifle Squad" : "SELECTED: none";

        if (commandText != null)
        {
            string order = attacking ? "ATTACKING" : moving ? "MOVING" : commandMode == CommandMode.Move ? "MOVE TARGET" : commandMode == CommandMode.Attack ? "ATTACK TARGET" : "DIRECT COMMAND";
            commandText.text = "ORDER: " + order;
        }

        if (objectiveText != null)
            objectiveText.text = complete ? "OBJECTIVE: Hostile patrol destroyed" : "OBJECTIVE: Destroy hostile patrol";

        if (enemyHealthText != null)
            enemyHealthText.text = complete ? "ENEMY PATROL: destroyed" : $"ENEMY PATROL: {enemyHealth}/{enemyMaxHealth}";

        if (enemyHealthFill != null)
            enemyHealthFill.fillAmount = complete ? 0f : Mathf.Clamp01((float)enemyHealth / Mathf.Max(1, enemyMaxHealth));
    }

    private void ShowToast(string message)
    {
        if (toastText != null)
            toastText.text = message;
    }

    private static bool IsNear(Vector3 point, Vector3 target, float radius)
    {
        return Vector2.Distance(point, target) <= radius;
    }

    private static void SetVisible(Transform target, bool visible)
    {
        if (target != null)
            target.gameObject.SetActive(visible);
    }
}
