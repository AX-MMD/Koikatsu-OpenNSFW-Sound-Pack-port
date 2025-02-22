using System.Collections.Generic;


public class Category
    {
        public string name { get; set; }
        public string author { get; set; }
        public List<WavFile> files { get; set; }

        public Category(string name, string author, List<WavFile> files)
        {
            this.name = name;
            this.author = author;
            this.files = files;
        }

        public Category(string name, string author)
        {
            this.name = name;
            this.author = author;
            this.files = new List<WavFile>();
        }

        public Category(string name)
        {
            this.name = name;
            this.author = "";
            this.files = new List<WavFile>();
        }

        public string getKey()
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

        public void AddFile(WavFile file)
        {
            this.files.Add(file);
        }

        public void AddFiles(List<WavFile> files)
        {
            this.files.AddRange(files);
        }
    }

public class WavFile
{
    public string prefabName { get; set; }
    private string _itemName;
    public string itemName
    {
        get
        {
            return this.isLoop ? _itemName : "(S)" + _itemName;
        }
        set
        {
            _itemName = value;
        }
    }
    public string path { get; set; }
    public bool isLoop { get; set; }

    public WavFile(string prefabName, string itemName, string path, bool isLoop)
    {
        this.prefabName = prefabName;
        this.itemName = itemName;
        this.path = path;
        this.isLoop = isLoop;
    }
}