# Member 2 Plan: Conveyor Belt / Movement Challenge Room

Project: A Tiny Robot Escape  
Branch: `member2-conveyor-room`  
Owner: Member 2, Conveyor Belt / Movement Challenge Room

## Your Goal

Build one playable 3D challenge room where the robot must cross conveyor belts and moving obstacles/platforms to reach a goal area. The room should be self-contained so it can later be connected to the main maze by Member 4.

## What You Need To Deliver

1. A Unity scene or prefab for the conveyor/movement room.
2. Conveyor belts that push the player in a direction.
3. Moving platforms or moving obstacles.
4. Clear win and fail conditions.
5. Basic lighting/materials so the room looks like a futuristic lab challenge.
6. Short notes explaining how teammates can connect the room to the main maze.

## Implemented In This Branch

- Scene: `Assets/Member2_ConveyorRoom/Scenes/ConveyorChallengeRoom.unity`
- Player test object: `Robot_Player_Test`, controlled in first person with WASD or Arrow Keys. Mouse looks around; Q/E also turn.
- Challenge layout: two conveyor belt sections, one moving platform, one moving hazard, a fail zone, and a goal trigger.
- Feedback: simple HUD with objective, reset status, and completion status.
- Visuals: lab floor/walls, colored conveyor/platform/hazard/goal materials, animated conveyor direction markers, and colored lights.
- Regeneration tool: Unity menu `Tiny Robot Escape > Build Member 2 Conveyor Room`.

## Recommended Room Design

Make the room simple and readable:

1. Entrance door or spawn point.
2. First conveyor belt section that pushes sideways.
3. Safe platform.
4. Second conveyor belt section that pushes forward/backward.
5. Moving obstacle or moving platform section.
6. Goal trigger at the end.
7. Hazard/fall zone or obstacle trigger that resets the robot.

This is enough for the checkpoint and can be polished later.

## Unity Object Checklist

Create these GameObjects in your scene:

- `Member2_ConveyorRoom`
- `SpawnPoint`
- `ConveyorBelt_A`
- `ConveyorBelt_B`
- `MovingPlatform_A`
- `MovingObstacle_A`
- `FailZone`
- `GoalZone`
- `RoomLights`
- `CameraAnchor` if your room needs a specific camera view

Keep all your room objects under the parent object `Member2_ConveyorRoom`.

## Scripts Included In This Branch

- `ConveyorBelt.cs`: pushes a Rigidbody while it stays on the belt.
- `MovingPlatform.cs`: moves an object between two local positions.
- `MovingHazard.cs`: moves a hazard and resets the player on contact.
- `FailZone.cs`: resets the player after falling or entering a failure area.
- `ChallengeGoal.cs`: detects when the player reaches the room goal.
- `PlayerRespawn.cs`: resets a player Rigidbody to a spawn point.
- `SimpleRobotController.cs`: temporary first-person test controller for the robot in this room, compatible with Unity's New Input System and legacy Input Manager.
- `FollowCamera.cs`: first-person camera follow behavior for the test scene.
- `ChallengeHud.cs`: on-screen objective/result text.
- `ConveyorBeltAnimator.cs`: animates conveyor direction markers.

## Step By Step Work Plan

### Step 1: Open The Project

Open the folder `cse_457_final` in Unity Hub using Unity `6000.3.1f1`.

If Unity asks to regenerate files or import packages, let it finish before editing scenes.

### Step 2: Confirm Your Branch

In Terminal:

```bash
cd "/Users/yitinghuang/Desktop/Project 5/cse_457_final"
git branch --show-current
```

It should print:

```text
member2-conveyor-room
```

### Step 3: Make Your Scene

In Unity:

1. Create a new scene.
2. Save it as `Assets/Member2_ConveyorRoom/Scenes/ConveyorChallengeRoom.unity`.
3. Add floor, walls, belts, platforms, hazards, goal, and lights.
4. Keep your assets under `Assets/Member2_ConveyorRoom`.

### Step 4: Build The Mechanics

Attach scripts like this:

- Add `ConveyorBelt` to each conveyor belt trigger collider.
- Add `MovingPlatform` to moving platforms or moving obstacles.
- Add `MovingHazard` to dangerous moving obstacles.
- Add `FailZone` to the fall zone or any failure trigger volume.
- Add `ChallengeGoal` to the goal trigger.
- Add `PlayerRespawn` to the robot/player object.

The player object should have:

- `Rigidbody`
- `Collider`
- Tag: `Player`
- `PlayerRespawn` script

### Step 5: Test The Room

Test these cases:

1. Robot gets pushed by conveyor belts.
2. Robot can ride or dodge moving objects.
3. Touching a hazard resets the robot.
4. Falling into `FailZone` resets the robot.
5. Reaching `GoalZone` logs success.

### Step 6: Polish

After the mechanics work:

1. Add lab-style materials.
2. Add colored lights to show safe/danger areas.
3. Add simple animation feel by rotating belt rollers or adding arrows.
4. Add signs or visual direction markers if the route is unclear.

### Step 7: Commit Your Work

Use small commits:

```bash
git status
git add Assets/Member2_ConveyorRoom Documentation/Member2_ConveyorRoom_Plan.md
git commit -m "Add member 2 conveyor room scaffold"
git push
```

## How To Connect Later

Member 4 can connect this room by loading your scene additively or by placing your room prefab in the main maze. The minimum integration points should be:

- Entrance position: `SpawnPoint`
- Exit/win trigger: `GoalZone`
- Failure reset: `PlayerRespawn`

Avoid modifying other members' folders unless the team agrees.
