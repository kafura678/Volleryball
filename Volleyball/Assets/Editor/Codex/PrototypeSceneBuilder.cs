using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace Volleyball.Editor
{
    public static class PrototypeSceneBuilder
    {
        const string ScenePath="Assets/Scenes/Match.unity";
        [MenuItem("Volleyball/Create Phase 1 Scene")]
        public static void Build()
        {
            if(File.Exists(ScenePath)) {Debug.Log("Match scene already exists; preserving it.");return;}
            Directory.CreateDirectory("Assets/ScriptableObjects");Directory.CreateDirectory("Assets/Prefabs");Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.Refresh();
            var settings=ScriptableObject.CreateInstance<PrototypeSettings>();AssetDatabase.CreateAsset(settings,"Assets/ScriptableObjects/PrototypeSettings.asset");
            AddInputs();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("MatchRoot");var match=root.AddComponent<MatchController>();var interaction=root.AddComponent<BallInteractionSystem>();
            var court=new GameObject("Court").AddComponent<CourtDefinition>();court.Settings=settings;
            var sand=Material("Sand",new Color(0.12f,0.19f,0.24f));var teal=Material("PlayerCourt",new Color(0.05f,0.43f,0.43f));var blue=Material("CpuCourt",new Color(0.11f,0.32f,0.5f));var white=Material("Lines",new Color(0.87f,0.92f,0.91f));
            var ground=Box("Ground",new Vector3(0,-0.15f,0),new Vector3(40,0.3f,40),sand,court.transform);ground.AddComponent<CourtSurface>();
            Box("PlayerCourt",new Vector3(0,0.012f,-3),new Vector3(8,0.02f,6),teal,court.transform,false);
            Box("CpuCourt",new Vector3(0,0.012f,3),new Vector3(8,0.02f,6),blue,court.transform,false);
            foreach(float x in new[]{-4f,4f}) Box("Sideline",new Vector3(x,0.03f,0),new Vector3(0.07f,0.02f,12),white,court.transform,false);
            foreach(float z in new[]{-6f,6f}) Box("Baseline",new Vector3(0,0.03f,z),new Vector3(8,0.02f,0.07f),white,court.transform,false);
            var net=new GameObject("Net");net.transform.parent=court.transform;var netCollider=net.AddComponent<BoxCollider>();netCollider.center=new Vector3(0,settings.netHeight/2,0);netCollider.size=new Vector3(8.4f,settings.netHeight,0.12f);
            for(float x=-4;x<=4;x+=0.4f) Box("NetThread",new Vector3(x,1.55f,0),new Vector3(0.015f,1.2f,0.015f),white,net.transform,false);
            for(float y=1;y<=settings.netHeight;y+=0.2f) Box("NetThread",new Vector3(0,y,0),new Vector3(8,0.015f,0.015f),white,net.transform,false);
            Box("NetTape",new Vector3(0,settings.netHeight,0),new Vector3(8.4f,0.08f,0.08f),white,net.transform,false);
            foreach(float x in new[]{-4.25f,4.25f}) Box("Post",new Vector3(x,1.2f,0),new Vector3(0.12f,2.4f,0.12f),white,court.transform);
            var ballObject=GameObject.CreatePrimitive(PrimitiveType.Sphere);ballObject.name="Ball";ballObject.transform.localScale=Vector3.one*0.42f;
            ballObject.GetComponent<Renderer>().sharedMaterial=Material("Ball",new Color(1,0.79f,0.13f));var body=ballObject.AddComponent<Rigidbody>();body.mass=0.27f;
            var ball=ballObject.AddComponent<BallController>();ball.Settings=settings;ballObject.transform.position=new Vector3(0,1.5f,-4.3f);
            var ballPrefab=PrefabUtility.SaveAsPrefabAsset(ballObject,"Assets/Prefabs/Ball.prefab");
            var human=Character("HumanPlayer",TeamId.Human,settings,court,match,interaction,new Color(1,0.42f,0.25f));
            var cpu=Character("CPUPlayer",TeamId.Cpu,settings,court,match,interaction,new Color(0.43f,0.73f,1));
            Physics.IgnoreCollision(ball.GetComponent<Collider>(),human.GetComponent<Collider>());
            Physics.IgnoreCollision(ball.GetComponent<Collider>(),cpu.GetComponent<Collider>());
            // Runtime also configures collision exclusions after loading the scene.
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,13,-16);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=52;camera.backgroundColor=new Color(0.035f,0.065f,0.09f);camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();
            var input=human.gameObject.AddComponent<PlayerInputController>();input.InputAsset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");input.Character=human;input.ViewCamera=camera;
            var light=new GameObject("Directional Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(50,-30,0);light.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(0.6f,0.65f,0.7f);
            match.Settings=settings;match.Court=court;match.Ball=ball;match.Human=human;match.Cpu=cpu;
            interaction.Match=match;interaction.Settings=settings;interaction.Court=court;interaction.Ball=ball;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",false)};
            AssetDatabase.SaveAssets();Debug.Log("Phase 1 Match scene created.");
        }
        [MenuItem("Volleyball/Add CPU and HUD")]
        public static void AddPresentation()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var match=Object.FindFirstObjectByType<MatchController>();
            if(!match.Cpu.GetComponent<CpuController>())
            {
                var cpu=match.Cpu.gameObject.AddComponent<CpuController>();cpu.Character=match.Cpu;cpu.Match=match;cpu.Settings=match.Settings;
            }
            var camera=Camera.main;
            if(!camera.GetComponent<MatchCameraController>()) camera.gameObject.AddComponent<MatchCameraController>().Ball=match.Ball.transform;
            if(!match.Ball.GetComponent<BallPresentation>())
            {
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="BallGroundMarker";Object.DestroyImmediate(marker.GetComponent<Collider>());marker.transform.localScale=new Vector3(0.65f,0.005f,0.65f);marker.GetComponent<Renderer>().sharedMaterial=Material("BallMarker",new Color(1,0.85f,0.25f));
                match.Ball.gameObject.AddComponent<BallPresentation>().GroundMarker=marker.transform;
            }
            if(!Object.FindFirstObjectByType<MatchHud>())
            {
                var canvas=new GameObject("Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=0.5f;
                var hud=canvas.AddComponent<MatchHud>();hud.Match=match;
                var header=Panel("ScorePanel",canvas.transform,new Vector2(0.5f,1),new Vector2(0,-48),new Vector2(540,78),new Color(0.025f,0.055f,0.08f,0.95f));
                hud.Score=Label("Score",header.transform,Vector2.one*0.5f,new Vector2(0,8),new Vector2(530,45),30,"PLAYER  0  :  0  CPU");
                Label("Subtitle",header.transform,Vector2.one*0.5f,new Vector2(0,-23),new Vector2(530,22),13,"OFFLINE 1 vs 1  /  FIRST TO "+match.Settings.winningScore);
                hud.Status=Label("Status",canvas.transform,new Vector2(0.5f,1),new Vector2(0,-108),new Vector2(700,36),22,"");
                var controls=Panel("Controls",canvas.transform,new Vector2(0.5f,0),new Vector2(0,36),new Vector2(1020,52),new Color(0.025f,0.055f,0.08f,0.95f));
                Label("Keys",controls.transform,Vector2.one*0.5f,Vector2.zero,new Vector2(1000,46),18,"MOVE  WASD / LS     RECEIVE  J / A     SET  K / Y     ATTACK  L / X     SERVE  SPACE / B");
                var result=Panel("ResultPanel",canvas.transform,Vector2.one*0.5f,Vector2.zero,new Vector2(440,220),new Color(0.025f,0.055f,0.08f,0.97f));hud.ResultPanel=result;
                hud.Result=Label("Result",result.transform,Vector2.one*0.5f,new Vector2(0,40),new Vector2(410,70),42,"YOU WIN");
                var button=Panel("Restart",result.transform,Vector2.one*0.5f,new Vector2(0,-50),new Vector2(230,55),new Color(0.06f,0.55f,0.5f));hud.RestartButton=button.AddComponent<Button>();
                Label("Text",button.transform,Vector2.one*0.5f,Vector2.zero,new Vector2(220,50),23,"PLAY AGAIN");result.SetActive(false);
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("CPU, camera and HUD added.");
        }
        public static void BuildMacPlayer()
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=new[]{ScenePath},locationPathName="/tmp/VolleyballPrototype.app",
                target=BuildTarget.StandaloneOSX,options=BuildOptions.Development
            });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Prototype player build failed: "+report.summary.result);
            Debug.Log("Prototype player build succeeded.");
        }
        static GameObject Panel(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=position;r.sizeDelta=size;g.GetComponent<Image>().color=color;return g;
        }
        static Text Label(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,int fontSize,string text)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=position;r.sizeDelta=size;
            var t=g.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.text=text;t.raycastTarget=false;return t;
        }
        static VolleyballCharacterController Character(string name,TeamId team,PrototypeSettings settings,CourtDefinition court,MatchController match,BallInteractionSystem interaction,Color color)
        {
            var go=new GameObject(name);var body=go.AddComponent<CharacterController>();body.center=Vector3.up;body.height=2;body.radius=0.35f;body.skinWidth=0.03f;
            var motor=go.AddComponent<CharacterMotor>();motor.Team=team;motor.Settings=settings;motor.Court=court;
            var actions=go.AddComponent<VolleyballActions>();actions.Settings=settings;actions.Match=match;actions.Interaction=interaction;
            var character=go.AddComponent<VolleyballCharacterController>();character.Match=match;
            var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule);visual.name="Visual";visual.transform.SetParent(go.transform);visual.transform.localPosition=Vector3.up;visual.transform.localScale=new Vector3(0.7f,1,0.7f);Object.DestroyImmediate(visual.GetComponent<Collider>());visual.GetComponent<Renderer>().sharedMaterial=Material(name,color);
            go.transform.position=court.Spawn(team);return character;
        }
        static Material Material(string name,Color color)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=color;AssetDatabase.CreateAsset(material,"Assets/Materials/"+name+".mat");return material;
        }
        static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,Transform parent,bool collider=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;if(!collider) Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static void AddInputs()
        {
            string path="Assets/InputSystem_Actions.inputactions";
            var asset=InputActionAsset.FromJson(File.ReadAllText(path));
            if(asset.FindActionMap("Volleyball")!=null) {Object.DestroyImmediate(asset);return;}
            var map=asset.AddActionMap("Volleyball");var move=map.AddAction("Move",InputActionType.Value,expectedControlLayout:"Vector2");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");move.AddBinding("<Gamepad>/leftStick");
            string[] names={"Receive","Set","Attack","Serve"},keys={"j","k","l","space"},buttons={"buttonSouth","buttonNorth","buttonWest","buttonEast"};
            for(int i=0;i<4;i++){var a=map.AddAction(names[i],InputActionType.Button);a.AddBinding("<Keyboard>/"+keys[i]);a.AddBinding("<Gamepad>/"+buttons[i]);}
            File.WriteAllText(path,asset.ToJson());Object.DestroyImmediate(asset);AssetDatabase.ImportAsset(path);
        }
    }
}
