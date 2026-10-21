using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
// Regression coverage for the supported route, including every retained stage identity.
[InitializeOnLoad]
public static class TwoDOnlyValidation {
    private const string Key="Defense.TwoDOnly.Validation";
    private static int phase,index,checks;
    private static double started;
    private static bool expectedError;
    private static RuntimeCardCycle oldCycle;
    private static string deckBytes,currencyBytes;
    private static StageDefinition stage;
    static TwoDOnlyValidation(){if(SessionState.GetBool(Key,false))Attach();}
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("2D_ONLY PASS: "+message);}
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
        if(string.IsNullOrWhiteSpace(path)||Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar)||Path.GetFullPath(PlayerCurrency.DefaultPath).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Distinct isolated Deck and currency files are required.");
        checks=0;Audit();SessionState.SetInt(Key+".Checks",checks);
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Audit(){
        var build=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        Check(build.SequenceEqual(new[]{"Assets/Defense/Scenes/MainMenu.unity",TopDownStage3Setup.ScenePath}),"Build starts at MainMenu and contains only the integrated 2D gameplay scene");
        var catalog=Resources.Load<GameCatalog>("GameCatalog");
        Check(catalog.Stages.Select(s=>s.DisplayName).SequenceEqual(new[]{"1-1","1-2","1-3","Test Stage","Test Stage 2"}),"Five original stage identities and order are preserved");
        foreach(var stage in catalog.Stages){
            Check(stage.SceneName==Path.GetFileNameWithoutExtension(TopDownStage3Setup.ScenePath),"Existing shared 2D map is explicit on "+stage.DisplayName);
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Defense/Scenes/"+stage.SceneName+".unity")!=null,"Stage scene exists: "+stage.DisplayName);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:EnemyDefinition t:TurretDefinition",new[]{"Assets/Defense/Data"})){
            var obj=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            var reference=new SerializedObject(obj).FindProperty("prefab");if(reference==null)continue;
            var actor=reference.objectReferenceValue as Component;
            Check(actor!=null&&actor.GetComponentInChildren<SpriteRenderer>(true)!=null&&actor.GetComponentInChildren<Collider>(true)==null&&actor.GetComponentInChildren<MeshRenderer>(true)==null,"Definition uses existing sprite actor: "+obj.name);
        }
        foreach(var path in new[]{TopDownStage1Setup.ScenePath,TopDownStage2Setup.ScenePath,TopDownStage3Setup.ScenePath}){
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try{
                var objects=scene.GetRootGameObjects();
                Check(objects.SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).All(c=>c!=null),"No Missing Script in "+path);
                Check(!objects.SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Any()&&!objects.SelectMany(g=>g.GetComponentsInChildren<Rigidbody>(true)).Any(),"Retained battlefield contains only 2D collision components: "+path);
            }finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
    private static void Attach(){phase=index=0;checks=SessionState.GetInt(Key+".Checks",0);started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string message,string trace,LogType type){
        if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;
        if(expectedError&&type==LogType.Error&&message.Contains("requires an available 2D scene"))return;
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);
    }
    private static void Click(string label){var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text==label);Check(button.interactable,"UI callback available: "+label);button.onClick.Invoke();}
    private static void InvalidRoute(){
        var original=PlayerSession.Catalog.Stages[0];var sceneName=original.SceneName;
        var data=new SerializedObject(original);data.FindProperty("sceneName").stringValue="Unavailable2DBattlefield";data.ApplyModifiedPropertiesWithoutUndo();
        var active=PlayerSession.ActiveStage;
        try{
            expectedError=true;
            Check(!Find<LobbyUI>().TryEnter()&&SceneManager.GetActiveScene().name=="MainMenu"&&PlayerSession.ActiveStage==active,"Missing stage scene blocks entry without substituting a map or changing active stage");
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("Unavailable2DBattlefield")&&t.text.Contains("requires an available 2D scene")),"Lobby displays an actionable missing-scene error");
        }finally{expectedError=false;data.FindProperty("sceneName").stringValue=sceneName;data.ApplyModifiedPropertiesWithoutUndo();}
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>90)throw new Exception("2D-only validation timed out at "+phase);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(phase==0){
            if(GameObject.Find("Start Game")==null)return;Click("Start Game");
            Check(GameObject.Find("Play 3D")==null&&GameObject.Find("Play 3D Fallback")==null&&GameObject.Find("Play 2D")==null&&GameObject.Find("Enter Stage")!=null,"Lobby has one stage entry and no mode-selection buttons");
            InvalidRoute();deckBytes=File.ReadAllText(DeckPersistence.DefaultPath);currencyBytes=File.ReadAllText(PlayerCurrency.DefaultPath);phase=1;
        }else if(phase==1){
            if(Find<LobbyUI>()==null)return;
            while(PlayerSession.Selection.SelectedStageIndex<index)PlayerSession.Selection.MoveNext();
            Find<LobbyUI>().Carousel.Advance(1);stage=PlayerSession.Catalog.Stages[index];Click("Enter Stage");phase=2;
        }else if(phase==2){
            if(Find<GameplayUI>()==null||GameObject.Find("Hand Card 1")==null)return;
            Find<FixedIntervalSpawner>().enabled=false;
            Check(PlayerSession.ActiveStage==stage&&SceneManager.GetActiveScene().name==stage.SceneName&&Find<BattlefieldMap2D>()!=null&&Find<EnemyRegistry>().Plane==WaypointPlane.XY,"Actual stage entry uses configured 2D map: "+stage.DisplayName);
            Check(Find<BaseHealth>().Current==stage.BaseHealth&&Find<ResourceWallet>().Balance==stage.StartingResources,"Stage-specific battle settings survive entry: "+stage.DisplayName);
            oldCycle=Find<GameplayHand>().Cycle;Find<GameFlow>().GameOver();Click("Retry");phase=3;
        }else if(phase==3){
            if(Find<GameplayUI>()==null||GameObject.Find("Hand Card 1")==null||Find<GameplayHand>().Cycle==oldCycle)return;
            Find<FixedIntervalSpawner>().enabled=false;
            Check(PlayerSession.ActiveStage==stage&&SceneManager.GetActiveScene().name==stage.SceneName&&Find<GameFlow>().IsPlaying&&Find<FixedIntervalSpawner>().SpawnedCount==0,"Retry creates a fresh run in the same configured 2D stage: "+stage.DisplayName);
            Check(File.ReadAllText(DeckPersistence.DefaultPath)==deckBytes&&File.ReadAllText(PlayerCurrency.DefaultPath)==currencyBytes,"Entry/Retry preserve all preset and currency bytes");
            PlayerSession.Progression.Complete(index);Click("Pause");Click("Exit");Click("Yes");phase=4;
        }else if(phase==4){
            if(Find<LobbyUI>()==null)return;
            Check(PlayerSession.Selection.SelectedStageIndex==index&&Time.timeScale==1,"Exit returns to the same Lobby selection");
            index++;if(index==PlayerSession.Catalog.Stages.Count)Finish(true,checks+" assertions");else phase=1;
        }
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Finish(bool pass,string message){
        if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
        Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/2D_Only_Result.txt",(pass?"PASS: ":"FAIL: ")+message);
        if(pass)Debug.Log("TWO_D_ONLY_PASSED "+message);else Debug.LogError("TWO_D_ONLY_FAILED "+message);
        EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);
    }
    public static void Build(){
        var output=Path.Combine(Path.GetTempPath(),"defense-2d-only-build","Defense.app");Directory.CreateDirectory(Path.GetDirectoryName(output));
        var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),output,BuildTarget.StandaloneOSX,BuildOptions.Development);
        Debug.Log("TWO_D_ONLY_BUILD "+result.summary.result+" errors="+result.summary.totalErrors);
        if(Application.isBatchMode)EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
}
}
