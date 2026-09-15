using System;
using Unity.Netcode;
using UnityEngine;

namespace Volleyball
{
    public struct ActionNetworkState : INetworkSerializable, IEquatable<ActionNetworkState>
    {
        public ServeStage ServeStage;
        public AttackStage AttackStage;
        public Vector2 Aim;
        public float Charge;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ServeStage);
            serializer.SerializeValue(ref AttackStage);
            serializer.SerializeValue(ref Aim);
            serializer.SerializeValue(ref Charge);
        }

        public bool Equals(ActionNetworkState other)
        {
            return ServeStage == other.ServeStage && AttackStage == other.AttackStage &&
                   Aim == other.Aim && Mathf.Approximately(Charge, other.Charge);
        }
    }
}
