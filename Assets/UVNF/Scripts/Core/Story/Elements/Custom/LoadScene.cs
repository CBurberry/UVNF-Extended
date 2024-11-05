using System.Collections;
using UnityEngine;
using UnityEditor;
using UVNF.Core.UI;
using UVNF.Extensions;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace UVNF.Core.Story.Utility
{
	public class LoadScene : StoryElement
	{
		public override string ElementName => nameof(LoadScene);

		public override Color32 DisplayColor => _displayColor;
		private Color32 _displayColor = new Color32().Utility();

		public override StoryElementTypes Type => StoryElementTypes.Utility;

		[JsonProperty]
		public string Scene = "Scene Name";

#if UNITY_EDITOR
		public override void DisplayLayout(Rect layoutRect, GUIStyle label)
		{
			Scene = EditorGUILayout.TextField(Scene);
		}
#endif

		public override IEnumerator Execute(UVNFManager managerCallback, UVNFCanvas canvas)
		{
			SceneManager.LoadScene(Scene);

			return null;
		}
	}
}