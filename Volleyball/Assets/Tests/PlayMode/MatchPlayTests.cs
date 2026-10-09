using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Volleyball.Tests
{
    public class MatchPlayTests
    {
        MatchController match;
        [UnitySetUp] public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Match");yield return null;
            match=Object.FindFirstObjectByType<MatchController>();
            Assert.NotNull(match);Assert.AreEqual(MatchState.ServePreparation,match.Rules.State);
        }
        [UnityTest] public IEnumerator Cpu_HumanServe_ReturnsBallThroughSharedActions()
        {
            yield return CompleteHumanServe();
            float deadline=Time.time+6;
            while(match.Cpu.Actions.SuccessfulHits==0 && Time.time<deadline) yield return new WaitForFixedUpdate();
            Assert.Greater(match.Cpu.Actions.SuccessfulHits,0);
            Assert.AreEqual(TeamId.Cpu,match.Rules.LastTouch);
            Assert.Less(match.Ball.Velocity.z,0);
        }
        [UnityTest] public IEnumerator Combination_FreeFlight_ReceiveSetJumpAttackAndCpuReturn()
        {
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            yield return CompleteHumanServe();
            float deadline=Time.time+20;
            while(match.Cpu.Actions.SuccessfulHits==0 && Time.time<deadline) yield return new WaitForFixedUpdate();
            Assert.Greater(match.Cpu.Actions.SuccessfulHits,0,"CPU returned initial serve");
            foreach(var action in new[]{ActionType.Receive,ActionType.Set,ActionType.Attack})
            {
                int before=match.Human.Actions.SuccessfulHits;
                while(before==match.Human.Actions.SuccessfulHits && Time.time<deadline && match.Rules.State==MatchState.Playing)
                {
                    Vector3 p=match.Ball.Body.position,v=match.Ball.Velocity;
                    float height=action==ActionType.Receive?1.7f:action==ActionType.Set?2.5f:match.Settings.attackContactHeight+match.Settings.jumpHeight;
                    float time=BallTrajectory.DescendingTime(p,v,match.Settings.gravity,height);
                    if(time>=0)
                    {
                        var target=match.Court.Clamp(BallTrajectory.AtTime(p,v,match.Settings.gravity,time),TeamId.Human);
                        Vector3 delta=target-match.Human.transform.position;delta.y=0;
                        match.Human.Move(Vector3.ClampMagnitude(delta/0.25f,1));
                        if(action==ActionType.Attack)
                        {
                            if(!match.Human.Actions.IsAttackInProgress && time<match.Settings.attackPerfectTime+0.04f)
                                match.Human.AttackStarted(Vector2.zero);
                            if(match.Human.Actions.CurrentAttackStage==AttackStage.Charging &&
                                match.Human.Actions.AttackElapsed>=match.Settings.attackPerfectTime)
                                match.Human.AttackReleased(Vector2.zero);
                        }
                        else if(time<0.15f) match.Human.RequestAction(action);
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.Greater(match.Human.Actions.SuccessfulHits,before,action+" during continuous flight");
            }
            Assert.AreEqual(TeamId.Human,match.Rules.LastTouch);
            Assert.Greater(match.Ball.Velocity.z,0);
            Capture("/tmp/volleyball-gameplay.png");
        }
        [UnityTest] public IEnumerator Combination_DifferentReleaseAims_ReceiveRightSetLeftAttackDuringFreeFlight()
        {
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            yield return CompleteHumanServe();
            float deadline=Time.time+20;
            while(match.Cpu.Actions.SuccessfulHits==0 && Time.time<deadline) yield return new WaitForFixedUpdate();
            Assert.Greater(match.Cpu.Actions.SuccessfulHits,0,"CPU returned initial serve");
            foreach(var action in new[]{ActionType.Receive,ActionType.Set,ActionType.Attack})
            {
                int before=match.Human.Actions.SuccessfulHits;
                while(before==match.Human.Actions.SuccessfulHits && Time.time<deadline && match.Rules.State==MatchState.Playing)
                {
                    Vector3 p=match.Ball.Body.position,v=match.Ball.Velocity;
                    float height=action==ActionType.Receive?1.7f:action==ActionType.Set?2.5f:match.Settings.attackContactHeight+match.Settings.jumpHeight;
                    float time=BallTrajectory.DescendingTime(p,v,match.Settings.gravity,height);
                    if(time>=0)
                    {
                        var target=match.Court.Clamp(BallTrajectory.AtTime(p,v,match.Settings.gravity,time),TeamId.Human);
                        Vector3 delta=target-match.Human.transform.position;delta.y=0;
                        match.Human.Move(Vector3.ClampMagnitude(delta/0.25f,1));
                        if(action==ActionType.Attack)
                        {
                            if(!match.Human.Actions.IsAttackInProgress && time<match.Settings.attackPerfectTime+0.04f)
                                match.Human.AttackStarted(Vector2.zero);
                            if(match.Human.Actions.CurrentAttackStage==AttackStage.Charging &&
                                match.Human.Actions.AttackElapsed>=match.Settings.attackPerfectTime)
                                match.Human.AttackReleased(Vector2.zero);
                        }
                        else
                        {
                            Vector2 aim=action==ActionType.Receive ? Vector2.right : Vector2.left;
                            if(time<.4f && !match.Human.Actions.AimedAction.HasValue)
                                match.Human.AimedActionStarted(action,Vector2.zero);
                            match.Human.AimedActionHeld(action,aim);
                            if(time<.15f) match.Human.AimedActionReleased(action,aim);
                        }
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.Greater(match.Human.Actions.SuccessfulHits,before,action+" during continuous flight");
            }
            Assert.AreEqual(TeamId.Human,match.Rules.LastTouch);
            Assert.Greater(match.Ball.Velocity.z,0);
            Capture("/tmp/polish1-combination.png");
        }
        [UnityTest] public IEnumerator CpuVersusCommandDriver_MultipleRallies_RemainsPlayable()
        {
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            var driver=match.Human.gameObject.AddComponent<CpuController>();driver.Character=match.Human;driver.Match=match;driver.Settings=match.Settings;
            float deadline=Time.time+60;
            int hits=0, rallyStartHits=0, priorPoints=0;
            while(Time.time<deadline && match.Rules.HumanScore+match.Rules.CpuScore<3)
            {
                hits=match.Human.Actions.SuccessfulHits+match.Cpu.Actions.SuccessfulHits;
                int points=match.Rules.HumanScore+match.Rules.CpuScore;
                if(points!=priorPoints) {priorPoints=points;rallyStartHits=hits;driver.enabled=true;}
                if(hits-rallyStartHits>=6) driver.enabled=false;
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(hits,8,"Several actual returns must occur");
            Assert.GreaterOrEqual(match.Rules.HumanScore+match.Rules.CpuScore,3,"Three rallies must resolve after intentionally stopping input");
            Assert.AreEqual(1,Object.FindObjectsByType<BallController>(FindObjectsSortMode.None).Length);
        }
        static void Capture(string path)
        {
            var camera=Camera.main;var canvas=Object.FindFirstObjectByType<Canvas>();
            var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
            var mode=canvas.renderMode;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.5f;
            Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=mode;Object.Destroy(texture);Object.Destroy(target);
        }
        IEnumerator CompleteHumanServe(Vector2? aim=null,float timing=-1)
        {
            match.Human.Aim(aim??Vector2.zero);
            Assert.True(match.Human.RequestAction(ActionType.Serve),"First input should toss");
            Assert.AreEqual(MatchState.ServePreparation,match.Rules.State);
            yield return new WaitForSeconds(timing<0?match.Settings.servePerfectTime:timing);
            match.Human.Aim(aim??Vector2.zero);
            Assert.True(match.Human.RequestAction(ActionType.Serve),"Second input should strike");
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(MatchState.Playing,match.Rules.State);
        }
        void PrepareAttackBall(float releaseTime,Vector3? horizontalOffset=null)
        {
            Vector3 offset=horizontalOffset??Vector3.zero;
            float initialHeight=match.Settings.attackContactHeight+match.Settings.jumpHeight+
                0.5f*match.Settings.gravity*releaseTime*releaseTime;
            match.Ball.Hold(match.Human.transform.position+offset+Vector3.up*initialHeight);
            match.Ball.Launch(Vector3.zero);
        }
        [UnityTest] public IEnumerator Serve_FirstInputTosses_SecondInputStrikesAndEarlyRepeatDoesNot()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            int serial=match.Ball.HitSerial;
            Assert.True(match.Human.RequestAction(ActionType.Serve));
            Assert.AreEqual(serial+1,match.Ball.HitSerial,"Toss launches the ball once");
            Assert.AreEqual(MatchState.ServePreparation,match.Rules.State);
            Assert.AreEqual(ServeStage.Tossed,match.Human.Actions.CurrentServeStage);
            Assert.False(match.Human.RequestAction(ActionType.Serve),"Rapid repeat must not strike");
            Assert.AreEqual(MatchState.ServePreparation,match.Rules.State);
            yield return new WaitForSeconds(match.Settings.servePerfectTime);
            Assert.True(match.Human.RequestAction(ActionType.Serve));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(serial+2,match.Ball.HitSerial);
            Assert.AreEqual(MatchState.Playing,match.Rules.State);
        }
        [UnityTest] public IEnumerator Serve_TossTimeout_ReturnsToSafeRetryState()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            Assert.True(match.Human.RequestAction(ActionType.Serve));
            yield return new WaitForSeconds(match.Settings.serveTossTimeout+0.15f);
            Assert.AreEqual(MatchState.ServePreparation,match.Rules.State);
            Assert.AreEqual(ServeStage.Ready,match.Human.Actions.CurrentServeStage);
            Assert.False(match.Ball.IsLive);
            Assert.True(match.Human.RequestAction(ActionType.Serve),"Serve can be tossed again");
        }
        [UnityTest] public IEnumerator Serve_AimInput_AppliesClampedOpponentTargetAndMarker()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            match.Human.Aim(new Vector2(10,10));
            yield return null;
            var marker=GameObject.Find("ServeTargetMarker");
            Assert.NotNull(marker);
            Assert.True(marker.activeSelf);
            Assert.IsNull(marker.GetComponent<Collider>(),"Target marker must not create a runtime physics collider");
            Assert.NotNull(marker.GetComponent<MeshFilter>().sharedMesh);
            Assert.NotNull(marker.GetComponent<Renderer>().sharedMaterial);
            Vector3 expected=ServeMechanics.TargetForInput(new Vector2(10,10),TeamId.Human,match.Settings);
            Assert.Less(Vector2.Distance(new Vector2(marker.transform.position.x,marker.transform.position.z),new Vector2(expected.x,expected.z)),0.01f);
            Capture("/tmp/volleyball-serve-target.png");
            yield return CompleteHumanServe(new Vector2(10,10));
            Assert.AreEqual(ServeTimingGrade.Perfect,match.Human.Actions.Interaction.LastServeGrade);
            Vector3 target=match.Human.Actions.Interaction.LastServeTarget;
            Assert.Greater(target.x,0);
            Assert.That(target.z,Is.InRange(match.Settings.serveTargetMargin,match.Settings.halfLength-match.Settings.serveTargetMargin));
            yield return null;
            Assert.False(marker.activeSelf);
        }
        [UnityTest] public IEnumerator Serve_KeyboardInput_StartsRallyAndCrossesNet()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            var oldBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var oldBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            Assert.NotNull(match.Human.GetComponent<PlayerInputController>().GameplayMap);
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();
                Assert.True(keyboard.spaceKey.isPressed,"Virtual keyboard event processed");
                Assert.AreEqual(ServeStage.Tossed,match.Human.Actions.CurrentServeStage);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                InputSystem.Update();
                yield return new WaitForSeconds(match.Settings.servePerfectTime);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();
                Assert.True(match.Human.Actions.IsActive,"Second input queues the strike");
                yield return new WaitForSeconds(1.2f);
                Assert.AreEqual(MatchState.Playing,match.Rules.State);Assert.Greater(match.Ball.transform.position.z,0);
            }
            finally {InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=oldBehavior;InputSystem.settings.backgroundBehavior=oldBackground;}
        }
        [UnityTest] public IEnumerator Serve_GamepadInput_StartsRally()
        {
            var old=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var state=new GamepadState {leftStick=Vector2.right};
                InputSystem.QueueStateEvent(pad,state.WithButton(GamepadButton.East));InputSystem.Update();
                Assert.True(pad.buttonEast.isPressed);Assert.AreEqual(ServeStage.Tossed,match.Human.Actions.CurrentServeStage);
                var start=match.Human.transform.position;
                InputSystem.QueueStateEvent(pad,new GamepadState {leftStick=Vector2.right});InputSystem.Update();
                yield return new WaitForSeconds(match.Settings.servePerfectTime);
                InputSystem.QueueStateEvent(pad,state.WithButton(GamepadButton.East));InputSystem.Update();
                yield return new WaitForFixedUpdate();Assert.AreEqual(MatchState.Playing,match.Rules.State);
                Assert.Less(Vector3.Distance(start,match.Human.transform.position),0.05f,"Aim input must not move the server");
            }
            finally {InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=old;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;}
        }
        [UnityTest] public IEnumerator Ball_HighSpeedLowShot_NetPreventsCrossing()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            yield return CompleteHumanServe();
            match.Ball.Hold(new Vector3(0,1.1f,-1));match.Ball.Launch(Vector3.forward*match.Settings.maxBallSpeed);
            yield return new WaitForSeconds(0.12f);Assert.Less(match.Ball.Body.position.z,0.25f,"Ball must collide with solid net");
        }
        [UnityTest] public IEnumerator Ball_UnrecoverableFall_AwardsOpponentOfLastTouch()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            yield return CompleteHumanServe();
            match.Ball.Hold(new Vector3(0,-4,3));match.Ball.Launch(Vector3.down);
            yield return new WaitForSeconds(0.08f);Assert.AreEqual(1,match.Rules.CpuScore);Assert.AreEqual(MatchState.PointFinished,match.Rules.State);
        }
        [UnityTest] public IEnumerator Camera_CourtAndHighBall_AreInFrame()
        {
            yield return new WaitForSeconds(1.5f);
            var camera=Camera.main;
            foreach(float x in new[]{-4f,4f}) foreach(float z in new[]{-6f,6f})
            {
                var viewport=camera.WorldToViewportPoint(new Vector3(x,0,z));
                Assert.That(viewport.x,Is.InRange(0.02f,0.98f));Assert.That(viewport.y,Is.InRange(0.08f,0.93f));
            }
            var high=camera.WorldToViewportPoint(new Vector3(0,7.3f,0));Assert.That(high.y,Is.InRange(0.1f,0.93f));
        }
        [UnityTest] public IEnumerator ReceiveSetAttack_SameAthlete_ThreeSuccessfulContacts()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(0.35f);
            int initial=match.Human.Actions.SuccessfulHits;
            foreach(var action in new[]{ActionType.Receive,ActionType.Set,ActionType.Attack})
            {
                var p=match.Human.transform.position;
                if(action==ActionType.Attack)
                {
                    PrepareAttackBall(match.Settings.attackPerfectTime);
                    Assert.True(match.Human.AttackStarted(Vector2.zero),action.ToString());
                    yield return new WaitForSeconds(match.Settings.attackPerfectTime);
                    Assert.True(match.Human.AttackReleased(Vector2.zero));
                    yield return new WaitForFixedUpdate();
                }
                else
                {
                    match.Ball.Hold(p+Vector3.up*(action==ActionType.Set?2.3f:1.2f));
                    match.Ball.Launch(Vector3.zero);
                    Assert.True(match.Human.RequestAction(action),action.ToString());
                    yield return new WaitForSeconds(0.08f);
                }
                initial++;Assert.AreEqual(initial,match.Human.Actions.SuccessfulHits,action.ToString());
                yield return new WaitForSeconds(0.35f);
            }
            Assert.Greater(match.Ball.Velocity.z,0);Assert.Greater(match.Human.Motor.JumpOffset,0);
            Assert.GreaterOrEqual(match.Rules.TouchCount,4);
        }
        [UnityTest] public IEnumerator Attack_StartHoldRelease_JumpsChargesAndHitsOnce()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            Vector3 movementStart=match.Human.transform.position;
            match.Human.Move(Vector3.right);
            yield return new WaitForFixedUpdate();
            Assert.Greater(match.Human.transform.position.x,movementStart.x,"Normal movement works before Attack");
            match.Human.Move(Vector3.zero);
            PrepareAttackBall(match.Settings.attackPerfectTime);
            int serial=match.Ball.HitSerial;
            Vector3 takeoffPosition=match.Human.transform.position;
            Assert.True(match.Human.AttackStarted(new Vector2(0.8f,0.5f)));
            Assert.True(match.Human.Motor.IsJumping);
            Assert.AreEqual(AttackStage.Charging,match.Human.Actions.CurrentAttackStage);
            match.Human.Move(Vector3.right);
            Assert.That(match.Human.Motor.MoveDirection.magnitude,Is.EqualTo(0).Within(0.001f));
            float firstCharge=match.Human.Actions.ChargeAmount;
            yield return new WaitForSeconds(0.2f);
            Vector2 updatedAim=new Vector2(-0.6f,0.7f);
            match.Human.AttackHeld(updatedAim);
            Assert.Greater(match.Human.Actions.ChargeAmount,firstCharge);
            Assert.Less(Vector2.Distance(match.Human.Actions.AttackAimInput,updatedAim),0.001f);
            Assert.Less(Vector2.Distance(
                new Vector2(match.Human.transform.position.x,match.Human.transform.position.z),
                new Vector2(takeoffPosition.x,takeoffPosition.z)),0.01f,"Attack jump must not move horizontally");
            Assert.AreEqual(serial,match.Ball.HitSerial,"Holding must not hit the ball");
            yield return new WaitForSeconds(match.Settings.attackPerfectTime-0.2f);
            Assert.True(match.Human.AttackReleased(updatedAim));
            Assert.AreEqual(AttackStage.Released,match.Human.Actions.CurrentAttackStage);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(serial+1,match.Ball.HitSerial);
            Assert.AreEqual(AttackTimingGrade.Perfect,match.Human.Actions.Interaction.LastAttackGrade);
            Assert.False(match.Human.AttackReleased(Vector2.zero),"One release cannot hit twice");
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(serial+1,match.Ball.HitSerial);
            Assert.False(match.Human.Actions.IsAttackInProgress);
            match.Human.Move(Vector3.right);
            Assert.That(match.Human.Motor.MoveDirection.magnitude,Is.EqualTo(0).Within(0.001f),"Movement stays locked until landing");
            while(match.Human.Motor.IsJumping) yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Less(Vector2.Distance(
                new Vector2(match.Human.transform.position.x,match.Human.transform.position.z),
                new Vector2(takeoffPosition.x,takeoffPosition.z)),0.01f,"Horizontal position stays fixed through landing");
            match.Human.Move(Vector3.right);
            Assert.That(match.Human.Motor.MoveDirection.magnitude,Is.EqualTo(1).Within(0.001f));
        }
        [UnityTest] public IEnumerator Attack_AimInput_ChangesClampedTargetAndShowsChargeFeedback()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            PrepareAttackBall(match.Settings.attackPerfectTime);
            Vector2 aim=new Vector2(10,10);
            Assert.True(match.Human.AttackStarted(aim));
            yield return new WaitForSeconds(0.16f);
            match.Human.AttackHeld(aim);
            var marker=GameObject.Find("AttackTargetMarker");
            var charge=GameObject.Find("AttackChargeIndicator");
            Assert.NotNull(marker);Assert.True(marker.activeSelf);
            Assert.NotNull(charge);Assert.True(charge.activeSelf);
            Assert.IsNull(marker.GetComponent<Collider>());
            Assert.IsNull(charge.GetComponent<Collider>());
            Assert.NotNull(marker.GetComponent<Renderer>().sharedMaterial);
            Assert.NotNull(charge.GetComponent<Renderer>().sharedMaterial);
            Vector3 expected=AttackMechanics.TargetForInput(aim,TeamId.Human,match.Settings);
            Assert.Less(Vector2.Distance(new Vector2(marker.transform.position.x,marker.transform.position.z),new Vector2(expected.x,expected.z)),0.01f);
            Capture("/tmp/volleyball-attack-target.png");
            yield return new WaitForSeconds(match.Settings.attackPerfectTime-0.16f);
            Assert.True(match.Human.AttackReleased(aim));
            yield return new WaitForFixedUpdate();
            Vector3 target=match.Human.Actions.Interaction.LastAttackTarget;
            Assert.Greater(target.x,0);
            Assert.That(target.z,Is.InRange(match.Settings.attackTargetMargin,match.Settings.halfLength-match.Settings.attackTargetMargin));
            yield return null;
            Assert.False(marker.activeSelf);Assert.False(charge.activeSelf);
        }
        [UnityTest] public IEnumerator Attack_OutOfRangeRelease_MissesWithoutBallContact()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            PrepareAttackBall(match.Settings.attackPerfectTime,Vector3.right*(match.Settings.hitRadius+0.5f));
            int serial=match.Ball.HitSerial;
            Assert.True(match.Human.AttackStarted(Vector2.zero));
            yield return new WaitForSeconds(match.Settings.attackPerfectTime);
            Assert.True(match.Human.AttackReleased(Vector2.zero));
            Assert.AreEqual(AttackTimingGrade.Miss,match.Human.Actions.PendingAttackGrade);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(serial,match.Ball.HitSerial);
            Assert.False(match.Human.Actions.IsAttackInProgress);
            match.Human.Move(Vector3.right);
            Assert.That(match.Human.Motor.MoveDirection.magnitude,Is.EqualTo(0).Within(0.001f),"Miss remains movement-locked in the air");
            while(match.Human.Motor.IsJumping) yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            match.Human.Move(Vector3.right);
            Assert.That(match.Human.Motor.MoveDirection.magnitude,Is.EqualTo(1).Within(0.001f),"Movement returns after a missed Attack lands");
        }
        [UnityTest] public IEnumerator Attack_GamepadPressHoldRelease_ProducesSingleSpike()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            PrepareAttackBall(match.Settings.attackPerfectTime);
            int serial=match.Ball.HitSerial;
            var old=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var held=new GamepadState {leftStick=Vector2.right}.WithButton(GamepadButton.West);
                InputSystem.QueueStateEvent(pad,held);InputSystem.Update();
                Assert.AreEqual(AttackStage.Charging,match.Human.Actions.CurrentAttackStage);
                yield return new WaitForSeconds(0.2f);
                float charge=match.Human.Actions.ChargeAmount;
                Assert.Greater(charge,0);Assert.AreEqual(serial,match.Ball.HitSerial);
                yield return new WaitForSeconds(match.Settings.attackPerfectTime-0.2f);
                InputSystem.QueueStateEvent(pad,new GamepadState {leftStick=Vector2.right});InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(serial+1,match.Ball.HitSerial);
                Assert.Greater(match.Human.Actions.Interaction.LastAttackTarget.x,0);
            }
            finally {InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=old;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;}
        }
        [UnityTest] public IEnumerator Action_OutOfRange_DoesNotHit()
        {
            yield return CompleteHumanServe();yield return new WaitForSeconds(0.35f);
            match.Ball.Hold(new Vector3(3,1.5f,-1));match.Ball.Launch(Vector3.zero);
            int serial=match.Ball.HitSerial;match.Human.RequestAction(ActionType.Receive);
            yield return new WaitForSeconds(0.15f);Assert.AreEqual(serial,match.Ball.HitSerial);
        }
        [UnityTest] public IEnumerator Landing_MatchWinAndRestart_ResetsAllState()
        {
            match.Cpu.GetComponent<CpuController>().enabled=false;
            for(int i=0;i<match.Settings.winningScore;i++)
            {
                yield return CompleteHumanServe();
                match.Ball.Hold(new Vector3(0,0.6f,3));match.Ball.Launch(Vector3.down);
                yield return new WaitForSeconds(0.3f);
                Assert.AreEqual(i+1,match.Rules.HumanScore);
                if(i<match.Settings.winningScore-1) yield return new WaitForSeconds(match.Settings.pointDelay+0.1f);
            }
            Assert.AreEqual(MatchState.MatchFinished,match.Rules.State);
            var hud=Object.FindFirstObjectByType<MatchHud>();Assert.True(hud.ResultPanel.activeSelf);Assert.True(hud.Score.text.Contains(match.Settings.winningScore.ToString()));
            Assert.False(match.Human.RequestAction(ActionType.Attack));
            hud.RestartButton.onClick.Invoke();yield return null;
            Assert.False(hud.ResultPanel.activeSelf);
            Assert.AreEqual(0,match.Rules.HumanScore);Assert.AreEqual(0,match.Rules.TouchCount);Assert.AreEqual(MatchState.ServePreparation,match.Rules.State);Assert.False(match.Ball.IsLive);Assert.Less(match.Ball.Velocity.magnitude,0.01f);
        }
    }
}
