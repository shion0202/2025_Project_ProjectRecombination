using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLegs : PartBase
{
    [SerializeField] private Transform modelPos;
    private Vector3 defaultMyPos;

    private float ValueA = 0.0f;
    private float ValueB = 0.0f;
    private bool isIncrease = false;
    private bool isJumping = false;

    private PlayerController owner;

    private void Awake()
    {
        defaultMyPos = new Vector3(0.02f, 0.06f, -0.03f);
    }

    private void Update()
    {
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        if (isIncrease)
        {
            ValueA += Time.deltaTime * 160.0f;
            float yValue = Time.deltaTime * 1.2f;
            if (ValueA >= 100.0f)
            {
                ValueA = 100.0f;
            }
            if (modelPos.localPosition.y - yValue <= -0.75f)
            {
                yValue = 0.0f;
            }
            smr.SetBlendShapeWeight(0, ValueA);
            modelPos.localPosition = new Vector3(modelPos.localPosition.x, modelPos.localPosition.y - yValue, modelPos.localPosition.z);
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z + yValue);
        }

        if (isJumping)
        {
            ValueB += Time.deltaTime * 250.0f;
            float yValue = Time.deltaTime * 1.6f;
            if (ValueB >= 50.0f)
            {
                ValueB = 50.0f;
            }
            if (modelPos.localPosition.y - yValue >= 0.25f)
            {
                yValue = 0.0f;
            }
            smr.SetBlendShapeWeight(1, ValueB);
            modelPos.localPosition = new Vector3(modelPos.localPosition.x, modelPos.localPosition.y + yValue, modelPos.localPosition.z);
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z - yValue);
        }

        if (owner != null && owner.FallVelocity.y <= 0.0f)
        {
            isJumping = false;
            ValueB -= Time.deltaTime * 200.0f;
            float yValue = Time.deltaTime * 1.5f;
            if (ValueB <= 0.0f)
            {
                ValueB = 0.0f;
            }
            if (modelPos.localPosition.y - yValue <= 0.0f)
            {
                yValue = 0.0f;
            }
            smr.SetBlendShapeWeight(1, ValueB);
            modelPos.localPosition = new Vector3(modelPos.localPosition.x, modelPos.localPosition.y - yValue, modelPos.localPosition.z);
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z + yValue);
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Jump(owner);
        }
    }

    public override void UseAbility()
    {
        Jump(owner);
        isIncrease = true;
    }

    private void Jump(PlayerController owner)
    {
        owner.PartJump(ValueA * 0.15f);

        isIncrease = false;
        isJumping = true;
        ValueA = 0.0f;
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        smr.SetBlendShapeWeight(0, ValueA);
        modelPos.localPosition = Vector3.zero;
        transform.localPosition = defaultMyPos;
    }

    public override void FinishActionForced()
    {
        Debug.Log("FinishActionForced called in BallLegs");
    }

    public override void UseCancleAbility()
    {
        Debug.Log("UseCancleAbility called in BallLegs");
    }
}
