> 이전 변경 기록입니다. 현재 덱은 6종 고유 정의이며 Available은 덱을 제외합니다. 최신 동작은 DeckLobbyHandRetryPatch.md를 참고하세요.

# Expanded test catalog

Lobby lists 1-1 through 1-10. These are separate stage definitions sharing the existing TestStage map and BasicEnemy. Existing sequential unlocking remains: only 1-1 starts unlocked. No automatic victory or unlock cheat was added.

Starting resources increase from 150 to 375 in steps of 25; enemy spawn intervals decrease from 2.0 to 1.1 seconds. Base HP stays at 10. Initial deck assignments are unchanged; all 15 tower definitions are available in the Deck Editor's scrollable left page.

All towers reuse BasicTurret, ClosestTargeting and DirectDamageAttack. Values are temporary test values, not balanced content.

| Tower | Damage | Range | Cooldown (seconds) | Cost |
|---|---:|---:|---:|---:|
| Test Turret 1 | 10 | 3 | 1.000 | 50 |
| Test Turret 2 | 30 | 5 | 2.500 | 75 |
| Test Turret 3 | 5 | 2.5 | 0.333 | 100 |
| Test Turret 4 | 8 | 3.5 | 0.500 | 60 |
| Test Turret 5 | 45 | 5.5 | 3.000 | 125 |
| Test Turret 6 | 3 | 2 | 0.200 | 70 |
| Test Turret 7 | 18 | 4 | 1.200 | 80 |
| Test Turret 8 | 60 | 6 | 4.000 | 150 |
| Test Turret 9 | 12 | 2.8 | 0.600 | 90 |
| Test Turret 10 | 22 | 4.5 | 1.500 | 95 |
| Test Turret 11 | 6 | 3.2 | 0.300 | 110 |
| Test Turret 12 | 35 | 3.8 | 2.000 | 105 |
| Test Turret 13 | 15 | 5.2 | 1.000 | 120 |
| Test Turret 14 | 9 | 2.2 | 0.250 | 130 |
| Test Turret 15 | 25 | 4.2 | 0.800 | 150 |

Added: Stage1_1, Stage1_4 through Stage1_10, TestTurret4 through TestTurret15 assets and their meta files. Updated: Stage1_2/3 settings, GameCatalog, LobbyAssetSetup, PrototypeValidation and current documentation. No new scenes, prefabs, packages or combat classes.

Validation: Unity Play Mode passed 233 assertions, including every tower's cost, range, damage and cooldown; catalog order, carousel boundaries and all 15 Deck Editor cards. Actual OS mouse/keyboard interaction and final visual layout were not manually verified.
