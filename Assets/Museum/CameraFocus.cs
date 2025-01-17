using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Museum;

namespace Home {
	[AddComponentMenu("�����/����ͷ�۽�����")]
	public class CameraFocus:MonoBehaviour {

		public class CancelFocus {
			public bool doCancel;
		}

		[SerializeField] float focusMargin = 0.5f;
		[SerializeField] UnityEvent<CancelFocus> onClick;
		Collider2D clickArea;

		private void Start() {
			clickArea=GetComponent<Collider2D>();
		}

		Vector2 pressPosition;
		bool pressed;
		private void Update() {
			if(Input.GetMouseButtonDown(0)&&(UIController.currentMode is EmptyMode)) {
				pressPosition=Input.mousePosition;
				Vector2 physicalPosition = Camera.main.ScreenToWorldPoint(pressPosition);
				pressed=clickArea.OverlapPoint(physicalPosition);
			}
			if(Input.GetMouseButtonUp(0)&&pressed&&(UIController.currentMode is EmptyMode)) {

				Vector2 releasePosition = Input.mousePosition;
				float dist = (releasePosition-pressPosition).sqrMagnitude;
				if(dist<400) {
					CancelFocus doCancel = new CancelFocus();
					onClick?.Invoke(doCancel);
					if(!doCancel.doCancel) {
						CameraController.instance.SetFocus(this);
						ButtonSound.instance.PlaySound();
					}
				}

			}
		}

		public float focusSize {
			get {
				return clickArea.bounds.size.x+focusMargin;
			}
		}
		public Vector2 focusPoint {
			get {
				return clickArea.bounds.center;
			}
		}

	}
}