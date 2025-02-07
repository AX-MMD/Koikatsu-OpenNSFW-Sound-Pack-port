# Pre-setup #############################################################################

## Dependencies (optionnal):

* You'll need python 3.8+ if you need to use the python scripts.

## File Explorer:
    
* Unzip [AX_MMD] zipmod (or other) and use it as a base, or other method of making a blank zipmod.
* Put the blank zipmod in `/mods`.

Your zipmod should look like this (NO majuscule and NO space, aside from the top folder):

<Name of zipmod>
-- manifest.xml
-- abdata/
   |
   -- studio/
   |  |
   |  -- <modname>/
   |     |
   |     -- <subfolders>/ (optionnal)
   |        |
   |        -- <name_you_want>.unity3d
   |
   -- info/
      |
      -- com_<author_name>_<modname>/
         |
         -- <subfolders>/ (optionnal, you can put these files in com_<author_name>_<modname>/)
            |
            -- ItemCategory_<number>_<kk_item_group>.csv (ex. KK group for 3D SFX is 11)
            -- ItemGroup_<name_of_parent_folder>.csv
            -- ItemList_<number>_<kk_item_group>_<your_first_category_number_inside_ItemCategory>

## Unity:
* Start with a KoikatsuModdingTools folder from github.
* Put `src/GeneratePrefabsFromWav.cs` in `Assets/Editor`.
* Put `src/SEComponent.cs` in `Assets/Scripts`.
* Top menu, GameObject->Create Empty.
* Rename object to `base_3dse`.
* In base_3dse -> Add Component -> SE Component (Script).
* Set attributes:
    - Sound Type = Game SE3D = 4
    - Is Loop = disabled = 0
    - Type = Logarithmic = 1
    - Rolloff Distance Min = 5
    - Rolloff Distance Max = 9
    - Volume = 1

    IMPORTANT: 
    - Is Loop = disabled for short single sounds, but enable it for longer sounds.
    - If you only have some sounds that should loop, you can enable it individually later.

* Drag and Drop `base_3dse` into `Assets/3DSE objects` (make that folder).
* Make a folder where you'll import your sound files, I like `Assets/3DSE sources`.

# Compilation ###########################################################################

## File Explorer:

* Your sound file needs at least 50ms of silence before the sound plays to prevent the risk of random sound burst in KK.
* If it's too short or you're not sure, Drag & Drop folder with sound files on kk_sound_file_mod.py

## Unity:

* Make a folder for the category of sound files.
* Drop sound files in to import to unity.
* Set attributes:
    - Decompress On Load
    - Preload Audio Data
    - Vorbis
    - Quality 100
    - Preserve Sample Rate 
    
* Modify `GetItemName` in `Assets/Editor/GeneratePrefabsFromWav.cs` so it can do one of these:
    - By default, will return the filename as is
    - Extract the number from the sound file name
    - Make a custom name, whatever you want
    
    IMPORTANT: Your `.prefab` file cannot have spaces or capital letters to be compatible with KK.
    
* In the Unity main window (NOT the directory tree on the left) Right-click folder with your audio and `Generate Prefabs From Wav`.
    
    IMPORTANT: If you have some `.prefabs` that needs to loop, now is the time to modify the individual objects in `Assets/Mods/Prefab`.
    
* Select all `.prefab` objects in `Assets/Mods/Prefab`.
* In bottom right `Asset Labels` menu, make New->studio\<mod_name>\<(optionnal)subfolders>/<compiled_assets_name> and select `unity3d`.
    
* In Top menu, Window -> AssetBundle Browser -> Build tab.
* Enable `Clear Folders` and disable `Compression`.
* Press `Build Asset Bundles`.
    

## Zipmod:

* Edit the `Item` .csv files with your new sound items.
* Put the `Item` .csv files in your blank zipmod as deep as you can go in `/abdata/studio/info`.
* Transfert the compiled `.unity3d` from `KoikatsuModdingTools/Build/abdata/...` into your zipmod base.
* Zip your mod into `./zipmods` using the command `./make build` or zip manually using compression = 0.
