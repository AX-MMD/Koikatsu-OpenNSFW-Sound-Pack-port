using System.Collections.Generic;
using ActionGame.MapSound;
using System;

namespace IllusionMods.Koikatsu3DSEModTools {

	public class Category
	{
		public string name { get; set; }
		public string author { get; set; }
		public List<StudioItemParam> items { get; set; }

		public Category(string name, string author, List<StudioItemParam> items)
		{
			this.name = name;
			this.author = author;
			this.items = items;
		}

		public Category(string name, string author)
		{
			this.name = name;
			this.author = author;
			this.items = new List<StudioItemParam>();
		}

		public Category(string name)
		{
			this.name = name;
			this.author = "";
			this.items = new List<StudioItemParam>();
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

		public void AddFile(StudioItemParam item)
		{
			this.items.Add(item);
		}

		public void AddFiles(List<StudioItemParam> items)
		{
			this.items.AddRange(items);
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

	public class StudioItemParam
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

		public StudioItemParam(string itemName, string path = null, PrefabModifier prefabModifier = null)
		{
			this.itemName = itemName;
			this.path = path;
			this.prefabModifier = prefabModifier;
		}
	}
}