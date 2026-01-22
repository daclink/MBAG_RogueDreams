# Rogue Dreams (working title)
Unity version: Unity 6 version 6000.3.5f1

## Documents
[Folder with design docs etc](https://drive.google.com/drive/folders/1xm3KBXomn7FhLXyPOoRf_2H_nCljgIJi)


## Info / Tutorials 

### Starting with Unity
The following 6 will help with quickly understanding the interface:
1. [Interface Overview (~5m)](https://www.youtube.com/watch?v=D7v2pjke5sc)
1. [Scene View (~11m)](https://youtu.be/nG0fXdXylMI)
1. [Game View (~6m)](https://www.youtube.com/watch?v=w7RLUM9TBXY)
1. [Hierarchy Window (~4m)](https://www.youtube.com/watch?v=9rR3AS74UH0)
1. [Project Window  (~8m)](https://www.youtube.com/watch?v=4iT4Zhez-zw)
1. [Inspector Window (~10m)](https://www.youtube.com/watch?v=qltyYjFdyVc)


### PCG resources
[unity wave function colapse](https://www.youtube.com/watch?v=57MaTTVH_XI)

### Seting up unity and Git
[Using Git with Unity (~7m)](https://csumb.hosted.panopto.com/Panopto/Pages/Viewer.aspx?id=64a26be7-199c-4ef5-ac22-b101018a97f8)


[Scripting tutorial series](https://www.youtube.com/playlist?list=PLX2vGYjWbI0S9-X2Q021GUtolTqbUBB9B)

### Creating new weapon pickups
- For initial creation, create an empty game object called WeaponPickup with a sprite renderer, any collider set to trigger, and add a text tmp pro UGUI as a child to this object
- Add the weapon pickup behavior script to the parent object we created and assign the pickupSO, text pop up and sprite renderer from THIS object. 
- Make this a prefab without the SO assigned

- When wanting to make a new weapon, just drag the weaponPickup prefab into the scene, and assign the specific weaponSO to the weapon pickup behavior script. 
- The weaponSO takes a sprite, the weapon prefab that will be passed to the player, and a weapon name

### Claude help with structure:

## Complete Flow:
```
Asset Creation:
1. Create WeaponSO (e.g., "SwordDataSO") → Set damage, sprite, stats
2. Create Weapon Prefab → Attach WeaponBehavior → Assign SwordDataSO
3. Create WeaponPickupSO (e.g., "SwordPickupSO") → Assign weapon prefab

In Scene:
4. Drag WeaponPickup prefab → Assign SwordPickupSO
5. Player collects → Weapon instantiated with all its data!
```

## Visual Structure:
```
Project Assets:
├── ScriptableObjects/
│   ├── Weapons/
│   │   ├── SwordDataSO (WeaponSO)
│   │   ├── AxeDataSO (WeaponSO)
│   │   └── SpearDataSO (WeaponSO)
│   └── Pickups/
│       ├── SwordPickupSO (WeaponPickupSO) → references SwordPrefab
│       ├── AxePickupSO (WeaponPickupSO) → references AxePrefab
│       └── SpearPickupSO (WeaponPickupSO) → references SpearPrefab
└── Prefabs/
    ├── Weapons/
    │   ├── SwordPrefab (has WeaponBehavior → references SwordDataSO)
    │   ├── AxePrefab (has WeaponBehavior → references AxeDataSO)
    │   └── SpearPrefab (has WeaponBehavior → references SpearDataSO)
    └── Pickups/
        └── WeaponPickup (reusable)
