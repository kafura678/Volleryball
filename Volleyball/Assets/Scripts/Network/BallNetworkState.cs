using System;
using Unity.Netcode;
using UnityEngine;

namespace Volleyball
{
    public struct BallNetworkState : INetworkSerializable, IEquatable<BallNetworkState>
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public bool IsLive;
        public bool IsKinematic;
        public int HitSerial;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref IsLive);
            serializer.SerializeValue(ref IsKinematic);
            serializer.SerializeValue(ref HitSerial);
        }

        public bool Equals(BallNetworkState other)
        {
            return Position == other.Position && Rotation == other.Rotation && Velocity == other.Velocity &&
                   IsLive == other.IsLive && IsKinematic == other.IsKinematic && HitSerial == other.HitSerial;
        }
    }
}
