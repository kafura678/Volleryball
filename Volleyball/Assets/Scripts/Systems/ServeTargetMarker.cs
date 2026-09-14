using UnityEngine;

namespace Volleyball
{
    public sealed class ServeTargetMarker : MonoBehaviour
    {
        public MatchController Match;
        GameObject marker;
        Material markerMaterial;

        void Start()
        {
            marker = MarkerVisualFactory.CreateDisc("ServeTargetMarker");
            marker.transform.localScale = new Vector3(0.55f, 1f, 0.55f);
            markerMaterial = MarkerVisualFactory.CreateMaterial(Match, new Color(1f, 0.18f, 0.65f));
            MarkerVisualFactory.SetMaterial(marker, markerMaterial);
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
            MarkerVisualFactory.DestroyVisual(marker);
            if (markerMaterial) Destroy(markerMaterial);
        }
    }
}
