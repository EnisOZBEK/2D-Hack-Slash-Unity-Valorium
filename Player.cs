using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character, IDamageable
{
    [Header("Input")]
    public KeyCode jumpKey   = KeyCode.Space;
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode blockKey  = KeyCode.Mouse1;
    public KeyCode rollKey   = KeyCode.LeftShift;
    public string  xMoveAxis = "Horizontal";


    [Header("MOVEMENT")]
    private float moveIntentionX = 0;

    [Header("JUMP")]
    private bool attemptJump = false;

    [Header("ROLL")] 
    public  float rollForce     = 6.0f;
    public  float rollDuration  = 0.5f;
    private float rollStartTime = 0.0f;
    private bool  attemptRoll   = false;
    private bool  isRolling     = false;

    [Header("BLOCK")]
    private bool  attemptBlock  = false;
    private bool  isBlocking    = false;
    private float blockDuration = 2f;


    [Header("COMBO ATTACK")]
    public  LayerMask enemyLayer             = 8;
    public  Transform attackOrigin           = null;
    private bool      isComboActive          = false;
    private bool      attemptAttack          = false;
    public  int       maxCombo               = 3;
    private int       comboCount             = 0;
    public  float     attackRadius           = 0.6f;
    public  float     damage                 = 2f;
    public  float     attackDelay            = 0.1f;
    private float     lastAttackTime         = 0f;
    public  float     comboResetTime         = 0.6f;
    public  float     timeBetweenAttacks     = 0.25f;
    public  float     timeUntilAttackReadied = 0;

    void Update()
    {
        GetInput();

        HandleJump();
        HandleBlock();

        if (Time.time - lastAttackTime >= comboResetTime)
        {
            comboCount = 0;
            isComboActive = false;
        }

        if (attemptAttack)
        {
            if (!isComboActive)
            {
                isComboActive = true;
                comboCount = 0;
            }

            if (Time.time - lastAttackTime >= timeBetweenAttacks)
            {
                HandleCombo();
            }
        }

        HandleRoll();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        HandleRun();
    }

    void OnDrawGizmosSelected()
    {
        Debug.DrawRay(transform.position, -Vector2.up * groundedLeeway, Color.green);

        if (attackOrigin != null)
        {
            Gizmos.DrawSphere(attackOrigin.position, attackRadius);
        }
    }

    private void GetInput()
    {
        moveIntentionX = Input.GetAxis(xMoveAxis);
        attemptJump = Input.GetKeyDown(jumpKey);
        attemptAttack = Input.GetKeyDown(attackKey);
        attemptBlock = Input.GetKeyDown(blockKey);
        attemptRoll = Input.GetKeyDown(rollKey);
    }

    private void HandleRun()
    {
        if (moveIntentionX < 0 && transform.eulerAngles.y != 180f)
        {

            transform.rotation = Quaternion.Euler(0, 180f, 0);
        }
        else if (moveIntentionX > 0 && transform.eulerAngles.y != 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        Rb2D.velocity = new Vector2(moveIntentionX * speed, Rb2D.velocity.y);
    }

    private void HandleJump()
    {
        if (attemptJump && CheckGrounded())
        {
            Rb2D.velocity = new Vector2(Rb2D.velocity.x, jumpForce);
        }
    }

    private void HandleRoll()
    {
        if (isRolling)
        {
            if (Time.time - rollStartTime >= rollDuration)
            {
                isRolling = false;
            }
        }

        if (attemptRoll && !isRolling)
        {
            isRolling = true;
            rollStartTime = Time.time;

            Animator animator = GetComponent<Animator>();
            animator.SetTrigger("IsRolling");
        }
    }

    private void HandleCombo()
    {
        if (comboCount < maxCombo)
        {
            comboCount++;

            lastAttackTime = Time.time;

            float attackDamage = 0f;
            switch (comboCount)
            {
                case 1:
                    attackDamage = 2f;
                    break;
                case 2:
                    attackDamage = 3f;
                    break;
                case 3:
                    attackDamage = 5f;
                    break;
            }

            HandleAttack("Attack" + comboCount, attackDamage);
        }
    }
    private void HandleAttack(string animationTrigger, float damage)
    {
        Debug.Log("Player: Attempting Attack");
        Collider2D[] overlappedColliders = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyLayer);

        for (int i = 0; i < overlappedColliders.Length; i++)
        {
            IDamageable enemyAttributes = overlappedColliders[i].GetComponent<IDamageable>();
            if (enemyAttributes != null)
            {
                enemyAttributes.ApplyDamage(damage);
            }
        }

        Animator animator = GetComponent<Animator>();
        animator.SetTrigger(animationTrigger);
    }

    private void HandleBlock()
    {
        if (attemptBlock && !isBlocking)
        {
            isBlocking = true;

            StartCoroutine(StopBlocking());
        }
    }

    private IEnumerator StopBlocking()
    {
        yield return new WaitForSeconds(blockDuration);

        isBlocking = false;
    }

    public virtual void ApplyDamage(float amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void HandleAnimations()
    {
        Animator animator = GetComponent<Animator>();

        animator.SetBool("Grounded", CheckGrounded());
        animator.SetFloat("AirSpeedY", Rb2D.velocity.y);
        animator.SetBool("IsBlocking", isBlocking);

        animator.SetBool("IsComboActive", isComboActive);

        if (CheckGrounded())
        {
            animator.SetBool("Idle", moveIntentionX == 0);
            animator.SetInteger("AnimState", 0);

            if (moveIntentionX != 0)
            {
                animator.SetInteger("AnimState", 1);
                animator.SetTrigger("Running");
            }
        }
        else
        {
            animator.SetBool("Idle", false);
            animator.SetInteger("AnimState", 0);
        }

        animator.SetBool("Jump", !CheckGrounded() && Rb2D.velocity.y > 0);
        animator.SetBool("Block", isBlocking);
    }
}