using NUnit.Framework;
using UnityEngine;

namespace Volleyball.Tests
{
    public class ControlAimTests
    {
        [TestCase(ActionType.Receive, TeamId.Human)]
        [TestCase(ActionType.Receive, TeamId.Cpu)]
        [TestCase(ActionType.Set, TeamId.Human)]
        [TestCase(ActionType.Set, TeamId.Cpu)]
        public void Target_DirectionsAndOversizedInput_UsesOwnCourtAndPreservesCenter(ActionType action, TeamId team)
        {
            var settings = ScriptableObject.CreateInstance<PrototypeSettings>();
            var obj = new GameObject("Court");
            var court = obj.AddComponent<CourtDefinition>();
            court.Settings = settings;
            float d = CourtDefinition.Direction(team);
            Vector3 actor = new Vector3(0, 0, -d * 3.5f);
            Vector3 center = ControlAimMechanics.TargetForInput(actor, Vector2.zero, team, action, court);
            Assert.AreEqual(actor.z + d * (action == ActionType.Set ? 1f : 0.35f), center.z);
            Assert.AreEqual(action == ActionType.Set ? 2.4f : 1.6f, center.y);
            Vector3 right = Target(Vector2.right), left = Target(Vector2.left);
            Assert.Greater(right.x * d, 0); Assert.Less(left.x * d, 0);
            Assert.Less(Target(Vector2.up).z * -d, center.z * -d);
            Assert.Greater(Target(Vector2.down).z * -d, center.z * -d);
            foreach (Vector2 aim in new[] { Vector2.one * 100, -Vector2.one * 100 })
            {
                Vector3 target = Target(aim);
                Assert.That(target.x, Is.InRange(-settings.halfWidth + .4f, settings.halfWidth - .4f));
                Assert.That(target.z * -d, Is.InRange(1.4f, settings.halfLength - .4f));
            }
            Object.DestroyImmediate(obj); Object.DestroyImmediate(settings);
            Vector3 Target(Vector2 screenAim) => ControlAimMechanics.TargetForInput(actor,
                MobileInputMath.ScreenAimToCourtAim(screenAim, team), team, action, court);
        }

        [TestCase(ActionType.Receive)]
        [TestCase(ActionType.Set)]
        public void Phase_ReceiveAndSet_AcceptsStartedReleasedAndLegacyCpuAction(ActionType action)
        {
            Assert.True(OnlineGameplayRules.IsActionPhaseAllowed(action, NetworkActionPhase.Started));
            Assert.True(OnlineGameplayRules.IsActionPhaseAllowed(action, NetworkActionPhase.Released));
            Assert.True(OnlineGameplayRules.IsActionPhaseAllowed(action, NetworkActionPhase.Performed));
            Assert.False(ControlAimMechanics.IsFinite(new Vector2(float.NaN, 0)));
            Assert.False(ControlAimMechanics.IsFinite(new Vector2(0, float.PositiveInfinity)));
            Assert.False(OnlineGameplayRules.IsOwnedRequest(1, 2));
        }
    }
}
