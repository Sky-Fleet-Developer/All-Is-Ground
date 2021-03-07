using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FLAIBot : AIBot
{
    bool Backtreck = false;
    float BacktreckValue;
    float BacktreckTimer;
    float StoppingTimer;
    Vector3 toWaypoint;
    float dist;
    float forward;

    protected override void SetControlInputAxis()
    {
        control.InputAxis = DefaultControlImputAxis();
    }

    Vector2 DefaultControlImputAxis()
    {
        RaycastHit ObstacleHit;

        if (Physics.Raycast(Tr.position + Tr.up * 0.1f, Tr.forward, out ObstacleHit, 3.6f, GameValues.BotAimingLayer))
        {
            if (Vector3.Angle(ObstacleHit.normal, Tr.up) > 55)
            {
                BacktreckValue = 1.5f;
            }
            Debug.DrawRay(Tr.position + Tr.up * 0.1f, Tr.forward * ObstacleHit.distance, Color.red);
        }
        else
        {
            Debug.DrawRay(Tr.position + Tr.up * 0.1f, Tr.forward * (3.6f), Color.green);
        }

        BacktreckValue = Mathf.MoveTowards(BacktreckValue, 0f, Time.deltaTime);

        toWaypoint = Tr.InverseTransformDirection(PathWaypoint - Tr.position);

        dist = toWaypoint.magnitude;

        if (IsEndPoint)
            forward = Mathf.Clamp(toWaypoint.z * 0.1f - Velocity.z * 0.1f, -1, 1);
        else forward = Mathf.Clamp(dist * 0.1f, 0.2f, 1);

        if (dist > 10)
        {
            forward = Mathf.Abs(forward);
        }
        toWaypoint.Normalize();

        float angle = Mathf.Atan2(toWaypoint.x, toWaypoint.z);

        float side = Mathf.Clamp(angle - Velocity.x / Mathf.Clamp(Mathf.Abs(Velocity.z), 1, 20) * 0.5f, -1, 1);

        if (forward > 0.5f)
            if (Mathf.Abs(Velocity.z) < 1)
                StoppingTimer += Time.deltaTime;
            else
                StoppingTimer = 0f;

        if (StoppingTimer > 3 && (int)StoppingTimer % 3 == 0)
        {
            BacktreckValue = 1f;
        }

        Backtreck = Mathf.Sin(BacktreckValue * 2) > 0.1f;

        Vector3 dirToNext = NextWaypoint - PathWaypoint;
        if (WantToJump)
        {
            Vector3 dirY = dirToNext + Vector3.down * dirToNext.y;
            Vector3 fY = Tr.forward;
            fY = fY + Vector3.down * fY.y;
            if (Vector3.Angle(dirY, fY) > 25)
                forward *= 0.2f;
        }
        else
        {
            float a = Vector3.Angle(dirToNext, Tr.forward);
            forward = Mathf.Lerp(forward, (-Velocity.z + 10) * 0.2f, a / 90);
        }

        forward = Backtreck ? -3 : forward;

        if (Backtreck)
        {
            BacktreckTimer += Time.deltaTime;
            if (!CheckGround())
            {
                forward = -Velocity.z;
            }
        }
        else
            BacktreckTimer = 0;

        forward = Mathf.Clamp(forward + Tr.forward.y * 0.8f, -1, 1);
        side = Mathf.Clamp(side * (locomotor.InverceBacktreck ? Side(Velocity.z) : 1), -1, 1);

        return new Vector2(forward, side);
    }

    protected override void SetFlyInputAxis()
    {
        Vector3 pp = PathWaypoint;
        pp.y = Tr.position.y;
        Vector3 itf = Tr.InverseTransformPoint(pp).normalized;
        control.InputAxis = new Vector2(Tr.forward.y * 2 + itf.z * 0.5f, itf.x * 5 - AngularVelocity.y * 0.8f);
    }

    protected override void SetCombatControlInputAxis()
    {
        var IA = DefaultControlImputAxis();
        switch (IsEndPoint && dist < 4 && !CanRocketShoot)
        {
            case true:
                Vector3 localF = Tr.InverseTransformDirection(control.Forward);
                float side = Mathf.Clamp(localF.normalized.x * 1.5f, -1, 1);
                forward = Side(toWaypoint.z);
                control.InputAxis = new Vector2(forward, side);
                break;
            case false:
                control.InputAxis = IA;
                break;
        }
    }

    public bool CheckGround()
    {
        RaycastHit GroundHit;
        Vector3 pos = Tr.TransformPoint(new Vector3(0, 0.2f, 3.5f * Side(Velocity.z) * Velocity.z * 0.1f));
        return Physics.Raycast(pos, Vector3.down, out GroundHit, 5, GameValues.BotAimingLayer);
    }
}
