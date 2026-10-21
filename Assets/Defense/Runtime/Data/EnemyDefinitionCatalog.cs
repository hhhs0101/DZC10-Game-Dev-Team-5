using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// Definition assets for authoring IDs; unrelated to EnemyRegistry's living scene actors.
[CreateAssetMenu(menuName="Defense/Enemy Definition Catalog")]
public sealed class EnemyDefinitionCatalog : ScriptableObject {
    public const string ResourcePath = "EnemyDefinitionCatalog";
    [SerializeField] private EnemyDefinition[] definitions = Array.Empty<EnemyDefinition>();
    public IReadOnlyList<EnemyDefinition> Definitions => Array.AsReadOnly(definitions ?? Array.Empty<EnemyDefinition>());
    public bool TryBuildLookup(out Dictionary<string,EnemyDefinition> lookup,out string error) {
        lookup = new Dictionary<string,EnemyDefinition>(StringComparer.Ordinal); error = null;
        for(int i=0;i<Definitions.Count;i++) {
            var definition = Definitions[i];
            if(definition == null) { error="Enemy catalog entry ["+i+"] has a missing definition reference.";return false; }
            string id=definition.EnemyTypeId;
            if(string.IsNullOrWhiteSpace(id)) { error="Enemy catalog entry ["+i+"] '"+definition.name+"' has a blank enemy ID.";return false; }
            if(lookup.ContainsKey(id)) { error="Enemy catalog has duplicate/ambiguous enemy ID '"+id+"'.";return false; }
            if(definition.Prefab == null || definition.Prefab.GetComponent<EnemyHealth>() == null || definition.Prefab.GetComponent<EnemyPathFollower>() == null) {
                error="Enemy catalog ID '"+id+"' requires a usable Enemy prefab with health and path movement.";return false;
            }
            lookup.Add(id,definition);
        }
        if(lookup.Count==0) {error="Enemy definition catalog is empty.";return false;}
        return true;
    }
}
}
