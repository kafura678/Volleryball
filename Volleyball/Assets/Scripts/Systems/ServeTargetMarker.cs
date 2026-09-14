using UnityEngine;

namespace Volleyball
{
    public sealed class ServeTargetMarker : MonoBehaviour
    {
        public MatchController Match;
        GameObject marker;

        void Start()
        {
            marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name="ServeTargetMarker";
            marker.transform.localScale=new Vector3(0.55f,0.008f,0.55f);
            Destroy(marker.GetComponent<Collider>());
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color=new Color(1f,0.18f,0.65f);
            marker.GetComponent<Renderer>().material=material;
            marker.SetActive(false);
        }

        void LateUpdate()
        {
            if(!marker || !Match || Match.Rules==null) return;
            bool visible=Match.Rules.State==MatchState.ServePreparation && Match.Rules.Server==TeamId.Human;
            marker.SetActive(visible);
            if(visible)
            {
                marker.transform.position=ServeMechanics.TargetForInput(
                    Match.Human.AimInput,TeamId.Human,Match.Settings)+Vector3.up*0.02f;
            }
        }

        void OnDestroy()
        {
            if(marker) Destroy(marker);
        }
    }
}
