using System;
using UnityEngine;
namespace Volleyball
{
    [RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
    public sealed class BallController : MonoBehaviour
    {
        public PrototypeSettings Settings;
        public event Action<Vector3> Landed;
        public event Action Faulted;
        float stalledTime;
        public Rigidbody Body { get; private set; }
        public bool IsLive { get; private set; }
        public bool HasSimulationAuthority { get; private set; } = true;
        Vector3 replicatedVelocity;
        public Vector3 Velocity => HasSimulationAuthority && Body ? Body.linearVelocity : replicatedVelocity;
        public int HitSerial { get; private set; }
        void Awake()
        {
            Body=GetComponent<Rigidbody>(); Body.useGravity=false;
            Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            Body.interpolation=RigidbodyInterpolation.Interpolate;
            Body.linearDamping=0; Body.angularDamping=0.05f;
        }
        public void Hold(Vector3 position)
        {
            if(!Body) Awake();
            if(!HasSimulationAuthority) return;
            if(!Body.isKinematic) { Body.linearVelocity=Vector3.zero; Body.angularVelocity=Vector3.zero; }
            IsLive=false; stalledTime=0; Body.isKinematic=true; Body.position=position; transform.position=position;
        }
        public void Launch(Vector3 velocity)
        {
            if(!HasSimulationAuthority) return;
            Body.isKinematic=false; IsLive=true; stalledTime=0;
            Body.linearVelocity=Vector3.ClampMagnitude(velocity,Settings.maxBallSpeed); HitSerial++;
        }
        void FixedUpdate()
        {
            if(!HasSimulationAuthority || !IsLive || !Settings) return;
            Body.AddForce(Vector3.down * Settings.gravity,ForceMode.Acceleration);
            if(Body.linearVelocity.sqrMagnitude > Settings.maxBallSpeed*Settings.maxBallSpeed)
                Body.linearVelocity=Vector3.ClampMagnitude(Body.linearVelocity,Settings.maxBallSpeed);
            Vector3 p=Body.position;
            stalledTime=Body.linearVelocity.sqrMagnitude<0.04f?stalledTime+Time.fixedDeltaTime:0;
            if(p.y < -3 || Mathf.Abs(p.x)>25 || Mathf.Abs(p.z)>30 || stalledTime>2)
            {
                IsLive=false; Faulted?.Invoke();
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if(HasSimulationAuthority && IsLive && collision.collider.GetComponent<CourtSurface>()) Finish(Body.position);
        }
        void Finish(Vector3 point)
        {
            IsLive=false;
            Landed?.Invoke(point);
        }
        public void SetSimulationAuthority(bool authoritative)
        {
            if(!Body) Awake();
            HasSimulationAuthority=authoritative;
            Body.useGravity=false;
            if(!authoritative)
            {
                if(!Body.isKinematic)
                {
                    Body.linearVelocity=Vector3.zero;
                    Body.angularVelocity=Vector3.zero;
                }
                Body.isKinematic=true;
            }
        }
        public void ApplyNetworkState(BallNetworkState state,bool snap,float interpolation=18f)
        {
            if(HasSimulationAuthority) return;
            if(!Body) Awake();
            float blend=snap?1f:1f-Mathf.Exp(-interpolation*Time.unscaledDeltaTime);
            Vector3 position=Vector3.Lerp(Body.position,state.Position,blend);
            Quaternion rotation=Quaternion.Slerp(Body.rotation,state.Rotation,blend);
            Body.position=position; Body.rotation=rotation;
            transform.SetPositionAndRotation(position,rotation);
            replicatedVelocity=state.Velocity;
            IsLive=state.IsLive;
            HitSerial=state.HitSerial;
        }
    }
}
