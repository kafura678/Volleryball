using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Volleyball.Tests
{
    public class FoundationPlayTests
    {
        GameObject root; PrototypeSettings settings;
        [SetUp] public void Setup() {root=new GameObject("TestRoot");settings=ScriptableObject.CreateInstance<PrototypeSettings>();}
        [TearDown] public void Cleanup() {Object.DestroyImmediate(root); Object.DestroyImmediate(settings);}
        GameObject Child(string name) {var g=new GameObject(name);g.transform.parent=root.transform;return g;}
        [UnityTest] public IEnumerator Move_BothTeams_RemainsInsideCourt()
        {
            var court=Child("Court").AddComponent<CourtDefinition>();court.Settings=settings;
            foreach(var team in new[]{TeamId.Human,TeamId.Cpu})
            {
                var g=Child("Athlete");var cc=g.AddComponent<CharacterController>();cc.center=Vector3.up;cc.height=2;
                var motor=g.AddComponent<CharacterMotor>(); motor.Court=court;motor.Settings=settings;motor.Team=team;
                motor.ResetPosition(court.Spawn(team));motor.MoveDirection=new Vector3(1,0,CourtDefinition.Direction(team));
                for(int i=0;i<90;i++) yield return new WaitForFixedUpdate();
                Assert.True(court.OnSide(g.transform.position,team));Assert.LessOrEqual(Mathf.Abs(g.transform.position.x),settings.halfWidth);
            }
        }
        [UnityTest] public IEnumerator Jump_Automatic_ReturnsToGround()
        {
            var court=Child("Court").AddComponent<CourtDefinition>();court.Settings=settings;
            var g=Child("Athlete");g.AddComponent<CharacterController>();var m=g.AddComponent<CharacterMotor>();m.Settings=settings;m.Court=court;
            m.ResetPosition(court.Spawn(TeamId.Human));Assert.True(m.BeginJump());Assert.False(m.BeginJump());
            yield return new WaitForSeconds(settings.jumpDuration/2);Assert.Greater(m.JumpOffset,settings.jumpHeight*0.85f);
            yield return new WaitForSeconds(settings.jumpDuration);Assert.False(m.IsJumping);Assert.Less(Mathf.Abs(g.transform.position.y),0.05f);
        }
        [UnityTest] public IEnumerator Ball_GroundContact_NotifiesOnce()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.parent=root.transform;floor.transform.position=new Vector3(0,-0.5f,0);floor.transform.localScale=new Vector3(20,1,20);floor.AddComponent<CourtSurface>();
            var ball=Child("Ball").AddComponent<BallController>();ball.Settings=settings;ball.GetComponent<SphereCollider>().radius=0.2f;
            int contacts=0;ball.Landed+=_=>contacts++;ball.Hold(new Vector3(0,2,0));ball.Launch(Vector3.zero);
            yield return new WaitForSeconds(1.1f);Assert.AreEqual(1,contacts);Assert.False(ball.IsLive);
        }
    }
}
