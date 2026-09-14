using NUnit.Framework;
using UnityEngine;
namespace Volleyball.Tests
{
    public class RulesTests
    {
        [Test] public void Serve_WrongTeam_Rejects() { var r = new MatchRules(5); r.Prepare(); Assert.False(r.Serve(TeamId.Cpu)); Assert.True(r.Serve(TeamId.Human)); }
        [Test] public void Award_DuplicateContact_ScoresOnce() { var r = Started(); Assert.True(r.Award(TeamId.Cpu)); Assert.False(r.Award(TeamId.Cpu)); Assert.AreEqual(1,r.CpuScore); Assert.AreEqual(TeamId.Cpu,r.Server); }
        [Test] public void Touch_SameAthleteThreeTimes_AllowsCombination() { var r = Started(); for(int i=0;i<3;i++) Assert.True(r.Touch(TeamId.Human)); Assert.AreEqual(4,r.TouchCount); }
        [Test] public void Award_TargetReached_EndsMatch() { var r=new MatchRules(1); r.Prepare(); r.Serve(TeamId.Human); r.Award(TeamId.Human); Assert.AreEqual(MatchState.MatchFinished,r.State); Assert.False(r.Prepare()); r.Reset(); Assert.AreEqual(0,r.HumanScore); Assert.True(r.Prepare()); }
        [TestCase(0,-3,TeamId.Cpu)] [TestCase(4,6,TeamId.Human)] [TestCase(4.1f,3,TeamId.Cpu)]
        public void Landing_InsideLineAndOutside_AwardsCorrectSide(float x,float z,TeamId expected) { Assert.AreEqual(expected,Started().LandingWinner(new Vector3(x,0,z),4,6)); }
        [Test] public void ToTarget_AfterFlight_ReachesTarget() { var a=new Vector3(1,3,-4); var b=new Vector3(-1,0,4); var v=BallTrajectory.ToTarget(a,b,9.81f,1.7f); Assert.Less(Vector3.Distance(b,BallTrajectory.AtTime(a,v,9.81f,1.7f)),0.001f); }
        [Test] public void Prediction_UnreachableHeight_ReturnsInvalid() { Assert.Less(BallTrajectory.DescendingTime(Vector3.zero,Vector3.zero,9.81f,5),0); }
        [Test] public void ServeTiming_EarlyGoodAndPerfect_ReturnExpectedGrades()
        {
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();
            Assert.AreEqual(ServeTimingGrade.None,ServeMechanics.EvaluateTiming(0.05f,3,settings));
            Assert.AreEqual(ServeTimingGrade.Good,ServeMechanics.EvaluateTiming(settings.servePerfectTime+settings.servePerfectWindow+0.05f,3,settings));
            Assert.AreEqual(ServeTimingGrade.Perfect,ServeMechanics.EvaluateTiming(settings.servePerfectTime,3,settings));
            Object.DestroyImmediate(settings);
        }
        [Test] public void ServeTarget_DirectionAndExtremeInput_StaysInsideOpponentCourt()
        {
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();
            Vector3 left=ServeMechanics.TargetForInput(Vector2.left,TeamId.Human,settings);
            Vector3 right=ServeMechanics.TargetForInput(Vector2.right,TeamId.Human,settings);
            Vector3 deep=ServeMechanics.TargetForInput(Vector2.up*50,TeamId.Human,settings);
            Vector3 shortTarget=ServeMechanics.TargetForInput(Vector2.down*50,TeamId.Human,settings);
            Assert.Less(left.x,right.x);
            Assert.Greater(deep.z,shortTarget.z);
            foreach(Vector3 target in new[]{left,right,deep,shortTarget})
            {
                Assert.That(Mathf.Abs(target.x),Is.LessThanOrEqualTo(settings.halfWidth-settings.serveTargetMargin));
                Assert.That(target.z,Is.InRange(settings.serveTargetMargin,settings.halfLength-settings.serveTargetMargin));
            }
            Object.DestroyImmediate(settings);
        }
        [Test] public void ServeVelocity_PerfectAndGood_ProduceDifferentTrajectories()
        {
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();
            Vector3 start=new Vector3(0,3.3f,-5);
            Vector3 target=ServeMechanics.TargetForInput(new Vector2(0.7f,0.4f),TeamId.Human,settings);
            Vector3 good=ServeMechanics.Velocity(start,target,ServeTimingGrade.Good,settings);
            Vector3 perfect=ServeMechanics.Velocity(start,target,ServeTimingGrade.Perfect,settings);
            Assert.AreNotEqual(good,perfect);
            Assert.Greater(new Vector2(perfect.x,perfect.z).magnitude,new Vector2(good.x,good.z).magnitude);
            Object.DestroyImmediate(settings);
        }
        [Test] public void AttackCharge_ElapsedTime_ClampsAtOne()
        {
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();
            Assert.AreEqual(0,AttackMechanics.Charge(0,settings));
            Assert.That(AttackMechanics.Charge(settings.attackMaxChargeTime*0.5f,settings),Is.EqualTo(0.5f).Within(0.001f));
            Assert.AreEqual(1,AttackMechanics.Charge(settings.attackMaxChargeTime*10,settings));
            Object.DestroyImmediate(settings);
        }
        [Test] public void AttackTiming_ReleaseConditions_ReturnMissGoodAndPerfect()
        {
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();
            Vector3 perfectBall=new Vector3(0,settings.attackContactHeight,0);
            Assert.AreEqual(AttackTimingGrade.Miss,AttackMechanics.EvaluateTiming(0.02f,0,perfectBall,true,settings));
            Assert.AreEqual(AttackTimingGrade.Good,AttackMechanics.EvaluateTiming(settings.attackPerfectTime+settings.attackPerfectWindow+0.05f,settings.jumpHeight-0.6f,perfectBall,true,settings));
            Assert.AreEqual(AttackTimingGrade.Perfect,AttackMechanics.EvaluateTiming(settings.attackPerfectTime,settings.jumpHeight,perfectBall,true,settings));
            Object.DestroyImmediate(settings);
        }
        [Test] public void AttackPower_ChargeAndTiming_IncreaseStrength()
        {
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();
            float weak=AttackMechanics.Power(0.25f,AttackTimingGrade.Good,settings);
            float charged=AttackMechanics.Power(0.9f,AttackTimingGrade.Good,settings);
            float perfect=AttackMechanics.Power(0.9f,AttackTimingGrade.Perfect,settings);
            Assert.Less(weak,charged);
            Assert.Less(charged,perfect);
            Vector3 start=new Vector3(0,3.4f,-2);
            Vector3 target=AttackMechanics.TargetForInput(Vector2.zero,TeamId.Human,settings);
            Vector3 weakVelocity=AttackMechanics.Velocity(start,target,0.25f,AttackTimingGrade.Good,settings);
            Vector3 goodVelocity=AttackMechanics.Velocity(start,target,0.9f,AttackTimingGrade.Good,settings);
            Vector3 perfectVelocity=AttackMechanics.Velocity(start,target,0.9f,AttackTimingGrade.Perfect,settings);
            Assert.Greater(new Vector2(goodVelocity.x,goodVelocity.z).magnitude,new Vector2(weakVelocity.x,weakVelocity.z).magnitude);
            Assert.Greater(new Vector2(perfectVelocity.x,perfectVelocity.z).magnitude,new Vector2(goodVelocity.x,goodVelocity.z).magnitude);
            Assert.LessOrEqual(perfectVelocity.magnitude,settings.maxBallSpeed);
            Object.DestroyImmediate(settings);
        }
        [Test] public void AttackTarget_AimAndExtremeInput_StaysInsideOpponentCourt()
        {
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();
            Vector3 left=AttackMechanics.TargetForInput(Vector2.left,TeamId.Human,settings);
            Vector3 right=AttackMechanics.TargetForInput(Vector2.right,TeamId.Human,settings);
            Vector3 deep=AttackMechanics.TargetForInput(Vector2.up*50,TeamId.Human,settings);
            Vector3 shortTarget=AttackMechanics.TargetForInput(Vector2.down*50,TeamId.Human,settings);
            Assert.Less(left.x,right.x);
            Assert.Greater(deep.z,shortTarget.z);
            foreach(Vector3 target in new[]{left,right,deep,shortTarget})
            {
                Assert.That(Mathf.Abs(target.x),Is.LessThanOrEqualTo(settings.halfWidth-settings.attackTargetMargin));
                Assert.That(target.z,Is.InRange(settings.attackTargetMargin,settings.halfLength-settings.attackTargetMargin));
            }
            Object.DestroyImmediate(settings);
        }
        [Test] public void MobileJoystick_DeadZoneAndLargeInput_ReturnZeroAndClampedVector()
        {
            Assert.AreEqual(Vector2.zero,MobileInputMath.ApplyRadialDeadZone(new Vector2(0.1f,0),0.15f));
            Vector2 value=MobileInputMath.ApplyRadialDeadZone(new Vector2(4,3),0.15f);
            Assert.That(value.magnitude,Is.EqualTo(1).Within(0.001f));
            Assert.Greater(value.x,0);Assert.Greater(value.y,0);
        }
        [Test] public void MobileJoystick_LocalCenterEdgesAndDiagonal_ReturnExpectedDirections()
        {
            var rect=new Rect(0,0,220,220);
            Assert.AreEqual(Vector2.zero,MobileVirtualJoystick.NormalizedInput(rect,rect.center));
            Assert.That(MobileVirtualJoystick.NormalizedInput(rect,new Vector2(rect.xMax,rect.center.y)).x,Is.EqualTo(1).Within(0.001f));
            Assert.That(MobileVirtualJoystick.NormalizedInput(rect,new Vector2(rect.xMin,rect.center.y)).x,Is.EqualTo(-1).Within(0.001f));
            Assert.That(MobileVirtualJoystick.NormalizedInput(rect,new Vector2(rect.center.x,rect.yMax)).y,Is.EqualTo(1).Within(0.001f));
            Assert.That(MobileVirtualJoystick.NormalizedInput(rect,new Vector2(rect.center.x,rect.yMin)).y,Is.EqualTo(-1).Within(0.001f));
            Vector2 diagonal=MobileVirtualJoystick.NormalizedInput(rect,new Vector2(rect.xMax,rect.yMax));
            Assert.Greater(diagonal.x,0);Assert.Greater(diagonal.y,0);
            Assert.That(diagonal.magnitude,Is.EqualTo(1).Within(0.001f));
        }
        [Test] public void MobileSwipe_DeadZoneDirectionAndScale_ReturnExpectedAim()
        {
            Vector2 start=new Vector2(100,100);
            Assert.AreEqual(Vector2.zero,MobileInputMath.SwipeAim(start,start+new Vector2(10,0),24,140));
            Vector2 right=MobileInputMath.SwipeAim(start,start+new Vector2(200,0),24,140);
            Vector2 deep=MobileInputMath.SwipeAim(start,start+new Vector2(0,200),24,140);
            Assert.Less(Vector2.Distance(right,Vector2.right),0.001f);
            Assert.Less(Vector2.Distance(deep,Vector2.up),0.001f);
            Assert.LessOrEqual(right.magnitude,1);
        }
        [Test] public void SafeArea_LandscapeNotch_NormalizesAndClampsAnchors()
        {
            Rect normalized=SafeAreaFitter.Normalize(new Rect(80,20,2440,1060),new Vector2Int(2532,1170));
            Assert.That(normalized.xMin,Is.EqualTo(80f/2532f).Within(0.0001f));
            Assert.That(normalized.yMin,Is.EqualTo(20f/1170f).Within(0.0001f));
            Assert.That(normalized.xMax,Is.EqualTo(2520f/2532f).Within(0.0001f));
            Assert.That(normalized.yMax,Is.EqualTo(1080f/1170f).Within(0.0001f));

            Rect invalid=SafeAreaFitter.Normalize(new Rect(-50,-20,2000,1000),Vector2Int.zero);
            Assert.AreEqual(Vector2.zero,invalid.min);
            Assert.AreEqual(Vector2.one,invalid.max);
        }
        [Test] public void Clamp_EitherSide_KeepsAthleteInside() { var g=new GameObject(); var c=g.AddComponent<CourtDefinition>(); c.Settings=ScriptableObject.CreateInstance<PrototypeSettings>(); foreach(TeamId t in new[]{TeamId.Human,TeamId.Cpu}) {var p=c.Clamp(new Vector3(100,0,100),t); Assert.LessOrEqual(Mathf.Abs(p.x),3.6f); Assert.True(c.OnSide(p,t));} Object.DestroyImmediate(c.Settings); Object.DestroyImmediate(g); }
        static MatchRules Started() {var r=new MatchRules(5);r.Prepare();r.Serve(TeamId.Human);return r;}
    }
}
