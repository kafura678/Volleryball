# Volleyball Game Specification

## 1. Project Goal

Create a 3D player-controlled volleyball game in Unity.

The overall gameplay direction is inspired by player-controlled sports games such as eFootball:

- the player directly controls an athlete on the court;
- movement and positioning matter;
- the player performs volleyball actions manually;
- the camera shows enough of the court to understand the match;
- controls should feel responsive rather than fully automated.

The long-term goal is to release the game on smartphones with online multiplayer.

Development must be incremental.

Do not attempt to implement every final feature at once.

The first priority is creating a fun, stable offline volleyball match.

---

# 2. Target Platforms

## Primary Development Platform

PC / macOS inside Unity Editor.

Keyboard and gamepad controls may be used during development.

## Final Target Platform

Smartphones:

- iOS
- Android

The game must eventually support touch controls.

Gameplay systems should therefore not depend directly on keyboard-specific input.

Input should be abstracted through Unity's new Input System.

---

# 3. Development Phases

Development should proceed in the following order.

## Phase 1 — Core Volleyball Prototype

Create a playable offline prototype.

Requirements:

- 3D volleyball court;
- two sides separated by a net;
- one controllable player per side;
- one human-controlled player;
- one CPU-controlled opponent;
- volleyball physics;
- serving;
- receiving;
- setting;
- attacking;
- basic scoring;
- match reset;
- camera;
- basic UI.

The objective of Phase 1 is to determine whether moving and hitting the volleyball feels enjoyable.

Do not implement online multiplayer during the first core gameplay implementation.

---

## Phase 2 — Complete Offline Match

Expand the prototype into a complete offline game.

Requirements:

- improved volleyball rules;
- better CPU behavior;
- player animations;
- match flow;
- score UI;
- serve rotation;
- victory and defeat;
- pause;
- restart;
- sound;
- basic visual effects.

Add additional players only after the one-player-per-side prototype is stable.

---

## Phase 3 — Mobile Version

Adapt the game for smartphones.

Requirements:

- virtual movement stick;
- touch buttons;
- responsive mobile UI;
- scalable interface;
- mobile performance optimization;
- Android build;
- iOS-compatible architecture.

Desktop controls must remain available for development.

Gameplay code must not directly depend on the mobile UI.

---

## Phase 4 — Online Multiplayer

Add online multiplayer only after offline gameplay is stable.

Target initial mode:

- 1 player versus 1 player online.

Recommended networking stack:

- Unity Netcode for GameObjects;
- Unity Multiplayer Services SDK;
- Unity Relay;
- Unity Authentication;
- Sessions.

Initial online functionality:

- host a match;
- join a match;
- join using a session or join code;
- synchronize players;
- synchronize volleyball;
- synchronize scoring;
- synchronize match state;
- handle disconnects;
- return to menu after match.

After direct friend matches work reliably, quick matchmaking may be added.

---

## Phase 5 — Expanded Team Gameplay

Long-term feature.

Possible modes:

- 2 vs 2;
- 3 vs 3;
- larger team volleyball.

Do not implement this phase until 1 vs 1 networking works correctly.

For team modes, investigate player switching similar to football games.

The user may control one athlete at a time while AI controls teammates.

---

# 4. Core Gameplay

## Match

Two teams play volleyball on opposite sides of a net.

The ball must be sent over the net and allowed to land on the opponent's court.

A player scores when the opponent fails to return the ball correctly.

---

# 5. Prototype Match Format

For the first prototype:

```text
Player
   ↓

──────────────
 Player Side
──────────────
      NET
──────────────
 Enemy Side
──────────────

   ↑
 CPU
```

Use:

- one player character;
- one CPU character;
- one volleyball.

This is deliberately simpler than full volleyball.

Its purpose is to validate the gameplay.

---

# 6. Player Movement

The player must be able to move freely around their side of the court.

Initial movement:

- forward;
- backward;
- left;
- right.

Movement should feel responsive.

Avoid excessive inertia unless testing demonstrates that it improves gameplay.

The character must remain inside the playable court area.

The player cannot freely move onto the opponent's side.

---

# 7. Player Orientation

The controlled character should generally orient toward the gameplay area or volleyball.

Movement should remain understandable even when the camera angle changes.

Avoid control schemes where direction unexpectedly changes based on character rotation.

Prefer camera-relative movement if it provides more intuitive sports-game controls.

---

# 8. Core Volleyball Actions

The first prototype should support the following actions.

## Receive

Used to return a low or incoming ball.

Characteristics:

- relatively easy to perform;
- sends the ball upward;
- provides enough control to continue the rally.

---

## Set

Used to prepare the ball for an attack.

Characteristics:

- sends the ball upward;
- provides more positional control than Receive;
- should make subsequent attacks easier.

---

## Attack / Spike

Used to send the ball aggressively toward the opponent's court.

Characteristics:

- stronger than Receive or Set;
- directed toward the opponent's side;
- effectiveness depends on positioning and timing.

---

## Serve

Starts a rally.

Initially use a simple serve.

Complex serve types are not required for Phase 1.

---

# 9. Action Assistance

The game should not require pixel-perfect positioning.

Provide moderate assistance when performing volleyball actions.

Possible assistance:

- slightly expand the valid hit area;
- automatically face toward the ball during an action;
- apply limited targeting assistance;
- select an appropriate contact point;
- compensate for small timing errors.

Do not make the game completely automatic.

The player should still feel responsible for:

- positioning;
- timing;
- action selection.

---

# 10. Ball Interaction

The volleyball should use physics that feel believable but prioritize gameplay.

Do not attempt to create a completely realistic physics simulation.

Important qualities:

- readable trajectory;
- predictable bounces;
- responsive hits;
- satisfying spikes;
- stable network synchronization later.

Create a central ball interaction system rather than scattering hit logic across unrelated scripts.

Volleyball actions should request a hit through this system.

---

# 11. Ball Contact

A volleyball action should only affect the ball when:

- the character is within an appropriate range;
- the action is currently active;
- the ball is in a valid contact region.

Do not allow unlimited hits from far away.

Provide enough forgiveness for a casual sports game.

---

# 12. Ball Targeting

Initially, attacks may automatically target a reasonable point on the opponent's court.

Later versions should allow directional targeting.

Potential control:

```text
Movement direction
+
Attack button
=
Attack direction
```

Avoid introducing complex manual aiming in the first prototype.

---

# 13. Rally Rules

For the prototype:

A rally begins with a serve.

The rally continues while the ball remains legally playable.

A point ends when:

- the ball touches the ground;
- the ball lands outside the playable area;
- the ball fails to cross the net when required;
- another clearly invalid state occurs.

The scoring system must identify which team receives the point.

---

# 14. Touch Count

Full official volleyball rules are not required during the earliest prototype.

However, architecture should allow a team touch-count rule to be added.

Later target:

Maximum three team contacts before returning the ball.

Do not implement complex edge cases until the basic rally is fun and stable.

---

# 15. Scoring

Prototype scoring:

- rally winner receives one point;
- first player to the configured score wins.

For development, use a small configurable target such as:

```text
5 points
```

The target score must be configurable rather than hard-coded throughout the project.

Later versions may implement official volleyball scoring rules.

---

# 16. Match State

Create explicit match states.

Suggested states:

```text
Waiting
ServePreparation
Playing
PointFinished
MatchFinished
```

Do not rely on unrelated boolean variables for every match condition.

There should be one authoritative match-state system.

---

# 17. CPU Opponent

Phase 1 requires a basic CPU opponent.

The CPU should:

1. observe the ball;
2. predict approximately where it will arrive;
3. move toward an appropriate position;
4. receive reachable balls;
5. return the ball over the net.

The CPU does not need advanced tactical behavior.

Prioritize reliability over intelligence.

The CPU must not instantly teleport to the ball.

---

# 18. CPU Difficulty

Design CPU behavior so parameters can later be adjusted.

Examples:

- movement speed;
- reaction delay;
- positioning accuracy;
- hit accuracy;
- mistake probability.

Do not build a complex difficulty system during Phase 1.

---

# 19. Camera

Use a sports-game camera inspired by broadcast-style football games.

The camera should allow the player to understand:

- their character position;
- opponent position;
- ball position;
- net;
- court boundaries.

The ball should normally remain visible.

The camera may dynamically follow the ball and controlled player.

Avoid excessive camera rotation.

Prioritize gameplay readability.

---

# 20. Camera Prototype

For Phase 1, start with a stable elevated perspective.

Example:

```text
              Camera
                 \
                  \
                   ↓

 Player       Ball       CPU

────────────── NET ──────────────
```

Do not spend excessive development time on cinematic camera behavior before gameplay works.

---

# 21. Input Architecture

Use Unity's new Input System.

Gameplay code must consume abstract gameplay actions.

Suggested actions:

```text
Move
Receive
Set
Attack
Serve
```

Development bindings may use keyboard and gamepad.

Example prototype keyboard bindings:

```text
Move        WASD

Receive     J
Set         K
Attack      L
Serve       Space
```

These are development defaults and may be changed later.

Do not embed these keyboard keys directly inside gameplay scripts.

Use Input Actions.

---

# 22. Mobile Controls

Future mobile layout:

```text
┌──────────────────────────────┐
│                              │
│           GAME               │
│                              │
│                              │
│   MOVE              SET      │
│    ○              RECEIVE    │
│                     ATTACK   │
│                              │
└──────────────────────────────┘
```

Left side:

- virtual joystick.

Right side:

- Receive;
- Set;
- Attack;
- Serve when serving.

Buttons may change contextually if testing shows that fewer controls provide better mobile gameplay.

Touch controls must invoke the same gameplay actions as desktop controls.

---

# 23. Character Controller Architecture

Separate:

```text
Player Input
      ↓
Player Controller
      ↓
Player Movement
      ↓
Volleyball Actions
```

Input must not contain core volleyball rules.

CPU characters should ideally use the same movement and volleyball action systems as human players.

Difference:

```text
Human
↓
PlayerInputController

CPU
↓
AIController

Both
↓
CharacterController
```

This is important for future multiplayer support.

---

# 24. Network-Ready Architecture

Offline gameplay should be designed so networking can later be added without rewriting the entire game.

Separate:

- input;
- simulation;
- visuals;
- match rules;
- networking.

Do not make every gameplay class a NetworkBehaviour during Phase 1.

First create stable offline gameplay.

Networking should later wrap or synchronize the gameplay systems that require it.

---

# 25. Online Multiplayer Architecture

Initial online architecture should use:

```text
Unity Multiplayer Services
        ↓
Session
        ↓
Relay
        ↓
Netcode for GameObjects
        ↓
Game Session
```

Initially use a client-hosted game.

One player acts as host.

The second player joins through Relay.

Do not expose player IP addresses directly.

---

# 26. Online Authority

When multiplayer is implemented, avoid allowing each client to independently decide important match results.

Important authoritative state should include:

- ball state;
- score;
- rally state;
- serve ownership;
- match state.

For the initial online version, the host may act as the gameplay authority.

Clients send gameplay inputs or requests.

The authoritative side validates important gameplay results.

---

# 27. Ball Networking

The volleyball is the most important networked object.

Online implementation must consider:

- latency;
- ownership;
- collision consistency;
- position synchronization;
- hit validation.

Avoid simply synchronizing raw Rigidbody state without considering network latency.

Prototype online ball synchronization independently before expanding online gameplay complexity.

---

# 28. Online Connection Flow

Target initial flow:

```text
Title
 ↓
Online
 ↓
Create Match / Join Match
 ↓
Session
 ↓
Opponent Connected
 ↓
Match Loading
 ↓
Match
 ↓
Result
 ↓
Return to Menu
```

Friend matches should be implemented before automatic matchmaking.

---

# 29. Join Code

Initial online prototype should support a join-code workflow.

Host:

```text
Create Match
↓
Join Code Generated
↓
Share Code
```

Client:

```text
Enter Join Code
↓
Join
```

This allows multiplayer testing without first building a full matchmaking system.

---

# 30. Authentication

Use Unity Authentication when online services are introduced.

Anonymous authentication is acceptable for the first online prototype.

Do not build account registration or social features during initial multiplayer development.

---

# 31. Matchmaking

Automatic matchmaking is a later feature.

Implement only after:

- host works;
- client works;
- Relay works;
- player synchronization works;
- ball synchronization works;
- complete online matches work.

Later use Unity Multiplayer Services matchmaking or Quick Join.

---

# 32. Scene Structure

Suggested scenes:

```text
Boot
Title
Match
Result
```

During the earliest prototype, a single Match scene is acceptable.

Do not create unnecessary menus before gameplay has been validated.

---

# 33. UI

Phase 1 requires only essential UI.

Display:

```text
PLAYER SCORE

CPU SCORE
```

Also display:

- current serve state when useful;
- match result;
- restart button.

Debug UI may be added during development.

---

# 34. Visual Prototype

Use placeholder assets during Phase 1.

Characters may initially use:

- capsules;
- simple humanoid models;
- primitive meshes.

Court objects may use basic geometry.

The ball must be visually distinct and easy to track.

Do not delay gameplay implementation while waiting for final art.

---

# 35. Animation

Do not make animation a dependency for initial gameplay.

Gameplay logic must function before polished animations exist.

Later animations should include:

- idle;
- run;
- receive;
- set;
- spike;
- serve;
- jump.

Animation must visually represent gameplay actions rather than determine all core gameplay state.

---

# 36. Audio

Later versions should provide feedback for:

- ball hit;
- serve;
- spike;
- ground impact;
- point scored;
- match victory.

Audio is not a Phase 1 blocker.

---

# 37. Feedback

Every successful volleyball hit should have clear feedback.

Potential feedback:

- impact sound;
- subtle camera feedback;
- ball speed change;
- visual hit effect;
- character animation.

Spikes should feel significantly stronger than normal receives.

Do not add excessive effects that make the ball difficult to see.

---

# 38. Physics

Use Unity physics where appropriate.

Physics must remain stable at the game's intended frame rate.

Avoid unnecessarily high ball speeds that cause collision tunneling.

Use appropriate collision detection for the volleyball if required.

Important gameplay values must be configurable.

Examples:

```text
Ball gravity
Receive force
Set force
Spike force
Serve force
Player movement speed
Jump force
```

---

# 39. Debugging Tools

Create useful debug tools where appropriate.

Examples:

- reset ball;
- reset rally;
- force serve;
- display ball velocity;
- display match state;
- visualize hit range.

Debug features should not interfere with normal gameplay.

---

# 40. Automated Tests

Prioritize automated testing for deterministic systems.

Edit Mode tests should cover:

- scoring;
- match state transitions;
- team identification;
- rally result calculation.

Play Mode tests should cover where practical:

- ball ground detection;
- serving;
- player movement;
- rally reset;
- object spawning.

Networking tests should be introduced during Phase 4.

---

# 41. Phase 1 Success Criteria

Phase 1 is complete when all of the following are true.

## Player

- player can move;
- player remains within court boundaries;
- player can Receive;
- player can Set;
- player can Attack;
- player can Serve.

## Ball

- volleyball behaves consistently;
- volleyball can cross the net;
- ball can be hit by both players;
- ball touching the ground ends the rally.

## CPU

- CPU moves toward playable balls;
- CPU can return the ball;
- rallies can occur between player and CPU.

## Match

- points can be scored;
- score UI updates;
- rally resets;
- match can end;
- match can restart.

## Technical

- Unity compiles;
- no significant Console errors;
- basic automated tests pass;
- gameplay can run for multiple rallies without breaking.

---

# 42. Phase 1 Explicit Non-Goals

Do NOT implement these during the initial prototype unless they are required for basic architecture:

- online multiplayer;
- matchmaking;
- login accounts;
- rankings;
- cosmetics;
- monetization;
- large character rosters;
- official tournament systems;
- full six-player teams;
- advanced animation systems;
- final art;
- commentary;
- spectators;
- complex AI formations.

The first objective is:

> Make moving, positioning, receiving, setting, and attacking the volleyball enjoyable.

---

# 43. Development Priority

When choosing between:

```text
More Features

or

Better Core Volleyball
```

choose:

```text
Better Core Volleyball
```

A small game with satisfying volleyball interactions is more valuable than a large game with weak core controls.

---

# 44. Codex Implementation Instructions

Before implementing:

1. Read this entire specification.
2. Read `AGENTS.md`.
3. Inspect the current Unity project.
4. Inspect existing Input Actions.
5. Inspect existing scenes and project settings.

Then implement Phase 1 incrementally.

Suggested order:

```text
1. Court
2. Player movement
3. Ball physics
4. Ball interaction
5. Serve
6. Receive
7. Set
8. Attack
9. Net and boundaries
10. Rally detection
11. Scoring
12. CPU opponent
13. Camera
14. UI
15. Match completion
16. Tests
17. Gameplay validation
```

Validate Unity regularly rather than implementing everything before testing.

Do not begin Phase 2, Phase 3, or Phase 4 merely because Phase 1 code exists.

Phase 1 must be playable and technically stable first.

---

# 45. Current Milestone

The current milestone is:

# Phase 1 — Offline 1 vs 1 Volleyball Prototype

The immediate target is a playable match between:

```text
Human Player
vs
CPU Player
```

using placeholder graphics.

Online multiplayer and smartphone implementation are future milestones.

The most important question for the current prototype is:

> Does directly controlling a volleyball player and positioning them to hit the ball feel enjoyable?
