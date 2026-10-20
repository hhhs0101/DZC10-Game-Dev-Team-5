using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Defense.Editor {
// Focused acceptance runner; requires a separate pre-migration profile copy.
[InitializeOnLoad]
public static class CondimentTowerValidation {
    private const string Key="Defense.Condiment.Validation";
    private static int phase,checks;
    private static double started;
    private static bool restart;
    private static string deckBytes,currencyBytes;
    private static T Find<T>() where T:UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>();
    static CondimentTowerValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run()=>Start(false);
    public static void ReadRestart()=>Start(true);
    private static void Start(bool read){
        var path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
        if(EditorApplication.isPlaying||string.IsNullOrWhiteSpace(path)||Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Use an isolated pre-migration profile in Edit Mode.");
        SessionState.SetBool(Key+".Restart",read);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        SessionState.SetString(Key+".Deck",File.ReadAllText(path));SessionState.SetString(Key+".Currency",File.ReadAllText(PlayerCurrency.DefaultPath));
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){phase=checks=0;started=EditorApplication.timeSinceStartup;restart=SessionState.GetBool(Key+".Restart",false);deckBytes=SessionState.GetString(Key+".Deck","");currencyBytes=SessionState.GetString(Key+".Currency","");EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string m,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,m+"\n"+trace);}
    private static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;Debug.Log("CONDIMENT PASS: "+message);}
    private static void Click(string label){var b=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.GetComponentInChildren<Text>().text==label);Check(b.interactable,"UI callback enabled: "+label);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    private static TowerCardDefinition Card(int number)=>AssetDatabase.LoadAssetAtPath<TowerCardDefinition>("Assets/Defense/Data/TowerCard"+number+".asset");
    private static void ProfileUnchanged(){Check(File.ReadAllText(DeckPersistence.DefaultPath)==deckBytes&&File.ReadAllText(PlayerCurrency.DefaultPath)==currencyBytes,"Pre-migration deck and currency bytes remain unchanged");}
    private static void ModelChecks(){
        string[] names={"Vinegar","Olive Oil","Hot Sauce","Black Pepper","Paprika Powder","Salt","Brown Sauce","Soy Sauce","Mustard","Mayonnaise","Ketchup"};
        Check(PlayerSession.Catalog.AvailableTowers.Count==15&&PlayerSession.Catalog.AvailableCards.Count==17,"All fifteen existing Towers and two Skills remain, with no new cards");
        var resolver=new CardCatalog(PlayerSession.Catalog.AvailableCards);
        for(int i=1;i<=15;i++){
            var card=Card(i);Check(resolver.TryResolve("tower.test."+i,out var resolved)&&resolved==card&&card.Tower==AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/TestTurret"+i+".asset"),"Stable Card ID resolves original Tower asset: "+i);
            if(i<=11){
                Check(card.DisplayName==names[i-1]&&card.Tower.DisplayName==names[i-1]&&card.Icon==card.Tower.Sprite&&card.Icon!=null,"Exact numeric name/art mapping: "+i+" -> "+names[i-1]);
                var path=AssetDatabase.GetAssetPath(card.Icon);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Check(Path.GetFileNameWithoutExtension(path)==names[i-1].Replace(" ","")&&importer.spriteImportMode==SpriteImportMode.Single&&importer.filterMode==FilterMode.Point&&importer.textureCompression==TextureImporterCompression.Uncompressed&&!importer.mipmapEnabled&&importer.spritePixelsPerUnit==128&&importer.alphaIsTransparency,"Single/Point/uncompressed/alpha Sprite uses exact named mapping: "+names[i-1]);
            }else Check(card.DisplayName=="Test Turret "+i&&card.Tower.Sprite==null&&card.Icon==null,"Unmapped Tower retains its existing name and placeholder: "+i);
        }
        Check(PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>().All(c=>c.Icon==null),"Skill cards retain their existing presentation");
        var saved=JsonUtility.FromJson<DeckSaveData>(deckBytes);
        Check(PlayerSession.Presets.ActivePresetIndex==saved.ActivePresetIndex&&Enumerable.Range(0,3).All(p=>PlayerSession.Presets.GetPreset(p).Slots.Select(c=>c.CardId).SequenceEqual(saved.Presets[p].CardIds)),"All three pre-migration preset compositions, order and active index load without repairs");
        ProfileUnchanged();
    }
    private static void CheckView(Transform root,CardDefinition definition){
        Check(root.GetComponentInChildren<Text>().text.Contains(definition.DisplayName),"Rendered UI label uses current data name: "+definition.DisplayName);
        var icon=root.Find("Card Artwork").GetComponent<Image>();
        Check(icon.gameObject.activeSelf==(definition.Icon!=null)&&icon.sprite==definition.Icon&&icon.preserveAspect&&!icon.raycastTarget,"Card artwork matches Tower Sprite without intercepting pointer input: "+definition.DisplayName);
    }
    private static void EditorChecks(){
        int selected=PlayerSession.Presets.ActivePresetIndex;
        for(int p=0;p<3;p++){
            if(PlayerSession.Presets.ActivePresetIndex!=p)Click("Preset "+(p+1));
            for(int i=0;i<6;i++)CheckView(GameObject.Find("Deck Slot T"+(i+1)).transform,PlayerSession.Deck.Slots[i]);
            foreach(var available in UnityEngine.Object.FindObjectsByType<DeckCardDrag>())CheckView(available.transform,available.Definition);
        }
        if(PlayerSession.Presets.ActivePresetIndex!=selected)Click("Preset "+(selected+1));ProfileUnchanged();
    }
    private static void ValidatePlaced(Turret turret,TowerCardDefinition card,TowerPlacementSlot slot){
        var visual=turret.GetComponentInChildren<SpriteRenderer>();
        Check(turret.Definition==card.Tower&&visual.sprite==card.Icon&&visual.color==Color.white&&turret.GetComponent<TurretSpriteView>()!=null,"Actually placed Tower uses shared presentation observer and correct art: "+card.DisplayName);
        Check(turret.transform.position==slot.PlacementPoint.position&&turret.transform.localScale==Vector3.one&&slot.GetComponent<BoxCollider2D>().size==Vector2.one&&turret.transform.Find("ProjectileOrigin").localPosition==new Vector3(0,.55f,0),"Logical placement/root/collider/attack anchor preserved: "+card.DisplayName);
        Check(visual.sortingOrder>slot.GetComponentInChildren<SpriteRenderer>().sortingOrder&&visual.sortingOrder==40,"Bottle renders above wooden plate: "+card.DisplayName);
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(card.Icon)));
        try{
            int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=16){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            var point=new Vector3(((minX+maxX+1)*.5f-card.Icon.pivot.x)/card.Icon.pixelsPerUnit,(minY-card.Icon.pivot.y)/card.Icon.pixelsPerUnit,0);
            Check(Vector2.Distance(visual.transform.TransformPoint(point),slot.PlacementPoint.position)<.001f&&Mathf.Abs((maxY-minY+1)/card.Icon.pixelsPerUnit*visual.transform.localScale.y-1.35f)<.001f,"Visible alpha base is centered on plate; bottle height is consistent: "+card.DisplayName);
        }finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    private static void PlacementChecks(){
        var hand=Find<GameplayHand>();var placement=Find<TurretPlacementController>();var flow=Find<GameFlow>();var wallet=Find<ResourceWallet>();var elixir=Find<ElixirSystem>();var slot=Find<BattlefieldMap2D>().PlacementSlots[0];
        foreach(int n in Enumerable.Range(1,11)){
            var card=Card(n);var fixture=new PlayerDeck(new CardDefinition[]{card}.Concat(PlayerSession.Catalog.AvailableCards.OfType<TowerCardDefinition>().Where(c=>c!=card).Take(5)).ToArray());
            hand.Initialize(fixture,placement,flow);elixir.Initialize(flow);wallet.Initialize(10000);
            // Fixture setup only: bring each of eleven types into the 3-card Hand without editing persistent presets.
            while(!hand.Cycle.Hand.Any(c=>c.Definition==card))hand.Cycle.Use(hand.Cycle.Hand[0]);
            int index=hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card);hand.CancelSelection();
            // Refresh the existing UI after the fixture-only cycle setup, then use its actual callback.
            var button=GameObject.Find("Hand Card "+(index+1));button.GetComponent<Button>().onClick.Invoke();CheckView(button.transform,card);
            var before=hand.Cycle.Hand.ToArray();var queue=hand.Cycle.UpcomingQueue.ToArray();var used=hand.SelectedCard;float energy=elixir.Current;
            var screen=Camera.main.WorldToScreenPoint(slot.PlacementPoint.position);
            Check(placement.ProcessPointer(screen,true),"Existing screen/Physics2D/Hand placement commits: "+card.DisplayName);
            Check(wallet.Balance==10000-card.Tower.Cost&&Mathf.Approximately(elixir.Current,energy-card.ElixirCost)&&hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=used).Concat(new[]{queue[0]}))&&hand.Cycle.UpcomingQueue.SequenceEqual(queue.Skip(1).Concat(new[]{used})),"Success retains resource/Elixir and exactly-one cycle semantics: "+card.DisplayName);
            var turret=slot.Occupant;ValidatePlaced(turret,card,slot);
            hand.Select(0);before=hand.Cycle.Hand.ToArray();queue=hand.Cycle.UpcomingQueue.ToArray();int balance=wallet.Balance;energy=elixir.Current;
            Check(!placement.ProcessPointer(screen,true)&&wallet.Balance==balance&&elixir.Current==energy&&hand.Cycle.Hand.SequenceEqual(before)&&hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Occupied-slot failure never spends or cycles: "+card.DisplayName);
            UnityEngine.Object.DestroyImmediate(turret.gameObject);
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CondimentTowerSetup.SharedPrefab);
        foreach(int n in Enumerable.Range(12,4)){
            var root=UnityEngine.Object.Instantiate(prefab);try{root.GetComponent<Turret>().Initialize(Card(n).Tower,Find<EnemyRegistry>());var visual=root.GetComponentInChildren<SpriteRenderer>();Check(visual.sprite==prefab.GetComponentInChildren<SpriteRenderer>().sprite&&visual.color==prefab.GetComponentInChildren<SpriteRenderer>().color,"Shared view retains placeholder for Tower "+n);}finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        hand.Initialize(PlayerSession.Deck,placement,flow);ProfileUnchanged();
        DeckPersistenceValidation.Models(PlayerSession.Catalog,Check);
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>100)throw new Exception("Condiment validation timeout at "+phase);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(phase==0){
            if(GameObject.Find("Start Game")==null)return;ModelChecks();Click("Start Game");
            for(int i=0;i<6;i++)CheckView(GameObject.Find("Deck Preview T"+(i+1)).transform,PlayerSession.Deck.Slots[i]);
            if(!restart){Click("Deck");EditorChecks();Click("Back to Lobby");}
            Click("Enter Stage");phase=1;
        }else if(phase==1){
            if(GameObject.Find("Hand Card 1")==null)return;Find<FixedIntervalSpawner>().enabled=false;
            var hand=Find<GameplayHand>();Check(hand.Cycle.InitialOrder.Select(c=>c.Definition).OrderBy(c=>c.CardId).SequenceEqual(PlayerSession.Deck.Slots.OrderBy(c=>c.CardId)),"Battle runtime copy still uses the same six saved card identities");
            for(int i=0;i<3;i++)CheckView(GameObject.Find("Hand Card "+(i+1)).transform,hand.Cycle.Hand[i].Definition);
            if(!restart)PlacementChecks();ProfileUnchanged();Finish(true,checks+(restart?" separate-process restart assertions":" focused mapping/UI/placement/persistence assertions"));
        }
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Finish(bool pass,string message){
        if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
        Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Condiment_"+(restart?"Restart":"Validation")+"_Result.txt",(pass?"PASS: ":"FAIL: ")+message);
        if(pass)Debug.Log("CONDIMENT_TOWERS_PASSED "+message);else Debug.LogError("CONDIMENT_TOWERS_FAILED "+message);
        EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);
    }
}
}
