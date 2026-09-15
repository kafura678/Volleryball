using Unity.Netcode.Components;

namespace Volleyball
{
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return true;
        }
    }
}
