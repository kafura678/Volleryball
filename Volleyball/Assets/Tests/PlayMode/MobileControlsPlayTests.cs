using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Volleyball.Tests
{
    public class MobileControlsPlayTests
    {
        MatchController match;
        MobileInputController mobile;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;
            match=Object.FindFirstObjectByType<MatchController>();
            mobile=Object.FindFirstObjectByType<MobileInputController>();
            Assert.NotNull(match);Assert.NotNull(mobile);
            match.Cpu.GetComponent<CpuController>().enabled=false;
        }

        [UnityTest]
        public IEnumerator MobileUi_JoystickVisualCoordinates_MapCenterEdgesAndScaledSafeArea()
        {
            MobileVirtualJoystick joystick=Object.FindFirstObjectByType<MobileVirtualJoystick>();
            CanvasScaler scaler=GameObject.Find("MobileControlsCanvas").GetComponent<CanvasScaler>();
            RectTransform safeArea=GameObject.Find("MobileSafeAreaRoot").GetComponent<RectTransform>();
            Assert.NotNull(joystick);Assert.NotNull(scaler);Assert.NotNull(safeArea);
            Canvas.ForceUpdateCanvases();

            Rect rect=joystick.Background.rect;
            Vector2 center=JoystickScreenPoint(joystick,rect.center);
            PointerEventData pointer=Pointer(41,center);
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            Assert.That(mobile.MoveInput.magnitude,Is.LessThan(0.001f));
            Assert.That(joystick.Knob.anchoredPosition.magnitude,Is.LessThan(0.001f));

            AssertJoystickDirection(joystick,pointer,new Vector2(rect.xMax,rect.center.y),Vector2.right);
            AssertJoystickDirection(joystick,pointer,new Vector2(rect.xMin,rect.center.y),Vector2.left);
            AssertJoystickDirection(joystick,pointer,new Vector2(rect.center.x,rect.yMax),Vector2.up);
            AssertJoystickDirection(joystick,pointer,new Vector2(rect.center.x,rect.yMin),Vector2.down);
            pointer.position=JoystickScreenPoint(joystick,new Vector2(rect.xMax,rect.yMax));
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.dragHandler);
            Assert.Greater(mobile.MoveInput.x,0);Assert.Greater(mobile.MoveInput.y,0);

            safeArea.anchorMin=new Vector2(0.08f,0.06f);
            safeArea.anchorMax=new Vector2(0.92f,0.94f);
            scaler.referenceResolution=new Vector2(1920,1080);
            Canvas.ForceUpdateCanvases();
            pointer.position=JoystickScreenPoint(joystick,joystick.Background.rect.center);
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.dragHandler);
            Assert.That(mobile.MoveInput.magnitude,Is.LessThan(0.001f));
            Assert.That(joystick.Knob.anchoredPosition.magnitude,Is.LessThan(0.001f));

            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.AreEqual(Vector2.zero,mobile.MoveInput);
        }

        [UnityTest]
        public IEnumerator MobileUi_JoystickPointer_MovesAndReturnsToZero()
        {
            GameObject mobileCanvas = GameObject.Find("MobileControlsCanvas");
            GameObject safeAreaRoot = GameObject.Find("MobileSafeAreaRoot");
            Assert.NotNull(mobileCanvas);
            Assert.NotNull(safeAreaRoot);
            Assert.AreEqual(mobileCanvas.transform,safeAreaRoot.transform.parent);
            CanvasScaler scaler=mobileCanvas.GetComponent<CanvasScaler>();
            Assert.NotNull(scaler);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize,scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1280,720),scaler.referenceResolution);
            Assert.That(scaler.matchWidthOrHeight,Is.EqualTo(0.5f).Within(0.001f));
            Assert.NotNull(safeAreaRoot.GetComponent<SafeAreaFitter>());
            Assert.NotNull(EventSystem.current);
            Assert.NotNull(EventSystem.current.GetComponent<InputSystemUIInputModule>());
            Assert.IsNull(EventSystem.current.GetComponent<StandaloneInputModule>());
            yield return CompleteHumanServe();
            yield return null;

            var joystick=Object.FindFirstObjectByType<MobileVirtualJoystick>();
            Assert.NotNull(joystick);
            Vector2 center=JoystickScreenPoint(joystick,joystick.Background.rect.center);
            Vector3 start=match.Human.transform.position;
            PointerEventData pointer=Pointer(1,center);
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            pointer.position=JoystickScreenPoint(joystick,new Vector2(
                joystick.Background.rect.xMax,joystick.Background.rect.center.y));
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.dragHandler);
            yield return new WaitForFixedUpdate();
            Assert.Greater(mobile.MoveInput.x,0);
            Assert.Greater(match.Human.Motor.MoveDirection.magnitude,0);
            Assert.Greater(match.Human.transform.position.x,start.x);

            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.AreEqual(Vector2.zero,mobile.MoveInput);
            Assert.Less(match.Human.Motor.MoveDirection.magnitude,0.001f);
        }

        [UnityTest]
        public IEnumerator MobileUi_ReceiveAndSetButtons_InvokeExistingActions()
        {
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            yield return null;
            int hits=match.Human.Actions.SuccessfulHits;

            MobileTapButton receive=FindTap(ActionType.Receive);
            PrepareBall(1.2f);
            Tap(receive,10);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(++hits,match.Human.Actions.SuccessfulHits);

            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            MobileTapButton set=FindTap(ActionType.Set);
            PrepareBall(2.3f);
            Tap(set,11);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(++hits,match.Human.Actions.SuccessfulHits);
        }

        [UnityTest]
        public IEnumerator MobileUi_AttackMousePointer_HoldsSwipesAndReleasesExistingAttack()
        {
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            PrepareAttackBall(match.Settings.attackPerfectTime);
            MobileSwipeArea attack=FindSwipe(MobileSwipeAction.Attack);
            Vector2 start=new Vector2(900,160);
            PointerEventData pointer=Pointer(-1,start);
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            Assert.AreEqual(AttackStage.Charging,match.Human.Actions.CurrentAttackStage);
            yield return new WaitForSeconds(0.14f);

            pointer.position=start+new Vector2(8,8);
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.dragHandler);
            Assert.AreEqual(Vector2.zero,match.Human.Actions.AttackAimInput,"Small swipe stays inside dead zone");
            pointer.position=start+new Vector2(150,110);
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.dragHandler);
            Assert.Greater(match.Human.Actions.AttackAimInput.x,0);
            Assert.Greater(match.Human.Actions.AttackAimInput.y,0);
            yield return null;
            Vector3 markerTarget=GameObject.Find("AttackTargetMarker").transform.position;
            CaptureMobileUi("/tmp/volleyball-mobile-controls.png");
            yield return new WaitForSeconds(match.Settings.attackPerfectTime-0.14f);

            int serial=match.Ball.HitSerial;
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(serial+1,match.Ball.HitSerial);
            Assert.AreEqual(AttackTimingGrade.Perfect,match.Human.Actions.Interaction.LastAttackGrade);
            Vector3 target=match.Human.Actions.Interaction.LastAttackTarget;
            Assert.That(Vector3.Distance(new Vector3(markerTarget.x,0.25f,markerTarget.z),target),Is.LessThan(0.01f),
                "Attack marker must use the same target as the final hit");
            Assert.Greater(target.x,0);Assert.Greater(target.z,3);
            Assert.That(target.x,Is.InRange(-match.Settings.halfWidth,match.Settings.halfWidth));
            Assert.That(target.z,Is.InRange(match.Settings.attackTargetMargin,match.Settings.halfLength-match.Settings.attackTargetMargin));
        }

        [UnityTest]
        public IEnumerator MobileUi_AttackReleaseOutsidePerfectWindow_ProducesGoodTiming()
        {
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            float releaseTime=match.Settings.attackPerfectTime+match.Settings.attackPerfectWindow+0.05f;
            PrepareAttackBall(releaseTime);
            MobileSwipeArea attack=FindSwipe(MobileSwipeAction.Attack);
            Vector2 start=new Vector2(900,160);
            PointerEventData pointer=Pointer(31,start);
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(releaseTime);
            pointer.position=start+Vector2.left*100;
            int serial=match.Ball.HitSerial;
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(serial+1,match.Ball.HitSerial);
            Assert.AreEqual(AttackTimingGrade.Good,match.Human.Actions.Interaction.LastAttackGrade);
        }

        [UnityTest]
        public IEnumerator MobileUi_AttackEarlyRelease_ProducesMissWithoutContact()
        {
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+0.05f);
            PrepareAttackBall(0.05f);
            MobileSwipeArea attack=FindSwipe(MobileSwipeAction.Attack);
            Vector2 start=new Vector2(900,160);
            PointerEventData pointer=Pointer(32,start);
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(0.05f);
            int serial=match.Ball.HitSerial;
            ExecuteEvents.Execute(attack.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(serial,match.Ball.HitSerial);
            Assert.True(match.LastMessage.Contains("MISS"));
        }

        [UnityTest]
        public IEnumerator MobileUi_ServeAndJoystickPointers_DoNotCompete()
        {
            var joystick=Object.FindFirstObjectByType<MobileVirtualJoystick>();
            MobileSwipeArea serve=FindSwipe(MobileSwipeAction.Serve);
            Vector2 joystickCenter=RectTransformUtility.WorldToScreenPoint(null,joystick.Background.position);
            PointerEventData movePointer=Pointer(21,joystickCenter);
            ExecuteEvents.Execute(joystick.gameObject,movePointer,ExecuteEvents.pointerDownHandler);
            Assert.True(mobile.IsPointerClaimed(21));
            Assert.False(mobile.BeginMove(23));
            Assert.False(mobile.IsPointerClaimed(23),"Rejected second Joystick pointer must not remain owned");
            Assert.False(mobile.TapAction(24,ActionType.Receive));
            Assert.False(mobile.IsPointerClaimed(24),"Rejected action pointer must be released immediately");

            PointerEventData conflicting=Pointer(21,new Vector2(900,160));
            ExecuteEvents.Execute(serve.gameObject,conflicting,ExecuteEvents.pointerDownHandler);
            Assert.AreEqual(ServeStage.Ready,match.Human.Actions.CurrentServeStage,"Joystick pointer cannot also start Serve");

            Vector2 start=new Vector2(900,160);
            PointerEventData servePointer=Pointer(22,start);
            ExecuteEvents.Execute(serve.gameObject,servePointer,ExecuteEvents.pointerDownHandler);
            Assert.AreEqual(ServeStage.Tossed,match.Human.Actions.CurrentServeStage);
            Assert.True(mobile.IsPointerClaimed(21));Assert.True(mobile.IsPointerClaimed(22));
            servePointer.position=start+new Vector2(170,100);
            ExecuteEvents.Execute(serve.gameObject,servePointer,ExecuteEvents.dragHandler);
            yield return new WaitForSeconds(match.Settings.servePerfectTime);
            ExecuteEvents.Execute(serve.gameObject,servePointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(joystick.gameObject,movePointer,ExecuteEvents.pointerUpHandler);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(MatchState.Playing,match.Rules.State);
            Assert.AreEqual(ServeTimingGrade.Perfect,match.Human.Actions.Interaction.LastServeGrade);
            Assert.Greater(match.Human.Actions.Interaction.LastServeTarget.x,0);
            Assert.False(mobile.IsPointerClaimed(21));Assert.False(mobile.IsPointerClaimed(22));
        }


        [UnityTest]
        public IEnumerator MobileUi_TeamBSwipeUsesCourtRelativeRightAndSameServeTargetAsMarker()
        {
            match.Human.Motor.Team=TeamId.Cpu;
            match.Rules.Synchronize(new MatchNetworkState
            {
                State=MatchState.ServePreparation,
                Server=TeamId.Cpu,
                LastTouch=TeamId.Human,
                TargetScore=match.Settings.winningScore
            });
            match.Human.ResetForRally(match.Court.Spawn(TeamId.Cpu));
            match.Ball.Hold(match.Human.transform.position+Vector3.up*1.25f);
            yield return null;

            MobileSwipeArea serve=FindSwipe(MobileSwipeAction.Serve);
            Vector2 start=new Vector2(900,160);
            PointerEventData pointer=Pointer(52,start);
            ExecuteEvents.Execute(serve.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            pointer.position=start+Vector2.right*180;
            ExecuteEvents.Execute(serve.gameObject,pointer,ExecuteEvents.dragHandler);
            Assert.Less(match.Human.AimInput.x,0,"Team B visual-right swipe maps to world -X");
            yield return null;
            Vector3 markerTarget=GameObject.Find("ServeTargetMarker").transform.position;
            yield return new WaitForSeconds(match.Settings.servePerfectTime);
            ExecuteEvents.Execute(serve.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            yield return new WaitForFixedUpdate();

            Vector3 target=match.Human.Actions.Interaction.LastServeTarget;
            Assert.That(Vector3.Distance(new Vector3(markerTarget.x,0.25f,markerTarget.z),target),Is.LessThan(0.01f),
                "Serve marker must use the same target as the final hit");
            Assert.Less(target.x,0);
            Assert.Less(target.z,0);
        }

        [UnityTest]
        public IEnumerator MobileUi_ServeAndAttackReleaseCoordinates_ReachCommandSinkForEitherTeam()
        {
            var sink=new RecordingCommandSink();
            match.Human.CommandSink=sink;
            Vector2 start=new Vector2(900,160);
            Vector2[] deltas={Vector2.zero,Vector2.left*180,Vector2.right*180,Vector2.up*180,Vector2.down*180};
            foreach(TeamId team in new[]{TeamId.Human,TeamId.Cpu})
            {
                match.Human.Motor.Team=team;
                match.Rules.Synchronize(new MatchNetworkState
                {
                    State=MatchState.ServePreparation,Server=team,TargetScore=match.Settings.winningScore
                });
                foreach(Vector2 delta in deltas)
                {
                    Assert.True(mobile.BeginSwipe(61,MobileSwipeAction.Serve,start));
                    Assert.True(mobile.EndSwipe(61,start+delta));
                    Vector2 expected=MobileInputMath.GameplayAim(start,start+delta,
                        match.Settings.mobileSwipeDeadZone,match.Settings.mobileSwipeFullScale,team);
                    Assert.AreEqual(expected,sink.LastAim,"Serve must use final Release coordinates");
                    Assert.True(mobile.BeginSwipe(62,MobileSwipeAction.Attack,start));
                    Assert.True(mobile.EndSwipe(62,start+delta));
                    Assert.AreEqual(expected,sink.LastReleaseAim,"Attack must use the same final Release conversion");
                }
            }
            match.Human.CommandSink=null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReceiveSet_SwipeReleaseBothTeams_UsesFinalAimOnceAndDoesNotCharge()
        {
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+.05f);
            match.Human.GetComponent<PlayerInputController>().enabled=false;
            foreach (TeamId team in new[] { TeamId.Human, TeamId.Cpu })
            foreach (ActionType action in new[] { ActionType.Receive, ActionType.Set })
            foreach (Vector2 direction in new[] { Vector2.zero, Vector2.left, Vector2.right, Vector2.up, Vector2.down })
            {
                match.Rules.Synchronize(new MatchNetworkState { State=MatchState.Playing, Server=team, TargetScore=5 });
                match.Human.Motor.Team=team;
                match.Human.ResetForRally(new Vector3(0,0,-CourtDefinition.Direction(team)*3.5f));
                PrepareBall(action==ActionType.Receive ? 1.2f : 2.3f);
                MobileTapButton button=FindTap(action);
                var pointer=new PointerEventData(EventSystem.current) { pointerId=95, position=new Vector2(300,300) };
                int serial=match.Ball.HitSerial;
                button.OnPointerDown(pointer);
                Assert.AreEqual(action,match.Human.Actions.AimedAction);
                Assert.False(match.Human.Actions.IsActive);
                Assert.Zero(match.Human.CurrentAttackCharge);
                pointer.position+=direction*160;
                button.OnDrag(pointer);
                Assert.AreEqual(serial,match.Ball.HitSerial,"Holding cannot hit");
                Vector2 aim=mobile.CurrentAim;
                Vector3 expected=ControlAimMechanics.TargetForInput(match.Human.transform.position,aim,team,action,match.Court);
                button.OnPointerUp(pointer);
                button.OnPointerUp(pointer);
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(serial+1,match.Ball.HitSerial);
                Assert.That(Vector3.Distance(expected,match.Human.Actions.Interaction.LastAimedActionTarget),Is.LessThan(.001f));
                Assert.IsNull(match.Human.Actions.AimedAction);
                Assert.False(mobile.HasActiveSwipe);
                yield return new WaitForSeconds(match.Settings.hitCooldown+.05f);
            }
        }

        [UnityTest]
        public IEnumerator ReceiveSet_ClientCommandSink_ReceivesStartedHeldAndFinalReleaseAim()
        {
            var sink=new RecordingCommandSink(); match.Human.CommandSink=sink;
            foreach (TeamId team in new[] { TeamId.Human, TeamId.Cpu })
            foreach (MobileSwipeAction action in new[] { MobileSwipeAction.Receive, MobileSwipeAction.Set })
            {
                match.Human.Motor.Team=team;
                Assert.True(mobile.BeginSwipe(96,action,Vector2.zero));
                mobile.HoldSwipe(96,new Vector2(160,0));
                Vector2 expected=MobileInputMath.GameplayAim(Vector2.zero,new Vector2(160,0),
                    match.Settings.mobileSwipeDeadZone,match.Settings.mobileSwipeFullScale,team);
                Assert.AreEqual(expected,sink.LastAim);
                Assert.True(mobile.EndSwipe(96,new Vector2(160,0)));
                Assert.AreEqual(expected,sink.LastReleaseAim);
            }
            match.Human.CommandSink=null; yield return null;
        }

        [UnityTest]
        public IEnumerator ReceiveSet_GamepadPressHoldRelease_PreservesMoveAndHitsOnRelease()
        {
            yield return CompleteHumanServe();
            yield return new WaitForSeconds(match.Settings.hitCooldown+.05f);
            var old=InputSystem.settings.backgroundBehavior;
            var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                foreach (ActionType action in new[] { ActionType.Receive, ActionType.Set })
                {
                    match.Rules.Synchronize(new MatchNetworkState { State=MatchState.Playing, TargetScore=5 });
                    match.Human.ResetForRally(new Vector3(0,0,-3.5f));
                    PrepareBall(action==ActionType.Receive ? 1.2f : 2.3f);
                    int serial=match.Ball.HitSerial;
                    var held=new GamepadState {leftStick=Vector2.right}.WithButton(action==ActionType.Receive ? GamepadButton.South : GamepadButton.North);
                    InputSystem.QueueStateEvent(pad,held); InputSystem.Update();
                    Assert.AreEqual(action,match.Human.Actions.AimedAction);
                    yield return new WaitForSeconds(.1f);
                    Assert.AreEqual(serial,match.Ball.HitSerial);
                    Assert.Greater(match.Human.Motor.MoveDirection.sqrMagnitude,0);
                    PrepareBall(action==ActionType.Receive ? 1.2f : 2.3f);
                    InputSystem.QueueStateEvent(pad,new GamepadState {leftStick=Vector2.right}); InputSystem.Update();
                    yield return new WaitForFixedUpdate();
                    Assert.AreEqual(serial+2,match.Ball.HitSerial); // PrepareBall also launches once.
                    Assert.Greater(match.Human.Actions.Interaction.LastAimedActionTarget.x,match.Human.transform.position.x);
                    InputSystem.QueueStateEvent(pad,new GamepadState()); InputSystem.Update();
                    yield return new WaitForSeconds(match.Settings.hitCooldown+.05f);
                }
            }
            finally { InputSystem.RemoveDevice(pad); InputSystem.settings.backgroundBehavior=old; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor; }
        }

        sealed class RecordingCommandSink : IVolleyballCommandSink
        {
            public ServeStage CurrentServeStage => ServeStage.Ready;
            public AttackStage CurrentAttackStage => AttackStage.Charging;
            public float CurrentAttackCharge => 0f;
            public Vector2 LastAim;
            public Vector2 LastReleaseAim;
            public void Move(Vector3 direction) { }
            public void Aim(Vector2 input) => LastAim=input;
            public bool RequestAction(ActionType action) => true;
            public bool AimedActionStarted(ActionType action, Vector2 aim) => true;
            public void AimedActionHeld(ActionType action, Vector2 aim) => LastAim=aim;
            public bool AimedActionReleased(ActionType action, Vector2 aim) { LastReleaseAim=aim; return true; }
            public bool AttackStarted(Vector2 aimDirection) => true;
            public void AttackHeld(Vector2 aimDirection) => LastAim=aimDirection;
            public bool AttackReleased(Vector2 aimDirection) { LastReleaseAim=aimDirection;return true; }
        }

        IEnumerator CompleteHumanServe()
        {
            Assert.True(match.Human.RequestAction(ActionType.Serve));
            yield return new WaitForSeconds(match.Settings.servePerfectTime);
            Assert.True(match.Human.RequestAction(ActionType.Serve));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(MatchState.Playing,match.Rules.State);
        }

        void PrepareBall(float height)
        {
            match.Ball.Hold(match.Human.transform.position+Vector3.up*height);
            match.Ball.Launch(Vector3.zero);
        }

        void PrepareAttackBall(float releaseTime)
        {
            float initialHeight=match.Settings.attackContactHeight+match.Settings.jumpHeight+
                0.5f*match.Settings.gravity*releaseTime*releaseTime;
            PrepareBall(initialHeight);
        }

        static PointerEventData Pointer(int pointerId,Vector2 position)
        {
            return new PointerEventData(EventSystem.current){pointerId=pointerId,position=position};
        }

        void AssertJoystickDirection(MobileVirtualJoystick joystick,PointerEventData pointer,
            Vector2 localPoint,Vector2 expected)
        {
            pointer.position=JoystickScreenPoint(joystick,localPoint);
            ExecuteEvents.Execute(joystick.gameObject,pointer,ExecuteEvents.dragHandler);
            Assert.That(mobile.MoveInput.x,Is.EqualTo(expected.x).Within(0.02f));
            Assert.That(mobile.MoveInput.y,Is.EqualTo(expected.y).Within(0.02f));
        }

        static Vector2 JoystickScreenPoint(MobileVirtualJoystick joystick,Vector2 localPoint)
        {
            Canvas canvas=joystick.Background.GetComponentInParent<Canvas>();
            Camera eventCamera=canvas && canvas.renderMode!=RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector3 worldPoint=joystick.Background.TransformPoint(localPoint);
            return RectTransformUtility.WorldToScreenPoint(eventCamera,worldPoint);
        }

        static void Tap(MobileTapButton button,int pointerId)
        {
            PointerEventData pointer=Pointer(pointerId,Vector2.zero);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
        }

        static MobileTapButton FindTap(ActionType action)
        {
            foreach(MobileTapButton button in Object.FindObjectsByType<MobileTapButton>(FindObjectsSortMode.None))
                if(button.Action==action) return button;
            Assert.Fail("Mobile tap button not found: "+action);return null;
        }

        static MobileSwipeArea FindSwipe(MobileSwipeAction action)
        {
            foreach(MobileSwipeArea area in Object.FindObjectsByType<MobileSwipeArea>(FindObjectsSortMode.None))
                if(area.Action==action) return area;
            Assert.Fail("Mobile swipe area not found: "+action);return null;
        }

        static void CaptureMobileUi(string path)
        {
            Camera camera=Camera.main;
            Canvas canvas=GameObject.Find("MobileControlsCanvas").GetComponent<Canvas>();
            RenderMode oldMode=canvas.renderMode;Camera oldCamera=canvas.worldCamera;
            var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.5f;
            Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;
            Object.Destroy(texture);Object.Destroy(target);
        }
    }
}
