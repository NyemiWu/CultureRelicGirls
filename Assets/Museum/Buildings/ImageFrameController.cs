using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Home;
namespace Museum {

	public class ImageFrameController:BuildingControllerBase {

		[SerializeField] Image targetImage;

		public override void OnClick(CameraFocus.CancelFocus cancelFocus) {

			if(saveData.level.value<0) {
				cancelFocus.doCancel=true;
				return;
			}

			BuildingLevelUpMode.EnterMode(id,OnExtraButtonClick);
			spriteRenderer.material=normalMaterial;
		}

		public void OnExtraButtonClick() {
			CharacterSelectionMode.EnterMode(CharacterFilter,true,OnCharacterSelection);
		}

		bool CharacterFilter(int id) {
			if(id==0) return false;
			return PlayerData.PlayerDataRoot.instance.characterDatas[id].level.value>0;
		}

		void OnCharacterSelection(int id) {
			saveData.extraStatus.value=id;
			BuildingLevelUpMode.instance.BackToThisMode();
		}

		protected override void FixedUpdate() {
			base.FixedUpdate();
			if(saveData.extraStatus.value<=0) targetImage.color=Color.clear;
			else {
				targetImage.color=Color.white;

				throw new System.NotImplementedException();
				//targetImage.sprite=CharacterData.datas[saveData.extraStatus.value].picture;
			}
		}

	}

}