using System;
using UnityEngine;
using ActionGame.MapSound;
using Manager;

namespace Manager
{
	public sealed class Sound
	{
		public enum Type
		{
			BGM,
			ENV,
			SystemSE,
			GameSE2D,
			GameSE3D
		}
	}
}

namespace Studio.Sound
{
	public class SEComponent : MonoBehaviour
	{
		[SerializeField]
		public AudioClip _clip;

		[SerializeField]
		public Manager.Sound.Type _soundType = Manager.Sound.Type.GameSE3D;

		[SerializeField]
		public bool _isLoop;

		[SerializeField]
		public SEComponent.RolloffType _type = SEComponent.RolloffType.Logarithmic;

		[SerializeField]
		public Threshold _rolloffDistance;

		[Range(0f, 1f)]
		[SerializeField]
		public float _volume = 1f;

		AudioSource _audioSource;

		public enum RolloffType
		{
			Logarithmic,
			Linear,
			BN2  // In reference to the modder BN2, who uses this value in his mod.
		}
	}
}

namespace ActionGame.MapSound
{
	[Serializable]
	public struct Threshold
	{
		public float min;

		public float max;

		public Threshold(float minValue, float maxValue)
        {
            this.min = minValue;
            this.max = maxValue;
        }
	}


}