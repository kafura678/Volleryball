using System;
using Unity.Collections;
using Unity.Netcode;

namespace Volleyball
{
    public struct MatchNetworkState : INetworkSerializable, IEquatable<MatchNetworkState>
    {
        public MatchState State;
        public TeamId Server;
        public TeamId LastTouch;
        public int TouchCount;
        public int TeamAScore;
        public int TeamBScore;
        public int TargetScore;
        public bool HasWinner;
        public TeamId Winner;
        public FixedString128Bytes Message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref State);
            serializer.SerializeValue(ref Server);
            serializer.SerializeValue(ref LastTouch);
            serializer.SerializeValue(ref TouchCount);
            serializer.SerializeValue(ref TeamAScore);
            serializer.SerializeValue(ref TeamBScore);
            serializer.SerializeValue(ref TargetScore);
            serializer.SerializeValue(ref HasWinner);
            serializer.SerializeValue(ref Winner);
            serializer.SerializeValue(ref Message);
        }

        public bool Equals(MatchNetworkState other)
        {
            return State == other.State && Server == other.Server && LastTouch == other.LastTouch &&
                   TouchCount == other.TouchCount && TeamAScore == other.TeamAScore &&
                   TeamBScore == other.TeamBScore && TargetScore == other.TargetScore &&
                   HasWinner == other.HasWinner && Winner == other.Winner && Message.Equals(other.Message);
        }
    }
}
