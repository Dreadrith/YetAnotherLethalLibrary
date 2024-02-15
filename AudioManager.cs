using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Networking;
using static YetAnotherLethalLibrary.Utility;

namespace YetAnotherLethalLibrary;

public static class AudioManager
{
	const string AUDIO_PLAYER_NAME = "YALLAudioSource";
	public static List<AudioSource> allAudioSources = new List<AudioSource>();

	public static void AddAudioSourceToPlayer(PlayerControllerB controller)
	{
		ConditionLog($"Client Spawned: {controller.actualClientId}");
		var go = controller.gameObject;
		var audioObject = go.transform.Find(AUDIO_PLAYER_NAME);
		if (audioObject == null)
		{
			ConditionLog("Creating audio player...");
			audioObject = new GameObject(AUDIO_PLAYER_NAME).transform;
			audioObject.SetParent(go.transform);
			audioObject.localPosition = Vector3.zero;
			audioObject.localRotation = Quaternion.identity;
			audioObject.localScale = Vector3.one;
			
			var audioPlayer = audioObject.gameObject.AddComponent<AudioSource>();
			audioPlayer.rolloffMode = AudioRolloffMode.Custom;
			audioPlayer.playOnAwake = false;
			audioPlayer.spatialBlend = 1;
			audioPlayer.maxDistance = 20;

			//Don't ask
			var customRollOffCurve = new AnimationCurve(
				new Keyframe(0, 1, -4.666667f, -4.666667f),
				new Keyframe(0.04f, 0.84f, -3.708333f, -3.708333f),
				new Keyframe(1/5f, 0.42f, -1.975f, -1.975f),
				new Keyframe(2/5f, 0.18f, -0.7083333f, -0.7083333f),
				new Keyframe(1, 0.05f, 0, 0)
			);
		
			audioPlayer.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRollOffCurve);
			
			ConditionLog("Adding audio player to list...");
			allAudioSources.Add(audioPlayer);
		}
	}
	
	public static List<AudioClip> LoadAudioClipsFromFolder(string folderPath)
	{
		var files = Directory.GetFiles(folderPath);
		List<AudioClip> audioClips = new List<AudioClip>();
		foreach (var f in files)
		{
			ConditionLog($"Loading audio clip: {f}");
			var clip = LoadAudioClipFromPath(f);
			if (clip != null) audioClips.Add(clip);
		}
		return audioClips;
	}

	public static AudioClip LoadAudioClipFromPath(string path) => LoadAudioClipFromPath(path, AudioTypeFromPath(path));
	public static AudioClip LoadAudioClipFromPath(string path, AudioType type)
	{
		using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, type))
		{
			string error;
			uwr.SendWebRequest();
			try
			{
				while (!uwr.isDone) { }

				if (uwr.result == UnityWebRequest.Result.Success)
				{
					var clip = DownloadHandlerAudioClip.GetContent(uwr);
					clip.name = Path.GetFileNameWithoutExtension(path);
					return clip;
				}
				error = uwr.error;
			}
			catch (Exception ex)
			{
				error = ex.ToString();
			}
			ConditionLog($"Failed to load audio clip from {path}.\nError: {error}", true, LogLevel.Error);
			return null;
		}
	}
	
	public static AudioType AudioTypeFromPath(string path) => AudioTypeFromExtension(Path.GetExtension(path).ToLower());
	public static AudioType AudioTypeFromExtension(string extension)
	{
		switch (extension)
		{
			//copilot filled this :)
			case ".wav":
				return AudioType.WAV;
			case ".mp3":
				return AudioType.MPEG;
			case ".ogg":
				return AudioType.OGGVORBIS;
			case ".aiff":
				return AudioType.AIFF;
			case ".aif":
				return AudioType.AIFF;
			case ".aifc":
				return AudioType.AIFF;
			case ".flac":
				return AudioType.AUDIOQUEUE;
			default:
				return AudioType.UNKNOWN;
		}
	}
	
	public static void PlayAudio(ulong clientID, AudioClip clip, float volume = 1, float range = 20, float loudness = 0.5f, bool audibleToEnemies = true)
	{
		if (ConditionLog("Attempting to play a null audioclip!", clip == null, LogLevel.Error)) return;
		if (!TryGetPlayerCustomAudioSource(clientID, out AudioSource audioSource))
		{
			ConditionLog($"Audio Source not found for ID: {clientID}", true, LogLevel.Error);
			return;
		}

		ConditionLog($"Playing clip {clip.name} for {clientID}", true, LogLevel.Debug);
		audioSource.volume = volume;
		audioSource.minDistance = 0;
		audioSource.maxDistance = Mathf.Max(0.01f, range);
		audioSource.PlayOneShot(clip);
		if (audibleToEnemies)
		{
			ConditionLog("Playing audible noise...");
			if (GameManager.TryGetPlayerController(clientID, out PlayerControllerB controller))
			{
				bool isInClosedShip = controller.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed;
				ConditionLog($"Audible noise playing for {controller.playerUsername}. (In Closed Ship: {isInClosedShip})", true, LogLevel.Debug);
				if (clip.length <= 1) RoundManager.Instance.PlayAudibleNoise(controller.transform.position, range, loudness, 0, isInClosedShip);
				else RoundManager.Instance.StartCoroutine(PlayAudibleNoiseForTime(clip.length - 0.5f, controller.transform.position, range/2f, loudness, isInClosedShip));
			}
			else ConditionLog($"Couldn't match ID {clientID} with a controller!");
		}
	}
	
	/*private static readonly Dictionary<AudioClip, float> clipLoudnessDictionary = new Dictionary<AudioClip, float>();
	public static float LoudnessFromClip(AudioClip clip)
	{
		if (clipLoudnessDictionary.TryGetValue(clip, out var clipLoudness)) return clipLoudness;
		float loudness = 0.5f;
		float[] samples = new float[clip.samples];
		if (clip.GetData(samples, 0))
		{
			float sum = samples.Sum(Math.Abs);
			loudness = sum / samples.Length;
			CustomLog($"Successfully sampled loudness as {loudness}");
		} else CustomLog($"Failed to sample loudness!", true, LogLevel.Error);
		clipLoudnessDictionary.Add(clip, loudness);
		return loudness;
	}*/

	public static IEnumerator PlayAudibleNoiseForTime(float time, Vector3 position, float range = 20, float loudness = 0.5f, bool isInClosedShip = false, float frequencyInSeconds = 0.5f)
	{
		if (frequencyInSeconds < 0.2f)
		{
			frequencyInSeconds = 0.2f;
			ConditionLog("Adjust audible noise frequency to 0.2 seconds.", true, LogLevel.Warning);
		}
		while (time > 0)
		{
			RoundManager.Instance.PlayAudibleNoise(position, range, loudness, 0, isInClosedShip);
			time -= frequencyInSeconds;
			yield return new WaitForSeconds(frequencyInSeconds);
		}
	}

	public static bool TryGetPlayerCustomAudioSource(ulong clientID, out AudioSource audioSource)
	{
		audioSource = null;
		if (GameManager.TryGetPlayerController(clientID, out PlayerControllerB controller))
		{
			var audioGO = controller.transform.Find(AUDIO_PLAYER_NAME);
			return audioGO.TryGetComponent(out audioSource);
		} 
		
		ConditionLog($"Failed to get player controller for client {clientID}!", true, LogLevel.Error);
		return false;
	}

	public static void Initialize()
	{
		ConditionLog("Initializing AudioManager...");
		GameManager.OnPlayerSpawned -= AddAudioSourceToPlayer;
		GameManager.OnPlayerSpawned += AddAudioSourceToPlayer;
	}
}