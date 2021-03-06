﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Movement : MonoBehaviour
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;
    private AnimationScript anim;

    [Space]
    [Header("Stats")]
    public float speed = 10;
    public float jumpForce = 50;
    public float slideSpeed = 5;
    public float wallJumpLerp = 10;
    public float dashSpeed = 20;
    public float dashTimer = 0;
    public float maxDash = .3f;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool wallGrab;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;

    [Space]

    public bool groundTouch;
    public bool touchingWall;
    public bool hasDashed;

    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem wallJumpParticle;
    public ParticleSystem slideParticle;

    private Transform slideParticleParent;

    [HideInInspector]
    public bool simulated = false;

    BetterJumping betterJumping;
    RippleEffect rippleEffect;
    Camera camera1;


    // Start is called before the first frame update
    void Start() {
        camera1 = Camera.main;
        rippleEffect = FindObjectOfType<RippleEffect>();
        betterJumping = GetComponent<BetterJumping>();
        Debug.Log("Movement Start");
    }

    void Awake()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<AnimationScript>();
        slideParticleParent = slideParticle.transform.parent;
        rb.velocity = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        touchingWall = coll.onWall;

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        float xRaw = Input.GetAxisRaw("Horizontal");
        float yRaw = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(x, y);

        Walk(dir);
        anim.SetHorizontalMovement(x, y, rb.velocity.y);

        if (coll.onWall && Input.GetButton("Fire3") && canMove)
        {
            if(side != coll.wallSide)
                anim.Flip(side*-1);
            wallGrab = true;
            wallSlide = false;
        }

        if (Input.GetButtonUp("Fire3") || !coll.onWall || !canMove)
        {
            wallGrab = false;
            wallSlide = false;
        }

        if (coll.onGround && !isDashing)
        {
            wallJumped = false;
            GetComponent<BetterJumping>().enabled = true;
        }

        if (wallGrab && !isDashing)
        {
            rb.gravityScale = 0;
            if(x > .2f || x < -.2f)
            rb.velocity = new Vector2(rb.velocity.x, 0);

            float speedModifier = y > 0 ? .5f : 1;

            rb.velocity = new Vector2(rb.velocity.x, y * (speed * speedModifier));
        }
        else
        {
            rb.gravityScale = 3;
        }

        if(coll.onWall && !coll.onGround)
        {
            if (x != 0 && !wallGrab)
            {
                wallSlide = true;
                WallSlide();
            }
        }

        if (!coll.onWall || coll.onGround)
            wallSlide = false;

        if (Input.GetButtonDown("Jump"))
        {
            anim.SetTrigger("jump");

            if (coll.onGround)
                Jump(Vector2.up, false);
            if (coll.onWall && !coll.onGround)
                WallJump();
        }

        if (Input.GetButtonDown("Fire1") && !hasDashed)
        {
            Debug.Log("Dashed: " + xRaw + ", " + yRaw);
            hasDashed = true;
            if(xRaw != 0 || yRaw != 0)
                Dash(xRaw, yRaw);
        }

        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            if (dashTimer >= maxDash)
            {
                DashEnd();
            }
        }

        if (coll.onGround && !groundTouch)
        {
            GroundTouch();
            groundTouch = true;
        }

        if(!coll.onGround && groundTouch)
        {
            groundTouch = false;
        }

        WallParticle(y);

        if (wallGrab || wallSlide || !canMove)
            return;

        if(x > 0)
        {
            side = 1;
            anim.Flip(side);
        }
        if (x < 0)
        {
            side = -1;
            anim.Flip(side);
        }
    }

    void GroundTouch()
    {
        hasDashed = false;
        isDashing = false;

        side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    public void Dash(float x, float y)
    {
        if (!simulated) {
            camera1.transform.DOComplete();
            camera1.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
            rippleEffect.Emit(camera1.WorldToViewportPoint(transform.position));
            anim.SetTrigger("dash");
        }
        hasDashed = true;
        var velocity = rb.velocity;
        velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);

        velocity += dir.normalized * dashSpeed;
        rb.velocity = velocity;

        rb.drag = 13f;
        rb.gravityScale = 0;
        betterJumping.enabled = false;
        wallJumped = true;
        isDashing = true;
        //StartCoroutine(DashWait());
    }

    public void DashEnd()
    {
        rb.drag = 0;

        if (!simulated) {
            dashParticle.Play();
        }
        rb.gravityScale = 3;
        GetComponent<BetterJumping>().enabled = true;
        wallJumped = false;
        isDashing = false;
        dashTimer = 0;
    }

    // private IEnumerator DashWait()
    // {
    //     // FindObjectOfType<GhostTrail>().ShowGhost();
    //     StartCoroutine(GroundDash());
    //     //DOVirtual.Float(14, 0, .8f, RigidbodyDrag);
    //     rb.drag = 13f;
    //
    //     if (!simulated) {
    //         dashParticle.Play();
    //     }
    //     rb.gravityScale = 0;
    //     betterJumping.enabled = false;
    //     wallJumped = true;
    //     isDashing = true;
    //     //Debug.Log(rb.velocity);
    //
    //     yield return new WaitForSeconds(.3f);
    //     rb.drag = 0;
    //
    //     if (!simulated) {
    //         dashParticle.Play();
    //     }
    //     rb.gravityScale = 3;
    //     GetComponent<BetterJumping>().enabled = true;
    //     wallJumped = false;
    //     isDashing = false;
    // }

    public IEnumerator GroundDash()
    {
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
            hasDashed = false;
    }

    public void WallJump()
    {
        if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        {
            side *= -1;
            anim.Flip(side);
        }

        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(.1f));

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

        Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);

        wallJumped = true;
    }

    private void WallSlide()
    {
        if(coll.wallSide != side)
         anim.Flip(side * -1);

        if (!canMove)
            return;

        bool pushingWall = false;
        if((rb.velocity.x > 0 && coll.onRightWall) || (rb.velocity.x < 0 && coll.onLeftWall))
        {
            pushingWall = true;
        }
        float push = pushingWall ? 0 : rb.velocity.x;

        rb.velocity = new Vector2(push, -slideSpeed);
    }

    public void Walk(Vector2 dir)
    {
        if (!canMove)
            return;

        if (wallGrab)
            return;

        if (!wallJumped)
        {
            rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * speed, rb.velocity.y)), wallJumpLerp * Time.deltaTime);
        }
    }

    public void Jump(Vector2 dir, bool wall)
    {

        slideParticleParent.localScale = new Vector3(ParticleSide(), 1, 1);
        ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        particle.Play();
    }

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void RigidbodyDrag(float x)
    {
        rb.drag = x;
    }

    void WallParticle(float vertical)
    {
        var main = slideParticle.main;

        if (wallSlide || (wallGrab && vertical < 0))
        {
            slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
            main.startColor = Color.white;
        }
        else
        {
            main.startColor = Color.clear;
        }
    }

    int ParticleSide()
    {
        int particleSide = coll.onRightWall ? 1 : -1;
        return particleSide;
    }
}
