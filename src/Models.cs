using System.Collections.Generic;
using ActionGame.MapSound;
using System;

namespace IllusionMods.Koikatsu3DSEModTools {

public class Category
    {
        public string name { get; set; }
        public string author { get; set; }
        public List<SoundFile> files { get; set; }

        public Category(string name, string author, List<SoundFile> files)
        {
            this.name = name;
            this.author = author;
            this.files = files;
        }

        public Category(string name, string author)
        {
            this.name = name;
            this.author = author;
            this.files = new List<SoundFile>();
        }

        public Category(string name)
        {
            this.name = name;
            this.author = "";
            this.files = new List<SoundFile>();
        }

        public string GetKey()
        {
            if (this.author == "")
            {
                return this.name;
            }
            else
            {
                return this.name + " [" + this.author + "]";
            }
        }

        public void AddFile(SoundFile file)
        {
            this.files.Add(file);
        }

        public void AddFiles(List<SoundFile> files)
        {
            this.files.AddRange(files);
        }
    }

public class PrefabModifier
{
    public bool isLoop { get; set; }
    public Utils.Tuple<float> threshold { get; set; }
    public float volume { get; set; }

    public PrefabModifier(bool isLoop = false, Utils.Tuple<float> threshold = null, float volume = -1.0f)
    {
        this.isLoop = isLoop;
        this.threshold = threshold;
        this.volume = volume;
    }
}

public class SoundFile
{
    private string _itemName;
    public string itemName
    {
        get
        {
            if (this.prefabModifier != null && this.prefabModifier.isLoop)
            {
                return _itemName;
            }
            else
            {
                return "(S)" + _itemName;
            }
        }
        set
        {
            _itemName = value;
        }
    }
    public string path { get; set; }
    public PrefabModifier prefabModifier { get; set; }
    public string prefabName { 
        get
        {
            return Utils.ToSnakeCase(_itemName);
        }
    }

    public SoundFile(string itemName, string path = null, PrefabModifier prefabModifier = null)
    {
        this.itemName = itemName;
        this.path = path;
        this.prefabModifier = prefabModifier;
    }
}

}