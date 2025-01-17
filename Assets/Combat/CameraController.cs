using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat {
	public class CameraController:MonoBehaviour {

		public static CameraController instance { get; private set; }

		[SerializeField] float moveSpeed = 5;
		[SerializeField] bool isFilming;

		new Camera camera;

		private void Start() {
			camera=GetComponent<Camera>();
			PlayerData.PlayerDataController.Init();
			instance=this;
		}

		private void Update() {
			if(isFilming) {
				UpdateFilmingMode();
			} else {
				UpdateTargetX();
				UpdateSelfPosition();
				UpdateScreenshake();
			}
		}

		float targetX;
		float cameraRadius;

		void UpdateTargetX() {
			/*
			//调整摄像机左右，越大越左，越小越右
			const float cameraStaticOffset = 10;
			targetX=(EntityFriendly.leftestX+EntityFriendly.rightestX)*0.5f+cameraStaticOffset;

			float originalTargetX = targetX;
			const float offsetRange = 12;
			const float offsetStrength = 0.2f;
			foreach(var entity in EntityBase.entities) {
				float x = entity.transform.position.x;
				if(Mathf.Abs(x-originalTargetX)<offsetRange) {
					float thisTimeStrength = x<originalTargetX ? 2 : 1;
					thisTimeStrength*=offsetStrength;
					targetX+=thisTimeStrength*Mathf.Sqrt(Mathf.Abs(x-originalTargetX))*Mathf.Sign(x-originalTargetX);
				}
			}

			float xMax = EntityFriendly.leftestX+cameraSize*0.5f-1f;
			float xMin = EntityFriendly.rightestX;

			targetX=Mathf.Clamp(targetX,xMin,xMax);
			*/

			float centerX = 0.5f*(EntityFriendly.rightestX+EntityFriendly.leftestX);
			float fov = Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView,camera.aspect);
			cameraRadius=Mathf.Tan(Mathf.Deg2Rad*fov*0.5f)*Mathf.Abs(transform.position.z);

			float minX = EntityFriendly.rightestX-cameraRadius*0.8f;
			float maxX = EntityFriendly.leftestX+cameraRadius*0.8f;


			SortedDictionary<float,EntityBase> enemiesByDist = new SortedDictionary<float,EntityBase>();
			foreach(var i in EntityBase.entities) {
				if((!i)||(!(i is EntityEnemy))) continue;
				if(!i.isActiveAndEnabled) continue;
				if(enemiesByDist.ContainsKey(Mathf.Abs(i.transform.position.x-centerX))) continue;
				enemiesByDist.Add(Mathf.Abs(i.transform.position.x-centerX),i);
			}

			float offset = 0;

			foreach(var i in enemiesByDist) {
				float x = i.Value.transform.position.x;
				if(x<maxX+cameraRadius&&x>minX-cameraRadius) {
					minX=Mathf.Max(minX,x-cameraRadius);
					maxX=Mathf.Min(maxX,x+cameraRadius);
				}
			}

			targetX=(minX+maxX)*0.5f;

			foreach(var i in enemiesByDist) {
				float x = i.Value.transform.position.x;
				if(x<targetX+cameraRadius&&x>targetX-cameraRadius) continue;
				if(Mathf.Abs(x-targetX)>cameraRadius*2) continue;
				offset+=2*(x-targetX)/(cameraRadius*2);
			}

			targetX=Mathf.Clamp(targetX+offset,minX,maxX);

		}


		void UpdateSelfPosition() {
			Vector3 position = transform.position;
			Vector3 targetPosition = position;

			float x = position.x;
			float speed = moveSpeed*Mathf.Abs(x-targetX);
			targetPosition.x=targetX;
			position=Vector2.MoveTowards(position,targetPosition,speed*Time.deltaTime);

			position.x=Mathf.Clamp(
				position.x,
				CombatRoomController.currentRoom.startX+cameraRadius,
				CombatRoomController.currentRoom.endX-cameraRadius
			);

			position.z=-4.8f;
			//调整摄像机前后
			position.y=CombatRoomController.currentRoom.transform.position.y+1.1f;
			//调整摄像机上下
			transform.position=position;



		}

		Vector3 filmingModeVelocity;
		const float filmingModeAcceleration = 6;
		const float filmingModeSpeed = 6;
		void UpdateFilmingMode() {

			Vector3 targetVelocity = Vector3.zero;
			if(Input.GetKey(KeyCode.RightArrow)) targetVelocity+=Vector3.right;
			if(Input.GetKey(KeyCode.LeftArrow)) targetVelocity+=Vector3.left;
			if(Input.GetKey(KeyCode.UpArrow)) targetVelocity+=Vector3.up;
			if(Input.GetKey(KeyCode.DownArrow)) targetVelocity+=Vector3.down;
			targetVelocity*=filmingModeSpeed;

			filmingModeVelocity=Vector3.MoveTowards(filmingModeVelocity,targetVelocity,Time.deltaTime*filmingModeAcceleration);

			transform.position+=filmingModeVelocity*Time.deltaTime;
		}

		#region 屏幕抖动
		//屏幕抖动及由屏幕抖动施加的offset
		public void AddScreenShake(float intensity) {
			if(screenShakeIntensityRaw+intensity>intensity*5) screenShakeIntensityRaw=intensity*5;
			else screenShakeIntensityRaw+=Mathf.Abs(intensity);
		}
		float screenShakeIntensityRaw;
		float screenShakeSampleY1;
		float screenShakeSampleY2;
		float screenShakeSampleY3;
		float screenShakeSampleX;
		bool screenShakeResampled;

		void UpdateScreenshake() {
			if(screenShakeIntensityRaw>0) {

				float screenShakeDecreaseSpeed = 5;
				if(screenShakeIntensityRaw>1) screenShakeDecreaseSpeed*=screenShakeIntensityRaw;
				screenShakeIntensityRaw-=screenShakeDecreaseSpeed*Time.deltaTime;

				float screenShakeIntensity = screenShakeIntensityRaw*10;
				if(screenShakeIntensity>1) screenShakeIntensity=Mathf.Sqrt(screenShakeIntensity);
				screenShakeIntensity*=0.1f;

				screenShakeSampleX+=Time.deltaTime*100f;

				//平动
				Vector2 screenShakeVector = new Vector2(
					Mathf.PerlinNoise(screenShakeSampleX,screenShakeSampleY1),
					Mathf.PerlinNoise(screenShakeSampleX,screenShakeSampleY2)
				);

				screenShakeVector-=0.5f*Vector2.one;
				screenShakeVector*=screenShakeIntensity;

				transform.position+=(Vector3)screenShakeVector;

				//转动
				float screenRotate = Mathf.PerlinNoise(screenShakeSampleX,screenShakeSampleY3);

				screenRotate-=0.5f;
				screenRotate*=20;
				screenRotate*=screenShakeIntensity;
				transform.rotation=new Angle(screenRotate).quaternion;

				screenShakeResampled=false;

			} else if(!screenShakeResampled) {
				transform.rotation=Quaternion.identity;
				screenShakeResampled=true;

				screenShakeSampleY1=Random.Range(-4000000f,4000000f);
				screenShakeSampleY2=Random.Range(-4000000f,4000000f);
				screenShakeSampleY3=Random.Range(-4000000f,4000000f);
				screenShakeSampleX=Random.Range(-8000000f,0);

			}

		}
		#endregion
	}
}