using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Museum {

	public class CharacterController:MonoBehaviour {

		static readonly Vector2 inactivePosition = new Vector2(0,-10000);

		[SerializeField] int characterIndex;
		[SerializeField] CharacterData staticData;
		PlayerData.CharacterData saveData;
		PathFinder pathFinder;
		BuildingWithCharacterInteractionBase.SlotToken slotToken;

		private void Start() {
			saveData=PlayerData.PlayerDataRoot.instance.characterDatas[characterIndex];
			pathFinder=GetComponent<PathFinder>();
			UpdateStateChange();
			UpdateWorkEnd();
			pathFinder.SetTarget(GetTargetPosition());
			pathFinder.TeleportToTarget();
		}

		private void Update() {
			UpdateWanderPosition();
		}

		int previousHealStatus;
		int previousLevelUpStatus;

		int currentHealStatus {
			get => saveData.healStatus.value;
			set => saveData.healStatus.value=value;
		}
		int currentLevelUpStatus {
			get => saveData.levelUpStatus.value;
			set => saveData.levelUpStatus.value=value;
		}
		int currentLevel {
			get => saveData.level.value;
			set => saveData.level.value=value;
		}

		Vector2 wanderPosition;
		float wanderTimer;
		void UpdateWanderPosition() {
			wanderTimer-=Time.deltaTime;
			if(wanderTimer<=0) {
				wanderTimer=Random.Range(10f,30f);
				ResetWanderPosition();
			}
		}
		void ResetWanderPosition() {

			FloorPath wanderFloor = pathFinder.currentFloor;
			if(Random.Range(0,1)<0.4f) {
				wanderFloor=FloorPath.floorPaths[Random.Range(0,3)];
			}
			wanderPosition=new Vector2(Random.Range(wanderFloor.leftX,wanderFloor.rightX),wanderFloor.y);

		}

		Vector2 GetTargetPosition() {
			if(saveData.level.value<=0) return inactivePosition;
			Vector2 result = wanderPosition;
			if(slotToken!=null) result=slotToken.position;
			return result;
		}

		void UpdateStateChange() {

			if(previousHealStatus!=currentHealStatus) {
				switch(currentHealStatus) {
				case 0:
					WorkBenchController.instance.FreeSlot(slotToken);
					slotToken=null;
					break;
				case PlayerData.CharacterData.healTime:
					slotToken=WorkBenchController.instance.GetSlot();
					break;
				case PlayerData.CharacterData.healCost:
					slotToken=WorkBenchController.instance.GetStaticSlot();
					break;
				}
			}

			if(previousLevelUpStatus!=currentLevelUpStatus) {
				switch(currentLevelUpStatus) {
				case 0:
					LibraryController.instance.FreeSlot(slotToken);
					ResearchStationController.instance.FreeSlot(slotToken);
					slotToken=null;
					break;
				case PlayerData.CharacterData.levelUpTime:
					slotToken=LibraryController.instance.GetSlot();
					break;
				case PlayerData.CharacterData.levelUpCost:
					slotToken=ResearchStationController.instance.GetStaticSlot();
					break;
				}
			}

			previousHealStatus=currentHealStatus;
			currentLevelUpStatus=currentLevelUpStatus;

		}

		void UpdateWorkEnd() {
			if(currentLevelUpStatus!=0&&saveData.levelUpProgression.completion) {
				currentLevelUpStatus=0;
				currentLevel++;
			}
			if(currentLevelUpStatus==PlayerData.CharacterData.healTime&&saveData.healProgression.completion) StopHealTime();
			if(currentLevelUpStatus==PlayerData.CharacterData.healCost&&pathFinder.arrived) FinishHealCost();
		}

		public void OnClick() {

		}

		public static string messageBuffer;

		bool InWork() {
			if(currentLevelUpStatus!=0) {
				messageBuffer="��������";
				return true;
			}
			if(currentHealStatus!=0) {
				messageBuffer="�����޸�";
				return true;
			}
			return false;
		}
		public bool CanLevelUpTime() {
			if(InWork()) return false;
			if(currentLevel>=staticData.maxLevel) {
				messageBuffer="�Ѿ�����";
				return false;
			}
			if(!LibraryController.instance.HasSlotLeft()) {
				messageBuffer="ͼ��������򲻿���";
				return false;
			}
			return true;
		}
		public bool CanLevelUpCost() {
			if(InWork()) return false;
			if(currentLevel>=staticData.maxLevel) {
				messageBuffer="�Ѿ�����";
				return false;
			}
			if(!ResearchStationController.instance.HasSlotLeft()) {
				messageBuffer="�о�վ�����򲻿���";
				return false;
			}
			if(PlayerData.PlayerDataRoot.smCount<staticData.levels[saveData.level.value].levelUpCost) {
				messageBuffer="��ʶ���岻��";
				return false;
			}
			return true;
		}
		public bool CanHealTime() {
			if(InWork()) return false;
			if(saveData.healthAmount>=1) {
				messageBuffer="Ѫ������";
				return false;
			}
			if(!WorkBenchController.instance.HasSlotLeft()) {
				messageBuffer="ά��̨�����򲻿���";
				return false;
			}
			return true;
		}
		public bool CanHealCost() {
			if(InWork()) return false;
			if(!WorkBenchController.instance.HasSlotLeft()) {
				messageBuffer="ά��̨�����򲻿���";
				return false;
			}
			if(PlayerData.PlayerDataRoot.smCount<staticData.levels[saveData.level.value].levelUpCost) {
				messageBuffer="��ʶ���岻��";
				return false;
			}
			return true;
		}

		public bool GoLevelUpTime() {
			if(!CanLevelUpTime()) return false;
			currentLevelUpStatus=PlayerData.CharacterData.levelUpTime;
			saveData.levelUpProgression.SetProgression(staticData.levels[currentLevel].levelUpTimeTime,0);
			return true;
		}
		public bool GoLevelUpCost() {
			if(!CanLevelUpTime()) return false;
			currentLevelUpStatus=PlayerData.CharacterData.levelUpCost;
			saveData.levelUpProgression.SetProgression(staticData.levels[currentLevel].levelUpCostTime,0);
			PlayerData.PlayerDataRoot.smCount-=staticData.levels[currentLevel].levelUpCost;
			return true;

		}
		public bool GoHealTime() {
			if(!CanHealTime()) return false;
			currentHealStatus=PlayerData.CharacterData.healTime;
			System.TimeSpan healtime = new System.TimeSpan((long)(System.TimeSpan.TicksPerSecond*staticData.levels[currentLevel].hpMax*staticData.healTimPerHpInSecond));
			saveData.healProgression.SetProgression(healtime,saveData.healthAmount);
			return true;
		}
		public bool GoHealCost() {
			if(!CanHealCost()) return false;
			currentHealStatus=PlayerData.CharacterData.healCost;
			return true;
		}

		public void StopHealTime() {
			float healProgression = saveData.healProgression.progressionAmount;
			saveData.healthAmount=healProgression;
			currentHealStatus=0;
		}

		void FinishHealCost() {
			PlayerData.PlayerDataRoot.smCount-=Mathf.CeilToInt(staticData.healCostPerHp*staticData.levels[currentLevel].hpMax);
			saveData.healthAmount=1;
			currentHealStatus=0;
		}

	}

}