using UnityEngine;
namespace Volleyball
{
    public sealed class BallPresentation : MonoBehaviour
    {
        public Transform GroundMarker;
        void LateUpdate()
        {
            if(GroundMarker) GroundMarker.position=new Vector3(transform.position.x,0.06f,transform.position.z);
        }
    }
}
