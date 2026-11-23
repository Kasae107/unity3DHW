using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CourseGame.Editor
{
    [InitializeOnLoad]
    public static class CourseBuilder
    {
        public const string Root = "Assets/CourseGame/Generated";
        public const string ScenePath = Root + "/LastSignal_v02.unity";
        const string CommandPath = "CourseGame-command.txt";
        const string Evidence = "CourseGameVerification";
        static string lastCommand = "";
        static bool verifyPending;
        static bool endurancePending;
        static CourseBuilder()
        {
            lastCommand=SessionState.GetString("CourseLastCommand","");
            Directory.CreateDirectory(Evidence);
            EditorApplication.update += Poll;
            Application.logMessageReceived += OnLog;
        }
        static void OnLog(string message,string stack,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
            {
                File.AppendAllText(Evidence+"/Runtime-errors-v01.txt",DateTime.Now.ToString("s")+" "+message+"\n"+stack+"\n");
                if(EditorApplication.isPlaying)File.AppendAllText(Evidence+"/PlayMode-errors-v01.txt",DateTime.Now.ToString("s")+" "+message+"\n"+stack+"\n");
            }
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(EditorApplication.isPlaying && CourseSession.Instance && CourseSession.Instance.State==CourseSession.Phase.Welcome && SessionState.GetBool("CourseRewardDeathCheck",false))
            {
                SessionState.EraseBool("CourseRewardDeathCheck");
                var s=CourseSession.Instance;s.VerificationRun=true;int stopEvents=0,gameOverEvents=0;
                s.survivalTimer.TimerStopped+=_=>stopEvents++;GameManager.Instance.GameOverStarted+=()=>gameOverEvents++;
                s.StartRun();s.player.AddExperience(5);
                bool opened=s.rewards.IsChoosing && s.rewards.PendingRewards==1;
                s.player.TakeDamage(999);GameManager.Instance.BeginGameOver();
                bool dead=s.State==CourseSession.Phase.Defeat && GameManager.Instance.IsGameOver && s.player!=null && Time.timeScale==0;
                File.AppendAllText(Evidence+"/PlayMode-results-v02.txt",(dead?"PASS ":"FAIL ")+"Player death enters defeat and preserves references\n");
                File.AppendAllText(Evidence+"/PlayMode-results-v02.txt",(!s.survivalTimer.IsRunning && stopEvents==1?"PASS ":"FAIL ")+"Death stops survival timer with one event\n");
                bool cleared=opened && !s.rewards.IsChoosing && s.rewards.PendingRewards==0 && !s.rewards.Panel.activeSelf && gameOverEvents==1 && !s.rewards.Choose(PlayerUpgradeType.Health) && Time.timeScale==0;
                File.AppendAllText(Evidence+"/PlayMode-results-v02.txt",(cleared?"PASS ":"FAIL ")+"Death during upgrade selection clears choices, blocks late reward and preserves result pause\nCOMPLETE\n");
                s.Restart();
            }
            if(EditorApplication.isPlaying && CourseSession.Instance && CourseSession.Instance.State==CourseSession.Phase.Welcome && SessionState.GetString("CourseRestartCheck","")!="")
            {
                string totals=SessionState.GetString("CourseRestartCheck","");SessionState.EraseString("CourseRestartCheck");
                var s=CourseSession.Instance;bool clean=s.Elapsed==0 && s.Kills==0 && s.Level==1 && s.player.CurrentHealth==100 && GameManager.Instance.EnemyCount==0;
                clean=clean && s.player.CurrentExperience==0 && s.player.RequiredExperience==5;
                clean=clean && s.survivalTimer.ElapsedSeconds==0 && !s.survivalTimer.IsRunning;
                File.AppendAllText(Evidence+"/PlayMode-results-v02.txt",(clean?"PASS ":"FAIL ")+"Restart reloads clean welcome state and EXP\nPrior totals="+totals+"\n");
                s.VerificationRun=true;s.StartRun();s.player.AddExperience(5);bool opened=s.rewards.IsChoosing;
                s.Finish(true);
                bool cleared=opened && s.State==CourseSession.Phase.Victory && !s.rewards.Panel.activeSelf && s.rewards.PendingRewards==0 && !s.rewards.IsChoosing && Time.timeScale==0;
                File.AppendAllText(Evidence+"/PlayMode-results-v02.txt",(cleared?"PASS ":"FAIL ")+"Victory closes pending upgrade overlay without resuming battle\n");
                SessionState.SetBool("CourseRewardDeathCheck",true);
                s.Restart();
            }
            if(verifyPending && EditorApplication.isPlaying && CourseSession.Instance)
            {
                verifyPending=false;
                CourseSession.Instance.gameObject.AddComponent<CourseVerification>();
            }
            if(endurancePending && EditorApplication.isPlaying && CourseSession.Instance)
            {endurancePending=false;CourseSession.Instance.gameObject.AddComponent<CourseEndurance>();}
            if(!File.Exists(CommandPath)) return;
            string raw;
            try { raw=File.ReadAllText(CommandPath).Trim(); } catch(IOException) { return; }
            if(raw.Length==0) return;
            if(raw==lastCommand) return;
            lastCommand=raw;
            SessionState.SetString("CourseLastCommand",raw);
            string cmd=raw.Split(' ').Last();
            try
            {
                if(cmd=="build") Build();
                else if(cmd=="apply1019") ApplyOctober19();
                else if(cmd=="apply1109") ApplyNovember9();
                else if(cmd=="apply1123") ApplyNovember23();
                else if(cmd=="open") Open();
                else if(cmd=="play") { Open(); EditorApplication.isPlaying=true; }
                else if(cmd=="verify") { verifyPending=true; if(!EditorApplication.isPlaying) { Open(); EditorApplication.isPlaying=true; } }
                else if(cmd=="endurance") { endurancePending=true; if(!EditorApplication.isPlaying) { Open(); EditorApplication.isPlaying=true; } }
                else if(cmd=="stop") EditorApplication.isPlaying=false;
                else if(cmd=="start" && CourseSession.Instance) CourseSession.Instance.StartRun();
                else if(cmd=="pause" && CourseSession.Instance) CourseSession.Instance.Pause();
                else if(cmd=="resume" && CourseSession.Instance) CourseSession.Instance.Resume();
                else if(cmd=="restart" && CourseSession.Instance) CourseSession.Instance.Restart();
                else if(cmd=="reset-test-score") {PlayerPrefs.SetInt("CourseGame.BestKills",0);PlayerPrefs.Save();}
                else if(cmd=="snapshot") Snapshot();
                else if(cmd=="inspect")
                {
                    var lines=new System.Collections.Generic.List<string>();
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Model/characterMedium.fbx");
                    foreach(var t in model.GetComponentsInChildren<Transform>())lines.Add("MODEL "+t.name+" pos="+t.localPosition+" scale="+t.localScale);
                    foreach(string n in new[]{"idle","run"})
                    {
                        var clip=AssetDatabase.LoadAllAssetsAtPath($"Assets/CourseGame/SourceAnimations/{n}.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
                        foreach(var b in AnimationUtility.GetCurveBindings(clip))
                            if(b.propertyName.Contains("Scale") || b.path=="" || b.path=="Root")
                            {var c=AnimationUtility.GetEditorCurve(clip,b);lines.Add(n+" CURVE "+b.path+" "+b.propertyName+" "+c.Evaluate(0));}
                    }
                    if(CourseSession.Instance)foreach(var t in CourseSession.Instance.player.GetComponentsInChildren<Transform>())lines.Add("PLAY "+t.name+" pos="+t.localPosition+" scale="+t.localScale);
                    File.WriteAllLines(Evidence+"/Animation-inspection.txt",lines);
                }
                else if(cmd=="capture") ScreenCapture.CaptureScreenshot(Evidence+"/Game-preview-v01.png");
                else if(cmd=="show")
                {
                    var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                    EditorWindow.GetWindow(t).Focus();
                }
                else throw new ArgumentException("Unknown command or game is not ready: "+cmd);
                File.WriteAllText(Evidence+"/Command-status.txt",DateTime.Now.ToString("s")+" "+cmd+" OK");
            }
            catch(Exception e) { File.WriteAllText(Evidence+"/Command-status.txt",cmd+" FAILED\n"+e); Debug.LogException(e); }
        }
        [MenuItem("Course Game/Open Last Signal")]
        public static void Open()
        {
            if(EditorApplication.isPlaying) return;
            if(SceneManager.GetActiveScene().path==ScenePath) return;
            if(SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save your current scene before opening Last Signal.");
            EditorSceneManager.OpenScene(ScenePath);
        }
        static void Snapshot()
        {
            var s=CourseSession.Instance;
            string status="Scene="+SceneManager.GetActiveScene().path+"\nPlaying="+EditorApplication.isPlaying;
            if(s)
            {
                status+=$"\nPhase={s.State}\nTime={s.Elapsed:F2}\nTimerRunning={s.survivalTimer.IsRunning}\nLevel={s.Level}\nEXP={s.player.CurrentExperience}/{s.player.RequiredExperience}\nHP={s.player.CurrentHealth}\nKills={s.Kills}\nShots={s.Shots}\nEnemies={GameManager.Instance.EnemyCount}\nSpawned={s.spawner.TotalSpawned}\nPosition={s.player.transform.position}\nEnemyPoolCreated={GameManager.Instance.GetEnemyPool().CreatedCount}\nProjectilePoolCreated={GameManager.Instance.GetProjectilePool().CreatedCount}";
                foreach(var r in s.player.GetComponentsInChildren<Renderer>())status+=$"\nRenderer={r.name} {r.bounds}";
            }
            File.WriteAllText(Evidence+"/Snapshot-v01.txt",status);
        }
        [MenuItem("Course Game/Build Last Signal v02")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
            if(SceneManager.GetActiveScene().path==ScenePath)
            {
                if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before rebuilding.");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
            var unfinished=GameObject.Find("Enemy_v01");if(unfinished && unfinished.GetComponent<EnemyUnit>())UnityEngine.Object.DestroyImmediate(unfinished);
            Directory.CreateDirectory(Root+"/Materials");
            Directory.CreateDirectory(Root+"/Prefabs");
            Directory.CreateDirectory("Assets/CourseGame/SourceAnimations");
            foreach(string name in new[]{"idle","run"})
            {
                string src=$"Assets/Art/Animations/{name}.fbx", dest=$"Assets/CourseGame/SourceAnimations/{name}.fbx";
                if(!File.Exists(dest)) AssetDatabase.CopyAsset(src,dest);
                var imp=(ModelImporter)AssetImporter.GetAtPath(dest);
                imp.animationType=ModelImporterAnimationType.Generic;
                imp.importAnimation=true;
                var clips=imp.clipAnimations.Length>0?imp.clipAnimations:imp.defaultClipAnimations;
                foreach(var c in clips) { c.loopTime=true; c.keepOriginalPositionXZ=true; c.keepOriginalPositionY=true; c.lockRootPositionXZ=true; c.lockRootHeightY=true; c.lockRootRotation=true; }
                imp.clipAnimations=clips;imp.SaveAndReimport();
            }
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Mesh_v01.controller");
            if(!controller)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/Mesh_v01.controller");
                foreach(string name in new[]{"idle","run"})
                {
                    var clip=AssetDatabase.LoadAllAssetsAtPath($"Assets/CourseGame/SourceAnimations/{name}.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
                    var state=controller.layers[0].stateMachine.AddState(name=="idle"?"Idle":"Run");
                    state.motion=clip;
                    if(name=="idle")controller.layers[0].stateMachine.defaultState=state;
                }
            }
            var slate=Mat("Slate",new Color(.15f,.17f,.23f));
            var edge=Mat("DarkMetal",new Color(.055f,.065f,.1f));
            var trim=Mat("BlueTrim",new Color(.31f,.37f,.56f));
            var amber=Mat("Amber",new Color(1f,.4f,.15f),null,true);
            var blue=Mat("SignalBlue",new Color(.32f,.46f,1f),null,true);
            var playerMat=Mat("Human",Color.white,"Assets/Art/Skins/humanMaleA.png");
            var enemyMat=Mat("CrimsonZombie",new Color(1f,.25f,.36f),"Assets/Art/Skins/zombieMaleA.png");
            var enemyAltMat=Mat("CrimsonZombieFemale",new Color(1f,.25f,.36f),"Assets/Art/Skins/zombieFemaleA.png");
            var groundGO=new GameObject("GroundTile_12m");
            Cube("Floor",groundGO.transform,new Vector3(0,-.17f,0),new Vector3(12,.34f,12),slate,true);
            for(int i=-2;i<=2;i++)
            {
                Cube("PanelJointX",groundGO.transform,new Vector3(i*2,0.006f,0),new Vector3(.035f,.012f,12),edge,false);
                Cube("PanelJointZ",groundGO.transform,new Vector3(0,.006f,i*2),new Vector3(12,.012f,.035f),edge,false);
            }
            var floorModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/FBX format/template-floor-detail.fbx");
            if(floorModel)
            {
                var deco=(GameObject)PrefabUtility.InstantiatePrefab(floorModel);
                deco.name="CourseFloorDetail";deco.transform.SetParent(groundGO.transform,false);
                var bounds=BoundsOf(deco);
                deco.transform.localScale*=3.2f/Mathf.Max(bounds.size.x,bounds.size.z);
                bounds=BoundsOf(deco);deco.transform.position+=new Vector3(-4-bounds.center.x,-.09f-bounds.max.y,-4-bounds.center.z);
                foreach(var r in deco.GetComponentsInChildren<Renderer>())r.sharedMaterial=trim;
            }
            for(int i=0;i<5;i++) Cube("HazardStripe",groundGO.transform,new Vector3(3.4f+i*.4f,.017f,4.7f),new Vector3(.2f,.014f,.7f),amber,false);
            Cube("TileMarker",groundGO.transform,new Vector3(-5.4f,.017f,4.8f),new Vector3(.045f,.014f,1.4f),blue,false);
            var ground=PrefabUtility.SaveAsPrefabAsset(groundGO,Root+"/Prefabs/Ground_v01.prefab");UnityEngine.Object.DestroyImmediate(groundGO);

            GameObject MakeEnemy(string name,Material material)
            {
                var go=new GameObject(name);go.tag="Enemy";
                var rb=go.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;
                var capsule=go.AddComponent<CapsuleCollider>();capsule.height=1.7f;capsule.center=new Vector3(0,.85f,0);capsule.radius=.38f;
                var status=go.AddComponent<EnemyStatus>();Set(status,"maxHealth",30f);
                var unit=go.AddComponent<EnemyUnit>();Set(unit,"status",status);
                var visual=Visual(go,material,controller);
                var move=go.AddComponent<EnemyMovement>();Set(move,"moveSpeed",2.55f);Set(move,"stopDistance",.62f);Set(move,"rotateTowardTarget",false);
                var anim=go.AddComponent<EnemyMovementAnimation>();Set(anim,"animator",visual.GetComponent<Animator>());Set(anim,"turnRoot",go.transform.Find("VisualRoot"));
                var attack=go.AddComponent<EnemyAttackModule>();Set(attack,"contactDamage",9f);Set(attack,"attackInterval",1.2f);
                go.AddComponent<CourseEnemyFeedback>();go.AddComponent<DeadModule>();go.AddComponent<EnemyPoolMember>();
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;
            }
            EnsureTag("Enemy");
            var enemy=MakeEnemy("Enemy_v01",enemyMat);
            MakeEnemy("EnemyFemale_v01",enemyAltMat);
            GameObject MakeProjectile(string name,Material material)
            {
                var go=new GameObject(name);var mesh=GameObject.CreatePrimitive(PrimitiveType.Sphere);mesh.name="Core";mesh.transform.SetParent(go.transform,false);mesh.transform.localScale=Vector3.one*.2f;mesh.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
                var collider=go.AddComponent<SphereCollider>();collider.radius=.18f;collider.isTrigger=true;
                var rb=go.AddComponent<Rigidbody>();rb.useGravity=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var projectile=go.AddComponent<Projectile>();Set(projectile,"damage",15f);Set(projectile,"lifetime",1.6f);
                var trail=go.AddComponent<TrailRenderer>();trail.time=.16f;trail.startWidth=.15f;trail.endWidth=.01f;trail.sharedMaterial=material;trail.startColor=Color.white;trail.endColor=new Color(1,1,1,0);trail.minVertexDistance=.05f;
                var p=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return p;
            }
            var projectileA=MakeProjectile("ProjectileBlue_v01",blue);
            var projectileB=MakeProjectile("ProjectileAmber_v01",amber);
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.56f,.58f,.69f);RenderSettings.fog=false;RenderSettings.skybox=null;
            var light=new GameObject("KeyLight").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1f,.86f,.74f);light.intensity=1.4f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(48,-35,0);
            var player=new GameObject("Player");player.tag="Player";player.transform.position=new Vector3(0,.05f,0);
            var cc=player.AddComponent<CharacterController>();cc.height=1.7f;cc.center=new Vector3(0,.85f,0);cc.radius=.34f;cc.stepOffset=.25f;
            var ps=player.AddComponent<PlayerStatus>();var pu=player.AddComponent<PlayerUnit>();Set(pu,"status",ps);player.AddComponent<PlayerMovement>();
            var pv=Visual(player,playerMat,controller);
            var pa=player.AddComponent<PlayerMovementAnimation>();Set(pa,"animator",pv.GetComponent<Animator>());Set(pa,"turnRoot",player.transform.Find("VisualRoot"));
            player.AddComponent<DeadModule>();
            var slots=new GameObject("WeaponSlotController");slots.transform.SetParent(player.transform,false);
            var weaponArray=new WeaponSlot[2];
            for(int i=0;i<2;i++)
            {
                var slotGO=new GameObject("WeaponSlot_"+(i+1));slotGO.transform.SetParent(slots.transform,false);
                var slot=slotGO.AddComponent<WeaponSlot>();weaponArray[i]=slot;
                var template=(GameObject)PrefabUtility.InstantiatePrefab(i==0?projectileA:projectileB);template.transform.SetParent(slotGO.transform,false);template.SetActive(false);
                var fire=new GameObject("FirePoint").transform;fire.SetParent(slotGO.transform,false);
                Cube("Emitter",slotGO.transform,Vector3.zero,new Vector3(.22f,.15f,.35f),i==0?blue:amber,false);
                Set(slot,"projectileTemplate",template.GetComponent<Projectile>());Set(slot,"firePoint",fire);Set(slot,"attackRange",9.5f);Set(slot,"projectileSpeed",18f);Set(slot,"attackInterval",i==0?.46f:.58f);
            }
            var layout=slots.AddComponent<WeaponSlotController>();Set(layout,"radius",.72f);Set(layout,"height",.95f);layout.RefreshSlots();layout.ApplyLayout();
            var poolGO=new GameObject("EnemyPool");var enemyPool=poolGO.AddComponent<EnemyPool>();Set(enemyPool,"enemyPrefab",enemy.GetComponent<EnemyPoolMember>());Set(enemyPool,"initialCapacity",30);Set(enemyPool,"maxCapacity",40);
            var bulletPool=new GameObject("ProjectilePool").AddComponent<ProjectilePool>();Set(bulletPool,"initialCapacity",24);Set(bulletPool,"maxCapacity",80);
            var manager=new GameObject("GameManager").AddComponent<GameManager>();Set(manager,"player",pu);Set(manager,"enemyPool",enemyPool);Set(manager,"projectilePool",bulletPool);
            var groundController=new GameObject("GroundController").AddComponent<InfiniteGroundController>();Set(groundController,"groundPrefab",ground);Set(groundController,"target",player.transform);Set(groundController,"tileSize",12f);Set(groundController,"viewRadius",1);
            var preview=new GameObject("GroundPreview_EditorOnly");preview.tag="EditorOnly";
            for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++) {var t=(GameObject)PrefabUtility.InstantiatePrefab(ground);t.transform.SetParent(preview.transform);t.transform.position=new Vector3(x*12,0,z*12);}
            var spawner=new GameObject("EnemySpawner").AddComponent<EnemySpawner>();Set(spawner,"groundController",groundController);Set(spawner,"spawnHeight",.04f);Set(spawner,"spawnRadius",10f);Set(spawner,"maxActiveEnemies",30);
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=10.5f;camera.nearClipPlane=.1f;camera.farClipPlane=120;camera.backgroundColor=new Color(.07f,.075f,.12f);camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.position=new Vector3(0,18,-12);camera.transform.rotation=Quaternion.Euler(56.31f,0,0);camera.gameObject.AddComponent<AudioListener>();Set(camera.gameObject.AddComponent<CameraFollow>(),"target",player.transform);
            var session=new GameObject("CourseSession").AddComponent<CourseSession>();session.player=pu;session.spawner=spawner;session.weapons=weaponArray;session.gameObject.AddComponent<CourseHUD>();session.gameObject.AddComponent<CoursePreviewCleanup>();
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Unity refused to save the generated scene. Use a new scene version if this path is already open.");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);
            if(!previous.isDirty) EditorSceneManager.OpenScene(ScenePath);
            File.WriteAllText(Evidence+"/Build-v01.txt","Build complete\nScene="+ScenePath+"\nModels=course characterMedium + floor detail\nAnimator=Idle/Run, no parameters/transitions\nGround=3x3, tile=12m\nWeapons=2 projectile templates\n");
            ApplyOctober19();
            ApplyNovember9();
            ApplyNovember23();
        }
        [MenuItem("Course Game/Apply November 23 upgrade choices")]
        public static void ApplyNovember23()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(SceneManager.GetActiveScene().path!=ScenePath)Open();
            var session=UnityEngine.Object.FindAnyObjectByType<CourseSession>();
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var stats=session.player.GetComponent<PlayerUpgradeStats>();if(!stats)stats=session.player.gameObject.AddComponent<PlayerUpgradeStats>();
            var hud=session.GetComponent<CourseHUD>();
            var canvas=hud.playerCanvas;
            if(!canvas.GetComponent<GraphicRaycaster>())canvas.gameObject.AddComponent<GraphicRaycaster>();
            var root=canvas.transform.Find("PlayerHUD");var panel=root.Find("LevelUpPanel");
            if(!panel)
            {
                panel=MakePanel("LevelUpPanel",root,Vector2.zero,new Vector2(1440,900));
                panel.GetComponent<Image>().color=new Color(.025f,.03f,.065f,.94f);panel.GetComponent<Image>().raycastTarget=true;
                var card=MakePanel("UpgradeCard",panel,new Vector2(365,170),new Vector2(710,560));
                var title=MakeText("Title",card,new Vector2(35,30),new Vector2(640,60),34,Color.white);title.text="升級強化 · 三選一";title.alignment=TextAnchor.MiddleCenter;
                var subtitle=MakeText("Subtitle",card,new Vector2(35,100),new Vector2(640,35),18,new Color(.72f,.76f,.9f));subtitle.text="選擇一項獎勵後繼續戰鬥";subtitle.alignment=TextAnchor.MiddleCenter;
                var pending=MakeText("Text_Pending",card,new Vector2(35,150),new Vector2(640,35),20,new Color(.98f,.46f,.24f));pending.alignment=TextAnchor.MiddleCenter;
                string[] names={"Button_Health","Button_Damage","Button_MoveSpeed"};
                for(int i=0;i<3;i++)
                {
                    var rect=UIRect(names[i],card,new Vector2(55,215+i*95),new Vector2(600,78));
                    var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.17f,.21f,.36f);
                    var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};
                    var colors=button.colors;colors.highlightedColor=new Color(.72f,.78f,1);colors.pressedColor=new Color(.98f,.55f,.3f);button.colors=colors;
                    var label=MakeText("Label",rect,new Vector2(15,5),new Vector2(570,68),21,Color.white);label.alignment=TextAnchor.MiddleCenter;
                }
            }
            var upgradeCard=panel.Find("UpgradeCard");
            var controller=manager.GetComponent<LevelUpRewardController>();if(!controller)controller=manager.gameObject.AddComponent<LevelUpRewardController>();
            Set(controller,"gameManager",manager);Set(controller,"player",session.player);Set(controller,"upgradeStats",stats);Set(controller,"session",session);Set(controller,"levelUpPanel",panel.gameObject);
            Set(controller,"healthButton",upgradeCard.Find("Button_Health").GetComponent<Button>());
            Set(controller,"damageButton",upgradeCard.Find("Button_Damage").GetComponent<Button>());
            Set(controller,"moveSpeedButton",upgradeCard.Find("Button_MoveSpeed").GetComponent<Button>());
            Set(controller,"pendingCountText",upgradeCard.Find("Text_Pending").GetComponent<Text>());
            session.rewards=controller;panel.gameObject.SetActive(false);
            var events=UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if(!events)
            {
                var go=new GameObject("EventSystem",typeof(EventSystem));events=go.GetComponent<EventSystem>();
                var module=go.AddComponent<InputSystemUIInputModule>();module.AssignDefaultActions();
            }
            else if(!events.GetComponent<InputSystemUIInputModule>())events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorUtility.SetDirty(session);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Scene save failed.");
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Course Game/Apply November 9 lessons to current scene")]
        public static void ApplyNovember9()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(SceneManager.GetActiveScene().path!=ScenePath)Open();
            var session=UnityEngine.Object.FindAnyObjectByType<CourseSession>();
            if(!session)throw new InvalidOperationException("Open the existing Last Signal scene.");
            foreach(string name in new[]{"Enemy_v01","EnemyFemale_v01"})
            {
                string path=Root+"/Prefabs/"+name+".prefab";
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(!prefab.GetComponent<EnemyGrowthModule>())prefab.AddComponent<EnemyGrowthModule>();
                    PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
            var timer=UnityEngine.Object.FindAnyObjectByType<SurvivalTimer>();
            if(!timer)timer=new GameObject("SurvivalTimer").AddComponent<SurvivalTimer>();
            Set(timer,"player",session.player);Set(timer,"autoStart",false);session.survivalTimer=timer;
            var hud=session.GetComponent<CourseHUD>();
            if(!hud.playerCanvas)throw new InvalidOperationException("The existing PlayerHUD Canvas is required.");
            var root=hud.playerCanvas.transform.Find("PlayerHUD");
            var panel=root.Find("SurvivalTimerUI");
            if(!panel)
            {
                panel=MakePanel("SurvivalTimerUI",root,new Vector2(490,120),new Vector2(460,58));
                var caption=MakeText("TimerCaption",panel,new Vector2(18,12),new Vector2(160,32),18,new Color(.7f,.74f,.88f));
                caption.text="存活時間";
                var value=MakeText("TimerText",panel,new Vector2(188,7),new Vector2(252,44),28,Color.white);
                value.alignment=TextAnchor.MiddleCenter;value.text="00:00";
            }
            var timerUI=panel.GetComponent<SurvivalTimerUI>();if(!timerUI)timerUI=panel.gameObject.AddComponent<SurvivalTimerUI>();
            Set(timerUI,"survivalTimer",timer);Set(timerUI,"timerText",panel.Find("TimerText").GetComponent<Text>());
            EditorUtility.SetDirty(session);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Scene save failed.");
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Course Game/Apply October 19 lessons to current scene")]
        public static void ApplyOctober19()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(SceneManager.GetActiveScene().path!=ScenePath)Open();
            var session=UnityEngine.Object.FindAnyObjectByType<CourseSession>();
            if(!session)throw new InvalidOperationException("Open the existing Last Signal scene.");
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers");int obstacleLayer=LayerMask.NameToLayer("CourseObstacle");
            if(obstacleLayer<0)
            {
                for(int i=8;i<32;i++)if(layers.GetArrayElementAtIndex(i).stringValue==""){obstacleLayer=i;layers.GetArrayElementAtIndex(i).stringValue="CourseObstacle";break;}
                if(obstacleLayer<0)throw new InvalidOperationException("No free physics layer for obstacles.");
                tags.ApplyModifiedPropertiesWithoutUndo();
            }
            var metal=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/BlueTrim.mat");
            var dark=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DarkMetal.mat");
            var orange=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Amber.mat");
            var obstaclePrefabs=new GameObject[3];
            string[] names={"SupplyCrate","Barricade","SignalPillar"};
            for(int i=0;i<3;i++)
            {
                var go=new GameObject(names[i]);
                Vector3 size=i==0?new Vector3(1.2f,1.1f,1.2f):i==1?new Vector3(2f,1.25f,.55f):new Vector3(.85f,1.9f,.85f);
                Cube("Structure",go.transform,new Vector3(0,size.y*.5f,0),size,i==1?dark:metal,false);
                Cube("SignalStripe",go.transform,new Vector3(0,size.y*.72f,-size.z*.5f-.015f),new Vector3(size.x*.72f,.10f,.035f),orange,false);
                if(i==0)for(int n=-1;n<=1;n+=2)Cube("Brace",go.transform,new Vector3(n*.44f,.55f,-.62f),new Vector3(.09f,1.1f,.07f),dark,false);
                var box=go.AddComponent<BoxCollider>();box.size=size;box.center=Vector3.up*size.y*.5f;
                foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=obstacleLayer;
                obstaclePrefabs[i]=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+names[i]+".prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
            string groundPath=Root+"/Prefabs/Ground_v01.prefab";
            var ground=PrefabUtility.LoadPrefabContents(groundPath);
            try
            {
                var node=ground.GetComponentInChildren<GroundObstacleNode>(true);
                if(!node){var child=new GameObject("ObstacleNode");child.transform.SetParent(ground.transform,false);node=child.AddComponent<GroundObstacleNode>();}
                var so=new SerializedObject(node);var candidates=so.FindProperty("obstaclePrefabs");candidates.arraySize=3;
                for(int i=0;i<3;i++)candidates.GetArrayElementAtIndex(i).objectReferenceValue=obstaclePrefabs[i];
                so.ApplyModifiedPropertiesWithoutUndo();Set(node,"edgePadding",2f);Set(node,"playerSafeRadius",0f);Set(node,"randomSeed",12345);
                PrefabUtility.SaveAsPrefabAsset(ground,groundPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(ground);}
            foreach(string enemyName in new[]{"Enemy_v01","EnemyFemale_v01"})
            {
                string path=Root+"/Prefabs/"+enemyName+".prefab";
                var enemy=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(!enemy.transform.Find("EnemyHealthCanvas"))
                    {
                        var canvasGO=new GameObject("EnemyHealthCanvas",typeof(RectTransform),typeof(Canvas));canvasGO.transform.SetParent(enemy.transform,false);
                        var rect=(RectTransform)canvasGO.transform;rect.sizeDelta=new Vector2(100,10);rect.localPosition=new Vector3(0,2.05f,0);rect.localScale=Vector3.one*.01f;
                        var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=5;
                        var bar=MakeSlider("EnemyHP_Slider",rect,Vector2.zero,new Vector2(100,10),new Color(1f,.38f,.18f));
                        var health=canvasGO.AddComponent<EnemyHealthUI>();Set(health,"enemyUnit",enemy.GetComponent<EnemyUnit>());Set(health,"healthSlider",bar);
                    }
                    foreach(var bar in enemy.GetComponentsInChildren<Slider>(true))NormalizeFill(bar);
                    PrefabUtility.SaveAsPrefabAsset(enemy,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(enemy);}
            }
            var experience=session.player.GetComponent<PlayerExperience>();if(!experience)experience=session.player.gameObject.AddComponent<PlayerExperience>();
            Set(session.player,"experience",experience);Set(experience,"firstLevelRequirement",5);
            var groundController=UnityEngine.Object.FindAnyObjectByType<InfiniteGroundController>();
            Set(groundController,"viewRadius",2);Set(groundController,"preloadRings",1);
            // View radius covers the camera; one extra ring prepares obstacles before visibility.
            var hud=session.GetComponent<CourseHUD>();
            if(!hud.playerCanvas)
            {
                var canvasGO=new GameObject("PlayerHUDCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
                var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
                var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var root=new GameObject("PlayerHUD",typeof(RectTransform));root.transform.SetParent(canvasGO.transform,false);
                var rootRect=(RectTransform)root.transform;rootRect.anchorMin=rootRect.anchorMax=new Vector2(.5f,.5f);rootRect.sizeDelta=new Vector2(1440,900);
                var hpPanel=MakePanel("HealthPanel",rootRect,new Vector2(30,145),new Vector2(305,104));
                var hpText=MakeText("HP_Text",hpPanel,new Vector2(20,10),new Vector2(265,30),18,Color.white);
                var hp=MakeSlider("PlayerHP_Slider",hpPanel,new Vector2(20,56),new Vector2(265,13),new Color(.98f,.46f,.24f));
                var expPanel=MakePanel("ProgressPanel",rootRect,new Vector2(1120,25),new Vector2(290,160));
                var killText=MakeText("Kills_Text",expPanel,new Vector2(20,10),new Vector2(250,32),23,Color.white);
                var levelText=MakeText("Level_Text",expPanel,new Vector2(20,48),new Vector2(250,28),20,new Color(.55f,.65f,1f));
                var expText=MakeText("EXP_Text",expPanel,new Vector2(20,83),new Vector2(250,26),18,new Color(.7f,.74f,.88f));
                var exp=MakeSlider("EXP_Slider",expPanel,new Vector2(20,125),new Vector2(250,10),new Color(.42f,.55f,1f));
                var playerHUD=root.AddComponent<PlayerHUD>();Set(playerHUD,"player",session.player);Set(playerHUD,"healthSlider",hp);Set(playerHUD,"healthText",hpText);Set(playerHUD,"experienceSlider",exp);Set(playerHUD,"experienceText",expText);Set(playerHUD,"levelText",levelText);
                hud.playerCanvas=canvas;hud.killText=killText;
            }
            foreach(var bar in hud.playerCanvas.GetComponentsInChildren<Slider>(true))NormalizeFill(bar);
            EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Scene save failed.");
            AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/October19-integration.txt","Applied in place to "+ScenePath+"\nThree obstacle prefabs; view radius=2; preload rings=1; 49 pooled grounds; 25-cell combat area.\nPlayerExperience: first requirement 5, subsequent ceil(previous*1.1).\nNative PlayerHUD HP/EXP sliders, legacy Text, world-space enemy health sliders.\n");
        }
        static RectTransform UIRect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(pos.x,-pos.y);rect.sizeDelta=size;return rect;
        }
        static RectTransform MakePanel(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var rect=UIRect(name,parent,pos,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.045f,.05f,.09f,.96f);image.raycastTarget=false;return rect;
        }
        static Text MakeText(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize,Color color)
        {
            var rect=UIRect(name,parent,pos,size);var text=rect.gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.color=color;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
        }
        static Slider MakeSlider(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
        {
            var rect=UIRect(name,parent,pos,size);var slider=rect.gameObject.AddComponent<Slider>();
            var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.15f,.16f,.23f);bg.raycastTarget=false;
            var fill=UIRect("Fill",rect,Vector2.zero,size);var image=fill.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
            slider.fillRect=fill;slider.minValue=0;slider.maxValue=1;slider.value=1;slider.interactable=false;slider.transition=Selectable.Transition.None;
            slider.navigation=new Navigation{mode=Navigation.Mode.None};return slider;
        }
        static void NormalizeFill(Slider slider)
        {
            var fill=slider.fillRect;fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;
            fill.pivot=new Vector2(.5f,.5f);fill.sizeDelta=Vector2.zero;fill.anchoredPosition=Vector2.zero;
        }
        static GameObject Visual(GameObject root,Material material,AnimatorController controller)
        {
            var pivot=new GameObject("VisualRoot");pivot.transform.SetParent(root.transform,false);
            var scaleRoot=new GameObject("ModelScale");scaleRoot.transform.SetParent(pivot.transform,false);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Model/characterMedium.fbx");
            var mesh=(GameObject)PrefabUtility.InstantiatePrefab(source);mesh.name="Mesh";mesh.transform.SetParent(scaleRoot.transform,false);
            var bounds=BoundsOf(mesh);scaleRoot.transform.localScale*=1.7f/bounds.size.y;
            bounds=BoundsOf(mesh);scaleRoot.transform.position+=new Vector3(root.transform.position.x-bounds.center.x,root.transform.position.y-bounds.min.y,root.transform.position.z-bounds.center.z);
            foreach(var r in mesh.GetComponentsInChildren<Renderer>()) {r.sharedMaterial=material;if(r is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;}
            // Course clips are rooted at the skeleton, not the imported model wrapper.
            var rig=mesh.transform.Find("Root");if(!rig)throw new Exception("Course model has no Root skeleton");
            var wrapperAnimator=mesh.GetComponent<Animator>();if(wrapperAnimator)UnityEngine.Object.DestroyImmediate(wrapperAnimator);
            var animator=rig.GetComponent<Animator>();if(!animator)animator=rig.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            return rig.gameObject;
        }
        static Bounds BoundsOf(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;
        }
        static Material Mat(string name,Color color,string texture=null,bool emission=false)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.2f);
            if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
            if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.5f);}
            EditorUtility.SetDirty(m);return m;
        }
        static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collider)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static void EnsureTag(string name)
        {
            var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var tags=so.FindProperty("tags");
            for(int i=0;i<tags.arraySize;i++)if(tags.GetArrayElementAtIndex(i).stringValue==name)return;
            tags.InsertArrayElementAtIndex(tags.arraySize);tags.GetArrayElementAtIndex(tags.arraySize-1).stringValue=name;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Set(UnityEngine.Object target,string property,object value)
        {
            var so=new SerializedObject(target);var p=so.FindProperty(property);if(p==null)throw new Exception("Missing property "+property);
            if(value is UnityEngine.Object obj)p.objectReferenceValue=obj;else if(value is float f)p.floatValue=f;else if(value is int n)p.intValue=n;else if(value is bool b)p.boolValue=b;else if(value is string s)p.stringValue=s;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
