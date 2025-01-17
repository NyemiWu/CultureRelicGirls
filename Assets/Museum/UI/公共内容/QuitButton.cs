using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Home;
namespace Museum {

	public class QuitButton:MonoBehaviour {

		Image image;
		private void Start() {
			image=GetComponent<Image>();
		}

		private void Update() {
			if(UIController.currentMode is EmptyMode) image.color=Color.clear;
			else image.color=Color.white;
		}

		public void OnClick() {

			if(UIController.currentMode.OverrideQuitButton()) return;

			CameraController.instance.SetFocus(null);
			EmptyMode.EnterMode();
		}

	}

}