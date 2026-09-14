using UnityEngine;
namespace Volleyball
{
    public sealed class MatchCameraController : MonoBehaviour
    {
        public Transform Ball;
        Vector3 offset=new Vector3(10,12,-12);
        void LateUpdate()
        {
            if(!Ball) return;
            Vector3 center=new Vector3(Mathf.Clamp(Ball.position.x*0.12f,-0.6f,0.6f),1,Mathf.Clamp(Ball.position.z*0.05f,-0.3f,0.3f));
            transform.position=Vector3.Lerp(transform.position,offset+center-Vector3.up,1-Mathf.Exp(-3*Time.deltaTime));
            transform.rotation=Quaternion.LookRotation(new Vector3(0,1,0)-offset);
        }
    }
}
