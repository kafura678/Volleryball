using UnityEngine;

namespace Volleyball
{
    public sealed class AttackTargetMarker : MonoBehaviour
    {
        public MatchController Match;

        GameObject targetMarker;
        GameObject chargeIndicator;
        Material targetMaterial;
        Material chargeMaterial;

        void Start()
        {
            targetMarker = MarkerVisualFactory.CreateDisc("AttackTargetMarker");
            targetMarker.transform.localScale = new Vector3(0.48f, 1f, 0.48f);
            targetMaterial = MarkerVisualFactory.CreateMaterial(Match, new Color(0.1f, 0.95f, 1f));
            MarkerVisualFactory.SetMaterial(targetMarker, targetMaterial);

            chargeIndicator = MarkerVisualFactory.CreateBox("AttackChargeIndicator");
            chargeMaterial = MarkerVisualFactory.CreateMaterial(Match, new Color(0.1f, 0.95f, 1f));
            MarkerVisualFactory.SetMaterial(chargeIndicator, chargeMaterial);

            SetVisible(false);
        }

        void LateUpdate()
        {
            if (!targetMarker || !chargeIndicator || !Match || !Match.Human) return;
            VolleyballActions actions = Match.Human.Actions;
            bool visible = Match.Rules != null &&
                Match.Rules.State == MatchState.Playing &&
                actions.CurrentAttackStage == AttackStage.Charging;
            SetVisible(visible);
            if (!visible) return;

            float charge = actions.ChargeAmount;
            targetMarker.transform.position = AttackMechanics.TargetForInput(
                actions.AttackAimInput,
                TeamId.Human,
                Match.Settings) + Vector3.up * 0.025f;
            float pulse = 0.85f + charge * 0.35f;
            targetMarker.transform.localScale = new Vector3(0.48f * pulse, 0.008f, 0.48f * pulse);
            Color color = Color.Lerp(new Color(0.1f, 0.95f, 1f), new Color(1f, 0.35f, 0.08f), charge);
            SetMaterialColor(targetMaterial, color);

            float height = Mathf.Lerp(0.08f, 1.1f, charge);
            chargeIndicator.transform.localScale = new Vector3(0.12f, height, 0.12f);
            chargeIndicator.transform.position = Match.Human.transform.position + Vector3.up * (2.2f + height * 0.5f);
            SetMaterialColor(chargeMaterial, color);
        }

        void SetVisible(bool visible)
        {
            targetMarker.SetActive(visible);
            chargeIndicator.SetActive(visible);
        }

        void OnDestroy()
        {
            MarkerVisualFactory.DestroyVisual(targetMarker);
            MarkerVisualFactory.DestroyVisual(chargeIndicator);
            if (targetMaterial) Destroy(targetMaterial);
            if (chargeMaterial) Destroy(chargeMaterial);
        }

        static void SetMaterialColor(Material material, Color color)
        {
            if (!material) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
