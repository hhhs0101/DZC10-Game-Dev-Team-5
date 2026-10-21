using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
namespace Defense {
public static class SpawnPlanLoader {
    public static bool TryLoad(TextAsset asset,string stageName,EnemyDefinitionCatalog catalog,out ResolvedSpawnPlan plan,out string error) {
        plan=null;error=null;var reader=new Reader(asset != null ? asset.text : "");
        try {
            if(asset==null)throw new FormatException("No JSON TextAsset assigned.");
            var dto=reader.Read();
            if(catalog==null)throw new FormatException("Enemy definition catalog is missing.");
            if(!catalog.TryBuildLookup(out var lookup,out var problem))throw new FormatException(problem);
            var events=new ResolvedSpawnEvent[dto.spawns.Length];
            for(int i=0;i<events.Length;i++) {
                var entry=dto.spawns[i];
                if(!lookup.TryGetValue(entry.enemyId,out var enemy))throw new FormatException("Event ["+i+"] has unknown enemyId '"+entry.enemyId+"'.");
                events[i]=new ResolvedSpawnEvent(enemy,entry.delay);
            }
            plan=new ResolvedSpawnPlan(dto.stageId,events);return true;
        } catch(FormatException exception) {
            error="Stage '"+stageName+"', spawn JSON '"+(asset!=null?asset.name:"<missing>")+"', stageId '"+(reader.StageId??"<unread>")+"': "+exception.Message;
            return false;
        }
    }
    // A strict reader for this small schema. JsonUtility alone silently defaults an omitted delay to zero.
    // Unknown/duplicate fields and nonnumeric values are rejected rather than guessed or coerced.
    private sealed class Reader {
        private readonly string source;
        private int cursor,eventIndex=-1;
        public string StageId { get; private set; }
        public Reader(string json) {source=json??"";}
        private void White() {while(cursor<source.Length&&(source[cursor]==' '||source[cursor]=='\t'||source[cursor]=='\r'||source[cursor]=='\n'))cursor++;}
        private bool Take(char value) {White();if(cursor>=source.Length||source[cursor]!=value)return false;cursor++;return true;}
        private void Need(char value) {if(!Take(value))Fail("Expected '"+value+"'.");}
        private void Fail(string message) {
            int count=Math.Min(32,source.Length-cursor);string value=count>0?source.Substring(cursor,count).Replace("\n"," ").Replace("\r"," "):"<end>";
            throw new FormatException((eventIndex>=0?"Event ["+eventIndex+"]: ":"")+message+" At character "+cursor+", near '"+value+"'.");
        }
        public SpawnPlanData Read() {
            Need('{');SpawnEventData[] entries=null;bool hasId=false,hasSpawns=false;
            if(!Take('}'))do {
                string key=String();Need(':');
                if(key=="stageId") {if(hasId)Fail("Duplicate stageId.");hasId=true;StageId=String();if(string.IsNullOrWhiteSpace(StageId))Fail("stageId must be a nonblank string.");}
                else if(key=="spawns") {if(hasSpawns)Fail("Duplicate spawns.");hasSpawns=true;entries=Events();}
                else Fail("Unknown plan field '"+key+"'.");
                if(Take('}'))break;Need(',');
            }while(true);
            White();if(cursor!=source.Length)Fail("Unexpected trailing JSON content.");
            if(!hasId)Fail("Required stageId is missing.");
            if(!hasSpawns||entries==null||entries.Length==0)Fail("Required spawns must be a nonempty array.");
            return new SpawnPlanData{stageId=StageId,spawns=entries};
        }
        private SpawnEventData[] Events() {
            Need('[');var entries=new List<SpawnEventData>();
            if(Take(']'))return entries.ToArray();
            do {
                eventIndex=entries.Count;Need('{');string id=null;float delay=0;bool hasId=false,hasDelay=false;
                if(!Take('}'))do {
                    string key=String();Need(':');
                    if(key=="enemyId") {if(hasId)Fail("Duplicate enemyId.");hasId=true;id=String();if(string.IsNullOrWhiteSpace(id))Fail("enemyId must be nonblank.");}
                    else if(key=="delay") {if(hasDelay)Fail("Duplicate delay.");hasDelay=true;delay=Delay();}
                    else Fail("Unknown event field '"+key+"'.");
                    if(Take('}'))break;Need(',');
                }while(true);
                if(!hasId)Fail("Required enemyId is missing.");if(!hasDelay)Fail("Required numeric delay is missing.");
                entries.Add(new SpawnEventData{enemyId=id,delay=delay});eventIndex=-1;
                if(Take(']'))break;Need(',');
            }while(true);
            return entries.ToArray();
        }
        private bool Digit() => cursor<source.Length&&source[cursor]>='0'&&source[cursor]<='9';
        private float Delay() {
            White();int start=cursor;
            if(cursor<source.Length&&source[cursor]=='-')cursor++;
            if(!Digit())Fail("delay must be an explicitly present JSON number.");
            if(source[cursor]=='0')cursor++;else while(Digit())cursor++;
            if(cursor<source.Length&&source[cursor]=='.') {cursor++;if(!Digit())Fail("Fraction requires digits.");while(Digit())cursor++;}
            if(cursor<source.Length&&(source[cursor]=='e'||source[cursor]=='E')) {
                cursor++;if(cursor<source.Length&&(source[cursor]=='+'||source[cursor]=='-'))cursor++;
                if(!Digit())Fail("Exponent requires digits.");while(Digit())cursor++;
            }
            string token=source.Substring(start,cursor-start);
            if(!double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out double number)||double.IsNaN(number)||double.IsInfinity(number)||number<0||number>float.MaxValue)Fail("Invalid delay '"+token+"'; expected a finite nonnegative gameplay duration.");
            return (float)number;
        }
        private string String() {
            Need('"');var result=new StringBuilder();
            while(cursor<source.Length) {
                char value=source[cursor++];if(value=='"')return result.ToString();
                if(value<32)Fail("Unescaped control character in JSON string.");
                if(value!='\\') {result.Append(value);continue;}
                if(cursor>=source.Length)Fail("Incomplete string escape.");
                value=source[cursor++];
                switch(value) {
                    case '"':case '\\':case '/':result.Append(value);break;
                    case 'b':result.Append('\b');break;case 'f':result.Append('\f');break;
                    case 'n':result.Append('\n');break;case 'r':result.Append('\r');break;case 't':result.Append('\t');break;
                    case 'u':
                        if(cursor+4>source.Length)Fail("Incomplete Unicode escape.");
                        if(!ushort.TryParse(source.Substring(cursor,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out var code))Fail("Invalid Unicode escape.");
                        result.Append((char)code);cursor+=4;break;
                    default:Fail("Invalid JSON string escape.");break;
                }
            }
            Fail("Unterminated JSON string.");return null;
        }
    }
}
}
