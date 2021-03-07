using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForwardLocomotor : Locomotor
{
    public override Vector3 GetInputAxis()
    {
        return Vector3.forward * Control.InputAxis.x;
    }

    public override Vector3 GetEngineForce()
    {
        TargetSpeed = new Vector2(InputAxis.z * VibroengineVelocity, 0f) * Gears[Gear];
        Vector3 TVDifference = new Vector3(TargetSpeed.y - Velocity.x, 0, Mathf.Clamp(TargetSpeed.x - Velocity.z, -Speed.x / 4, Speed.x / 4) * Traction);
        return TVDifference * SpaceEngineForce;
    }

    public override void SetVibroengineDrag()
    {
        VibroengineVelocity -= (EngineForce.z * VibroengineSide.x) / VibroengineMass * Gears[Gear] * Time.fixedDeltaTime;
    }

    public override void SetGear()
    {
        int max = 10;
        Vector3 TargetSpaceVelocity = new Vector3(0, 0, TargetSpeed.x);
        if (TargetSpaceVelocity == Vector3.zero)
            TargetSpaceVelocity = Velocity;

        Quaternion TV = Quaternion.LookRotation(TargetSpaceVelocity);
        Vector3 tvVel = TV.GetInverse() * Velocity;
        max = (int)GearTurns.Evaluate(tvVel.z / Speed.x);

        if (GearTurnTimer <= 0)
        {
            if (VibroengineVelocity > Speed.x * GrowingGearTurns)
            {
                int next = Mathf.Min(max, Mathf.Clamp(Gear + 1, 0, Gears.Count - 1));
                if (next != Gear)
                {
                    Gear = next;
                    Traction = 0;
                }
                GearTurnTimer = 0.5f;
            }
            if (VibroengineVelocity < Speed.x * LowingGearTurns)
            {
                int next = Mathf.Clamp(Gear - 1, 0, Gears.Count - 1);
                if (next != Gear)
                {
                    Gear = next;
                    Traction = 0;
                }
                GearTurnTimer = 0.5f;
            }
        }
        GearTurnTimer -= Time.fixedDeltaTime;
    }

    public override float GetEngineTenson()
    {
        return EngineForce.z;
    }

    public override Vector3 GetGyroscopeForce()
    {
        float ZMP = InverceBacktreck ? Side(Control.InputAxis.x + 0.01f) : 1f;
        if (Control.LocomotionType == LocomotorType.Forward)
            ZMP = (InverceBacktreck ? Side(Velocity.z) : 1f) * Mathf.Clamp(Rigid.velocity.magnitude / 15, -1, 1);

        float TarY = Control.InputAxis.y * ZMP  * RotationVelocty.y;
        return new Vector3(Mathf.Clamp(-AngularVelocity.x * DragForce, -1, 1) * GyroscopeForce.x, Mathf.Clamp((TarY - AngularVelocity.y) * DragForce, -1, 1) * GyroscopeForce.y, Mathf.Clamp(-AngularVelocity.z * DragForce, -1, 1) * GyroscopeForce.z);
    }
    public override void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {
        Parameters.Add("Движение");
        Values.Add("Продольное");
        Parameters.Add("Масса");
        Values.Add(GetComponent<Rigidbody>().mass + " кг");
        Parameters.Add("Скорость");
        Values.Add((Mathf.Ceil(Speed.x * 3.6f)).ToString() + " км/ч");
        Parameters.Add("Количество опор");
        Values.Add((Supports.Count).ToString());
        Parameters.Add("Мощность опор");
        Values.Add((Supports[0].Force * FORCE / 1000000) + " Т");
        Parameters.Add("Мощность \nвибродвигателя");
        Values.Add("");
        Values.Add((VibroengineForce) + " м/сек2");
        Parameters.Add("Мощность плоскостей \n сцепления");
        Values.Add("");
        Values.Add((SpaceEngineForce) + " КН");
        Parameters.Add("Мощность гироскопа");
        Values.Add(GyroscopeForce.ToString());
    }
}
