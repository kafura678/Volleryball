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
            targetMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetMarker.name = "AttackTargetMarker";
            targetMarker.transform.localScale = new Vector3(0.48f, 0.008f, 0.48f);
            Destroy(targetMarker.GetComponent<Collider>());
            targetMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            targetMaterial.color = new Color(0.1f, 0.95f, 1f);
            targetMarker.GetComponent<Renderer>().material = targetMaterial;

            chargeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chargeIndicator.name = "AttackChargeIndicator";
            Destroy(chargeIndicator.GetComponent<Collider>());
            chargeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            chargeIndicator.GetComponent<Renderer>().material = chargeMaterial;

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
            targetMaterial.color = Color.Lerp(new Color(0.1f, 0.95f, 1f), new Color(1f, 0.35f, 0.08f), charge);

            float height = Mathf.Lerp(0.08f, 1.1f, charge);
            chargeIndicator.transform.localScale = new Vector3(0.12f, height, 0.12f);
            chargeIndicator.transform.position = Match.Human.transform.position + Vector3.up * (2.2f + height * 0.5f);
            chargeMaterial.color = targetMaterial.color;
        }

        void SetVisible(bool visible)
        {
            targetMarker.SetActive(visible);
            chargeIndicator.SetActive(visible);
        }

        void OnDestroy()
        {
            if (targetMarker) Destroy(targetMarker);
            if (chargeIndicator) Destroy(chargeIndicator);
            if (targetMaterial) Destroy(targetMaterial);
            if (chargeMaterial) Destroy(chargeMaterial);
        }
    }
}
